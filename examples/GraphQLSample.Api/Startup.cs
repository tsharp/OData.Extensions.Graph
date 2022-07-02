using GraphQLSample.Api.Core;
using GraphQLSample.Api.Dto;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Playground;
using HotChocolate.Types;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.OpenApi.Models;
using HotChocolate.Stitching;
using System;
using OData.Extensions.Graph;

namespace GraphQLSample.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry(Configuration);

            services
                .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"));

            services.AddAuthorization(options =>
            {
                // By default, all incoming requests will be authorized according to the default policy
                options.FallbackPolicy = options.DefaultPolicy;
            });

            services
                .AddRazorPages()
                .AddMvcOptions(options => { })
                .AddMicrosoftIdentityUI();

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TestGraphQLApi", Version = "v1" });
            });

            services.AddHttpClient("users", (sp, client) =>
            {
                client.BaseAddress = new Uri("https://rusty.azurewebsites.net/graphql");
            });

            services.AddHttpClient("jobs", (sp, client) =>
            {
                client.BaseAddress = new Uri("https://api.graphql.jobs");
            });

            services
                .AddODataForGraphQL("ODataGraph")
                .AddAuthorization()
                .AddInMemorySubscriptions()
                .AddRemoteSchema("jobs")
                .AddRemoteSchema("users");
                // .AddType<ObjectType<User>>()
                //.AddType<ObjectType<Class>>()
                //.AddType<ObjectType<Conference>>()
                // .AddSubscriptionType<SubscriptionObjectType>()
                // .AddMutationType<MutationObjectType>()
                // .AddQueryType<QueryObjectType>();

            services.AddCors(options =>
            {
                options.AddPolicy(name: "default",
                    builder =>
                    {
                        builder.AllowAnyOrigin();
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                    });

            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestGraphQLApi v1"));
                app.UsePlayground(new PlaygroundOptions
                {
                    QueryPath = "/graphql",
                    Path = "/playground"
                });
            }

            // app.UseHttpsRedirection();

            // 1.1 Use forwarded headers since containers are behind a proxy.
            // app.UseForwardedHeaders();

            // 1.2 Use configured request scheme
            // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-3.1
            app.Use((context, next) =>
            {
                var basePath = Configuration.GetValue("ServiceOptions:BasePath", "");

                if (!string.IsNullOrWhiteSpace(basePath))
                {
                    app.UsePathBase(basePath);
                }

                context.Request.Scheme = Configuration.GetValue("ServiceOptions:RequestScheme", "https");
                return next();
            });

            //app.Use(async (context, next) =>
            //{
            //    if(context.Request.Path.Value?.EndsWith(".well-known/microsoft-identity-association.json", System.StringComparison.InvariantCultureIgnoreCase) == false)
            //    {
            //        await next();
            //        return;
            //    }

            //    context.Response.StatusCode = 200;
            //    context.Response.ContentType = "application/json";
            //    var content = Encoding.UTF8.GetBytes("{ \"associatedApplications\": [{ \"applicationId\": \"76091f8b-03aa-4a42-a05b-c4ca7561b0bd\" }] }");
            //    await context.Response.Body.WriteAsync(content, 0, content.Length);
            //    await context.Response.Body.FlushAsync();
            //});

            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseRouting();
            app.UseCors("default");

            // app.UseAuthentication();
            // app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapODataForGraphQL(schemaName: "ODataGraph");
                endpoints.MapGraphQL();
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}
