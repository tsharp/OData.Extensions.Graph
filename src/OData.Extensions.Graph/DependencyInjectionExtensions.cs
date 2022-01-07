namespace Microsoft.AspNetCore.Builder
{
    using global::OData.Extensions.Graph;
    using global::OData.Extensions.Graph.Metadata;
    using HotChocolate;
    using HotChocolate.AspNetCore.Serialization;
    using HotChocolate.Execution.Configuration;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Linq;
    using System.Text;
    using static Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory;

    public static class DependencyInjectionExtensions
    {
        public static bool IsAdded<TService, TImplementation>(this IServiceCollection services)
        {
            return services
                .Where(s => s.ServiceType == typeof(TService) && s.ImplementationType == typeof(TImplementation))
                .Any();
        }

        public static IRequestExecutorBuilder AddODataForGraphQL(this IServiceCollection services)
        {
            // Make sure memory caching is enabled / available to use for DI/IoC.
            services.AddMemoryCache();

            return services
                .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                .AddSingleton<IEdmModelProvider, ODataModelSchemaTranslator>()
                .AddGraphQLServer();
        }

        // https://github.com/ChilliCream/hotchocolate/blob/ee5813646fdfea81035c681989793514f33b5d94/src/HotChocolate/AspNetCore/src/AspNetCore/Extensions/HotChocolateAspNetCoreServiceCollectionExtensions.Http.cs
        public static IEndpointConventionBuilder MapODataForGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            string path = "/api",
            NameString schemaName = default)
            => MapODataForGraphQL(endpointRouteBuilder, new PathString(path), schemaName);

        public static IEndpointConventionBuilder MapODataForGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            PathString path,
            NameString schemaName = default)
        {
            if (endpointRouteBuilder is null)
            {
                throw new ArgumentNullException(nameof(endpointRouteBuilder));
            }

            var pattern = Parse(path.ToString().TrimEnd('/') + "/{**slug}");

            IApplicationBuilder requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
            var schemaNameOrDefault = schemaName.HasValue ? schemaName : Schema.DefaultName;

            requestPipeline
                .UseMiddleware<GraphMetadataMiddleware>(schemaNameOrDefault)
                .UseMiddleware<GraphDataMiddleware>(schemaNameOrDefault)
                .Use(async (context, next) =>
                {
                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "application/json";
                    var content = Encoding.UTF8.GetBytes("{ \"error\": \"Bad Request\" }");
                    await context.Response.Body.WriteAsync(content, 0, content.Length);
                    await context.Response.Body.FlushAsync();
                });

            return new GraphEndpointConventionBuilder(
                endpointRouteBuilder
                    .Map(pattern, requestPipeline.Build())
                    .WithDisplayName("OData for GraphQL Pipeline"));
        }
    }
}