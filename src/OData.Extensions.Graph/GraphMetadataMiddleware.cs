using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Validation;
using OData.Extensions.Graph.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace OData.Extensions.Graph
{
    public class GraphMetadataMiddleware : MiddlewareBase
    {
        private readonly IEdmModelProvider modelProvider;
        private readonly IMemoryCache memoryCache;

        public GraphMetadataMiddleware(
            Microsoft.AspNetCore.Http.RequestDelegate next,
            IMemoryCache memoryCache,
            IEdmModelProvider modelProvider,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            NameString schemaName)
            : base(next, executorResolver, resultSerializer, schemaName)
        {
            this.memoryCache = memoryCache;
            this.modelProvider = modelProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await Task.CompletedTask;

            bool handle =
                HttpMethods.IsGet(context.Request.Method) &&
                context.Request.Path.HasValue &&
                context.Request.Path.Value.EndsWith("/$metadata", StringComparison.InvariantCultureIgnoreCase);

            if (handle)
            {
                await HandleRequestAsync(context);
            }
            else
            {
                // if the request is not a get request or if the content type is not correct
                // we will just invoke the next middleware and do nothing.
                await NextAsync(context);
            }
        }

        //private async Task<string> GetGraphQLSchemaAsync(HttpContext context)
        //{
        //    IRequestExecutor requestExecutor = await GetExecutorAsync(context.RequestAborted);

        //    string fileName =
        //        requestExecutor.Schema.Name.IsEmpty ||
        //        requestExecutor.Schema.Name.Equals(Schema.DefaultName)
        //            ? "schema.graphql"
        //            : requestExecutor.Schema.Name + ".schema.graphql";

        //    using (MemoryStream stream = new MemoryStream())
        //    {
        //        await SchemaSerializer.SerializeAsync(
        //            requestExecutor.Schema,
        //            stream,
        //            indented: true,
        //            context.RequestAborted)
        //            .ConfigureAwait(false);

        //        return Encoding.UTF8.GetString(stream.ToArray());
        //    }
        //}

        private async Task<byte[]> GetRawMetadata(HttpRequest request)
        {
            MemoryStream stream = new MemoryStream();

            var model = await modelProvider.GetModelAsync(request);

            using (XmlWriter writer = XmlWriter.Create(stream))
            {
                CsdlWriter.TryWriteCsdl(model, writer, CsdlTarget.OData, out IEnumerable<EdmError> errors);
                writer.Flush();
                await stream.FlushAsync();
            }

            stream.Position = 0;

            return stream.ToArray();
        }

        private async Task<Stream> GetMetadataStreamAsync(HttpRequest request)
        {
            // Do some fancy stuff if we need to but it's mostly for caching ...
            var cacheId = await modelProvider.GetModelCacheIdAsync(request);

            if (string.IsNullOrWhiteSpace(cacheId))
            {
                return new MemoryStream(await GetRawMetadata(request));
            }

            var metadata = await memoryCache.GetOrCreateAsync($"graph.metadata.cache.{cacheId}", async (@entry) => await GetRawMetadata(request));

            return new MemoryStream(metadata);
        }

        // TODO: Convert GraphQL Schema to IEdmModel Schema for OData Emit
        private async Task HandleRequestAsync(HttpContext context)
        {
            // Proof that we can call an embedded server ...
            var schema = (await GetExecutorAsync(context.RequestAborted)).Schema;

            //EdmEntityType customer = new EdmEntityType("NS", "Customer");
            //customer.AddKeys(customer.AddStructuralProperty("ID", EdmPrimitiveTypeKind.Int32));
            //customer.AddStructuralProperty("Name", EdmPrimitiveTypeKind.String);
            //EdmStructuralProperty ssn = customer.AddStructuralProperty("SSN", EdmPrimitiveTypeKind.String);

            //EdmModel model = new EdmModel(true);
            //model.AddElement(customer);
            //EdmTerm stringTerm = new EdmTerm("DefaultNamespace", "StringTerm", EdmCoreModel.Instance.GetString(true));
            //model.AddElement(stringTerm);


            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/xml";

            using (var stream = await GetMetadataStreamAsync(context.Request))
            {
                await stream.CopyToAsync(context.Response.Body);
                await context.Response.Body.FlushAsync();
            }
        }
    }
}
