namespace OData.Extensions.Graph
{
    using OData.Extensions.Graph.Metadata;
    using HotChocolate;
    using HotChocolate.AspNetCore.Serialization;
    using HotChocolate.Execution.Configuration;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Linq;
    using System.Text;
    using static Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory;
    using System.Threading.Tasks;
    using HotChocolate.Execution;
    using HotChocolate.AspNetCore;
    using HotChocolate.Types.Descriptors;
    using OData.Extensions.Graph.Conventions;

    public static class DependencyInjectionExtensions
    {
        public static bool IsAdded<TService, TImplementation>(this IServiceCollection services)
        {
            return services
                .Where(s => s.ServiceType == typeof(TService) && s.ImplementationType == typeof(TImplementation))
                .Any();
        }

        public static IRequestExecutorBuilder AddODataForGraphQL(this IServiceCollection services, NameString schemaName = default)
        {
            // Make sure memory caching is enabled / available to use for DI/IoC.
            services.AddMemoryCache();
            services.AddSingleton<IBindingResolver, BindingResolver>();

            return services
                .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                .AddSingleton<IEdmModelProvider, ODataModelSchemaTranslator>(services =>
                {
                    return new ODataModelSchemaTranslator(
                        services.GetRequiredService<IBindingResolver>(),
                        services.GetRequiredService<IRequestExecutorResolver>(), 
                        schemaName);
                })
                .AddGraphQLServer(schemaName)
                .AddConvention<INamingConventions, ODataGraphNamingConventions>();
        }

        // https://github.com/ChilliCream/hotchocolate/blob/ee5813646fdfea81035c681989793514f33b5d94/src/HotChocolate/AspNetCore/src/AspNetCore/Extensions/HotChocolateAspNetCoreServiceCollectionExtensions.Http.cs
        public static IEndpointConventionBuilder MapODataForGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            string odataPath = "/api",
            string graphPath = "/graphql",
            bool enableGraphEndpoint = false,
            NameString schemaName = default)
            => endpointRouteBuilder.MapODataForGraphQL(new PathString(odataPath), new PathString(graphPath), enableGraphEndpoint, schemaName);

        public static IEndpointConventionBuilder MapODataForGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            PathString odataPath,
            PathString graphPath,
            bool enableGraphEndpoint,
            NameString schemaName = default)
        {
            if (endpointRouteBuilder is null)
            {
                throw new ArgumentNullException(nameof(endpointRouteBuilder));
            }

            var pattern = Parse(odataPath.ToString().TrimEnd('/') + "/{**slug}");

            IApplicationBuilder requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
            var schemaNameOrDefault = schemaName.HasValue ? schemaName : Schema.DefaultName;

            requestPipeline
                .UseMiddleware<GraphMetadataMiddleware>(odataPath.ToString().TrimEnd('/'), schemaNameOrDefault)
                .UseMiddleware<GraphDataMiddleware>(schemaNameOrDefault)
                .Use(async Task (HttpContext context, Func<Task> next) =>
                {
                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "application/json";
                    var content = Encoding.UTF8.GetBytes("{ \"error\": \"Bad Request\" }");
                    await context.Response.Body.WriteAsync(content, 0, content.Length);
                    await context.Response.Body.FlushAsync();
                });

            if (enableGraphEndpoint)
            {
                endpointRouteBuilder
                    .MapGraphQL(schemaName: schemaName, path: graphPath)
                    .WithOptions(new GraphQLServerOptions()
                    {
                        EnableGetRequests = false,
                        EnableSchemaRequests = true,
                        EnableMultipartRequests = true,
                        Tool = {
                            Enable = false
                        }
                    });
            }

            return new GraphEndpointConventionBuilder(
                endpointRouteBuilder
                    .Map(pattern, requestPipeline.Build())
                    .WithDisplayName("OData for GraphQL Pipeline"));
        }
    }
}