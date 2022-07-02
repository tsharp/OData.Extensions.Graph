using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
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
        private IEdmModelProvider modelProvider;

        public GraphDataMiddleware(
            Microsoft.AspNetCore.Http.RequestDelegate next,
            IEdmModelProvider modelProvider,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            NameString schemaName)
            : base(next, executorResolver, resultSerializer, schemaName)
        {
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
                var qt = new QueryTranslator(model);

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
            await Task.CompletedTask;

            // Do Conversion Stuff ...

            var translatedQuery = qt.Translate(parser);
            var result = await ExecuteGraphQuery(context, translatedQuery.PathSegment, translatedQuery.DocumentNode);

            context.Response.ContentType = "application/json";

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                result,
                typeof(object),
                Constants.Serialization.Options,
                context.RequestAborted);

            return true;
        }

        private Task<object> ExecuteGraphQuery(HttpContext context, string entitySet, string query)
        {
            return ExecuteGraphQuery(context, entitySet, Utf8GraphQLParser.Parse(query));
        }

        private async Task<object> ExecuteGraphQuery(HttpContext context, string entitySet, DocumentNode document)
        {
            var response = new Dictionary<string, object>();

            try
            {
                var requestExecutor = await GetExecutorAsync(context.RequestAborted);

                var request = QueryRequestBuilder.New();
                request.SetQuery(document);

                var result = await requestExecutor.ExecuteAsync(request.Create());

                if (result.Errors?.Any() == true)
                {
                    var errors = result.Errors.Select(e => e.ToString()).ToArray();
                    response.Add("errors", errors);
                    context.Response.StatusCode = 400;
                    return response;
                }

                var queryResult = result as QueryResult;

                if (queryResult != null)
                {
                    var resultMap = (ResultMapList)queryResult.Data[entitySet];
                    var values = new List<IDictionary<string, object>>();

                    response.Add("values", values);

                    foreach (var entity in resultMap)
                    {
                        var entityDict = new Dictionary<string, object>();
                        values.Add(entityDict);

                        foreach (var property in entity)
                        {
                            entityDict.Add(property.Name, property.Value);
                        }
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
