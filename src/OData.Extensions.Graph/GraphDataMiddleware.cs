using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.OData;
using Microsoft.OData.UriParser;
using OData.Extensions.Graph.Lang;
using OData.Extensions.Graph.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OData.Extensions.Graph
{
    public class GraphDataMiddleware : MiddlewareBase
    {
        private readonly IEdmModelProvider modelProvider;
        private readonly IBindingResolver bindingResolver;
        private readonly NameString schemaName;
        
        public GraphDataMiddleware(
            Microsoft.AspNetCore.Http.RequestDelegate next,
            IBindingResolver bindingResolver,
            IEdmModelProvider modelProvider,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            NameString schemaName)
            : base(next, executorResolver, resultSerializer, schemaName)
        {
            this.schemaName = schemaName;
            this.bindingResolver = bindingResolver;
            this.modelProvider = modelProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = Regex.Replace(context.Request.Path.ToString(), @"^(\/api\/)", "");

            var model = await modelProvider.GetModelAsync(context.Request);

            ODataUriParser parser = new ODataUriParser(model,
                new Uri($"{context.Request.Scheme}://{context.Request.Host.ToUriComponent()}/api"),
                new Uri($"{path}{context.Request.QueryString}", UriKind.Relative));

            parser.Settings.MaximumExpansionCount = 5;
            parser.Settings.MaximumExpansionDepth = 2;
            parser.Resolver = new ODataUriResolver()
            {
                EnableCaseInsensitive = true
            };

            try
            {
                // TODO: Disallow anything but get requests for 0.3.x
                if (context.Request.Method != HttpMethods.Get)
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    return;
                }

                var qt = new QueryTranslator(bindingResolver, model, schemaName);

                if (await HandleRequestAsync(context, parser, qt))
                {
                    return;
                }
            }
            catch (ODataUnrecognizedPathException)
            {
                // This is an invalid odata path based on the current configuration
                // Move along and continue processing the request stream ...
            }

            // if the request is not a get request or if the content type is not correct
            // we will just invoke the next middleware and do nothing.
            await NextAsync(context);
        }

        private async Task<bool> HandleRequestAsync(HttpContext context, ODataUriParser parser, QueryTranslator qt)
        {
            // Do Conversion Stuff ...
            TranslatedQuery translatedQuery = null;

            try
            {
                translatedQuery = qt.Translate(parser);
                var result = await ExecuteGraphQuery(context, translatedQuery.PathSegment, translatedQuery.DocumentNode);
                await SendResponseObjectAsync(context, result);
            }
            catch (ODataException ex)
            {
                var result = new Dictionary<string, object>()
                {
                    ["@odata.context"] = "https://some.random-api.com/api/$metadata#Edm.String",
                    ["@odata.next"] = null,
                    ["@odata.count"] = null,
                    ["errors"] = new object[]
                        {
                            new {
                                message = ex.Message,
                                type = "ODataError"
                            }
                        }
                };

#if DEBUG
                if (translatedQuery != null)
                {
                    result.Add("debug_commandText", translatedQuery.DocumentNode.ToString(true));
                    result.Add("debug_pathSegment", translatedQuery.PathSegment);
                }
#endif

                await SendResponseObjectAsync(context, result, StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                var result = new Dictionary<string, object>()
                {
                    ["@odata.context"] = "https://some.random-api.com/api/$metadata#Edm.String",
                    ["@odata.next"] = null,
                    ["@odata.count"] = null,
                    ["errors"] = new object[]
                        {
                            new {
                                message = ex.Message,
                                type = "InternalError"
                            }
                        }
                };

#if DEBUG
                if (translatedQuery != null)
                {
                    result.Add("debug_commandText", translatedQuery.DocumentNode.ToString(true));
                    result.Add("debug_pathSegment", translatedQuery.PathSegment);
                }
#endif

                await SendResponseObjectAsync(context, result, StatusCodes.Status500InternalServerError);
            }


            return true;
        }

        private async Task SendResponseObjectAsync(HttpContext context, object data, int statusCode = 200)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                data,
                typeof(object),
                Constants.Serialization.Options,
                context.RequestAborted);
        }

        private Task<object> ExecuteGraphQuery(HttpContext context, string entitySet, string query)
        {
            return ExecuteGraphQuery(context, entitySet, Utf8GraphQLParser.Parse(query));
        }

        private async Task<object> ExecuteGraphQuery(HttpContext context, string entitySet, DocumentNode document)
        {
            var response = new Dictionary<string, object>()
            {
                ["@odata.context"] = null,
                ["@odata.next"] = null,
                ["@odata.count"] = null,
            };

            try
            {
                var requestExecutor = await GetExecutorAsync(context.RequestAborted);

                var request = QueryRequestBuilder.New();
                request.SetQuery(document);

                var result = await requestExecutor.ExecuteAsync(request.Create());

                if (result.Errors?.Any() == true)
                {
                    var errors = result.Errors.Select(e => new {
                        message = e.Message ?? e.ToString(),
                        type = "GraphError",
                        code = e.Code
                    }).ToArray();

                    response.Add("errors", errors);
                    context.Response.StatusCode = 500;

#if DEBUG
                    if (document != null)
                    {
                        response.Add("debug_commandText", document.ToString(true));
                        response.Add("debug_pathSegment", entitySet);
                    }
#endif

                    if (response.Any(e => e.Key == ErrorCodes.Authentication.NotAuthenticated))
                    {
                        context.Response.StatusCode = 401;
                        return response;
                    }

                    if (response.Any(e => e.Key == ErrorCodes.Authentication.NotAuthorized))
                    {
                        context.Response.StatusCode = 403;
                        return response;
                    }

                    return response;

                }

                var queryResult = result as QueryResult;

                if (queryResult != null)
                {
                    var operation = bindingResolver.Resolve(entitySet, schemaName);
                    // WARN: This may be a ResultMapList
                    var resultMap = (ResultMap)queryResult.Data[operation?.Operation ?? entitySet];
                    var values = new List<IDictionary<string, object>>();

                    response.Add("values", values);

                    var entityDict = new Dictionary<string, object>();
                    values.Add(entityDict);

                    foreach (var property in resultMap)
                    {
                        if(property.Name == "items")
                        {
                            foreach(var subList in property.Value as ResultMapList)
                            {
                                foreach (var attribute in subList)
                                {
                                    entityDict.Add(attribute.Name, attribute.Value);
                                }
                            }

                            continue;
                        }

                        entityDict.Add(property.Name, property.Value);
                    }

                    return response;
                }

                // TODO: Some other parsing ...
                return response;
            }
            catch (Exception ex)
            {
                // I am not sure what kind of exceptions to expect here
                context.Response.StatusCode = 400;
                return response;
            }
        }
    }
}
