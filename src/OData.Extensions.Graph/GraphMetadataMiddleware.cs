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
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OData.Extensions.Graph
{
    public class GraphMetadataMiddleware : MiddlewareBase
    {
        private readonly IEdmModelProvider modelProvider;
        private readonly IMemoryCache memoryCache;
        private readonly PathString metadataBase;

        public GraphMetadataMiddleware(
            Microsoft.AspNetCore.Http.RequestDelegate next,
            IMemoryCache memoryCache,
            IEdmModelProvider modelProvider,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            string routeBase,
            NameString schemaName)
            : base(next, executorResolver, resultSerializer, schemaName)
        {
            this.memoryCache = memoryCache;
            this.modelProvider = modelProvider;

            metadataBase = new PathString($"{routeBase}/$metadata");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await Task.CompletedTask;
            PathString subPaths = new PathString();

            bool handle =
                HttpMethods.IsGet(context.Request.Method) &&
                context.Request.Path.HasValue &&
                context.Request.Path.StartsWithSegments(metadataBase, StringComparison.InvariantCultureIgnoreCase, out subPaths);

            if (!handle || !await HandleRequestAsync(context, subPaths))
            {
                // if the request is not a get request or if the content type is not correct
                // we will just invoke the next middleware and do nothing.
                await NextAsync(context);
            }
        }

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
        private async Task<bool> HandleRequestAsync(HttpContext context, PathString subString)
        {
            // Proof that we can call an embedded server ...
            var schema = (await GetExecutorAsync(context.RequestAborted)).Schema;

            if (subString.StartsWithSegments(new PathString("/schema")))
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/graphql; charset=utf-8";
                await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(schema.ToString()));
                await context.Response.BodyWriter.FlushAsync();

                return true;
            }

            if (subString.HasValue && !string.IsNullOrWhiteSpace(subString.Value))
            {
                return false;
            }

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/xml; charset=utf-8";

            using (var stream = await GetMetadataStreamAsync(context.Request))
            {
                await stream.CopyToAsync(context.Response.Body);
                await context.Response.Body.FlushAsync();
            }

            return true;
        }
    }
}
