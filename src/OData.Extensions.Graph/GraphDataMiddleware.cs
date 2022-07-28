using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder.Core.V1;
using Microsoft.OData.UriParser;
using OData.Extensions.Graph.Lang;
using OData.Extensions.Graph.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

            var translator = new OperationTranslator(bindingResolver, model, schemaName);

            if (await HandleRequestAsync(context, parser, translator))
            {
                return;
            }

            // if the request is not a get request or if the content type is not correct
            // we will just invoke the next middleware and do nothing.
            await NextAsync(context);
        }

        private async Task<bool> HandleRequestAsync(HttpContext context, ODataUriParser parser, OperationTranslator translator)
        {
            ODataResponse response = new ODataResponse();
            
            TranslatedOperation operation = null;
            IDictionary<string, object> variables = null;

            try
            {
                // TODO: 
                // Parse path and initial segment
                var model = await modelProvider.GetModelAsync(context.Request);
                ODataPath path = parser.ParsePath();
                var pathSegment = ODataUtility.GetIdentifierFromSelectedPath(path);

                IEdmEntitySet entitySet = model.GetEntitySet(pathSegment);
                OperationBinding operationBinding = null;

                switch (context.Request.Method.ToUpperInvariant())
                {
                    case "POST":
                    case "PATCH":
                        if (!context.Request.HasJsonContentType())
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            await context.Response.WriteAsync($"`{context.Request.ContentType}` Content-Type is not supported");
                            return true;
                        }

                        context.Request.EnableBuffering();
                        context.Request.Body.Position = 0;

                        variables = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(context.Request.Body, Constants.Serialization.Reading);
                        
                        operationBinding = bindingResolver.ResolveMutation(context.Request.Method, entitySet.Name, schemaName);
                        operation = translator.TranslateMutation(parser, path, entitySet, operationBinding, variables?.Keys?.ToArray());
                        break;
                    case "DELETE":
                        operationBinding = bindingResolver.ResolveMutation(context.Request.Method, entitySet.Name, schemaName);
                        operation = translator.TranslateMutation(parser, path, entitySet, operationBinding);
                        break;
                    // These methods can have id's in their arguments only
                    case "GET":
                        operationBinding = bindingResolver.ResolveQuery(entitySet.Name, schemaName);
                        operation = translator.TranslateQuery(parser, path, entitySet, operationBinding);
                        break;
                    default:
                        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                        return true;
                }

                response = await ExecuteGraphQuery(context, operationBinding, operation.PathSegment, operation.DocumentNode);                
            }
            catch (ODataUnrecognizedPathException)
            {
                // This is an invalid odata path based on the current configuration
                // Move along and continue processing the request stream ...
                return false;
            }
            catch (ODataException ex)
            {
                response
                    .WithErrors(new[] {
                        new {
                            message = ex.Message,
                            type = "ODataError"
                        }
                    })
                    .WithStatusCode(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                response
                    .WithErrors(new object[]
                    {
                        new {
                            message = ex.Message,
                            type = "InternalError"
                        }
                    })
                    .WithStatusCode(StatusCodes.Status500InternalServerError);
            }

            await response.WriteAsync(context.Response, context.RequestAborted);

            return true;
        }

        private async Task<ODataResponse> ExecuteGraphQuery(HttpContext context, OperationBinding binding, string entitySet, DocumentNode document, IDictionary<string, object> variables = null)
        {
            var response = new ODataResponse();
            
            // TODO: Update base endpoint.
            response.WithContext($"{context.Request.Scheme}://{context.Request.Host}/api/$metadata#{entitySet}");

#if DEBUG
            response.WithDebug("pathSegment", entitySet);
            response.WithDebug("operation", binding?.Operation ?? "_unknown_");

            if (document != null)
            {
                response.WithDebug("commandText", document.ToString(true));
            }
#endif

            try
            {
                var requestExecutor = await GetExecutorAsync(context.RequestAborted);
                
                var request = QueryRequestBuilder.New();
                request.SetQuery(document);

                if(variables != null)
                {
                    foreach(var variable in variables)
                    {
                        request.AddVariableValue(variable.Key, variable.Value);
                    }
                }
                
                // TODO: Find a way to use operation bindings to be more useful
                // with determining the type of response code to send back.

                var result = await requestExecutor.ExecuteAsync(request.Create());

                if (result.Errors?.Any() == true)
                {
                    response.WithStatusCode(500);
                    
                    var errors = result.Errors.Select(e => new {
                        message = e.Message ?? e.ToString(),
                        type = "GraphError",
                        code = e.Code
                    }).ToArray();

                    response.WithErrors(errors);


                    if (errors.Any(e => e.code == ErrorCodes.Authentication.NotAuthenticated))
                    {
                        response.WithStatusCode(401);
                        return response;
                    }

                    if (errors.Any(e => e.code == ErrorCodes.Authentication.NotAuthorized))
                    {
                        response.WithStatusCode(403);
                        return response;
                    }

                    return response;
                }

                var queryResult = result as QueryResult;

                if (queryResult != null)
                {
                    // TODO: Use model to resolve metadata
                    // TODO: Fixme, add operation bindings for remote schemas too.
                    
                    // WARN: This may be a ResultMapList
                    var queryResultData = queryResult.Data[binding?.Operation ?? entitySet];
                    
                    // TODO: Parse
                    var resultData = ParseResults(queryResultData);

                    if(resultData != null && resultData.GetType().IsArray)
                    {
                        response.WithValues((object[])resultData);
                    }
                    else if(resultData != null)
                    {
                        var resultObject = resultData as IDictionary<string, object>;

                        if (resultObject.ContainsKey("items"))
                        {
                            resultObject.Add("value", resultObject["items"]);
                            resultObject.Remove("items");
                        }

                        response.WithProperties(resultObject);
                    }

                    return response;
                }

                // TODO: Some other parsing ...
                return response;
            }
            catch (Exception)
            {
                // I am not sure what kind of exceptions to expect here
                response.WithStatusCode(400);
                return response;
            }
        }

        private static object ParseResults(object result)
        {
            var resultMap = result as IResultMap;
            var resultMapList = result as IResultMapList;

            if (resultMapList != null)
            {
                return ParseResultMapList(resultMapList);
            }

            if (resultMap != null)
            {
                return ParseResultMap(resultMap);
            }

            return null;
        }

        private static IEnumerable<IDictionary<string, object>> ParseResultMapList(IResultMapList resultMapList)
        {
            var results = new List<IDictionary<string, object>>();

            foreach (var resultMap in resultMapList)
            {
                results.Add(ParseResultMap(resultMap));
            }

            return results.ToArray();
        }

        private static IDictionary<string, object> ParseResultMap(IResultMap resultMap)
        {
            var data = new Dictionary<string, object>();

            foreach (var property in resultMap)
            {
                // Hidden properties or null data
                if(property.Value == null || property.Name.StartsWith("_"))
                {
                    continue;
                }

                var subList = property.Value as IResultMapList;

                if (subList != null)
                {
                    var subResults = ParseResultMapList(subList);
                    data.Add(property.Name, subResults);
                    continue;
                }
                
                data.Add(property.Name, property.Value);
            }

            return data;
        }
    }
}
