using GraphQLSample.Api.Core;
using GraphQLSample.Api.Dto;
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
using OData.Extensions.Graph;
using System;

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

            //services.AddHttpClient("jobs", (sp, client) =>
            //{
            //    client.BaseAddress = new Uri("https://swapi-graphql.netlify.app/.netlify/functions/index");
            //});

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

            services
                .AddODataForGraphQL("ODataGraph", "test")
                .AddAuthorization()
                .AddInMemorySubscriptions()
                // TODO: More testing and dynamic configuration with remote schemas
                // for the time being this api fully supports HotChocolate based GraphQL
                // api's
                // .AddRemoteSchema("jobs")
                .AddFiltering()
                .AddSorting()
                // .AddQueryType()
                .AddType<ObjectType<User>>()
                .AddType<ObjectType<Class>>()
                .AddType<ObjectType<Conference>>()
                // .AddSubscriptionType<SubscriptionObjectType>()
                // .AddMutationType<MutationObjectType>()
                .AddQueryType<QueryObjectType>();

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
            }

            // app.UseHttpsRedirection();

            // 1.1 Use forwarded headers since containers are behind a proxy.
            app.UseForwardedHeaders();

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

            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseRouting();
            app.UseCors("default");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapODataForGraphQL(schemaName: "ODataGraph", enableGraphEndpoint: true);
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}
