﻿using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace OData.Extensions.Graph.Test.Api
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-6.0
    /// </summary>
    public class BasicApiTests : IClassFixture<WebApplicationFactory<GraphQLSample.Api.Startup>>
    {
        private readonly WebApplicationFactory<GraphQLSample.Api.Startup> factory;

        public BasicApiTests(WebApplicationFactory<GraphQLSample.Api.Startup> factory)
        {
            this.factory = factory;
        }

        [Theory]
        [InlineData("/api/$metadata")]
        public async Task GetAndVerifyResponseCode(string url, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            // Arrange
            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(statusCode, response.StatusCode);
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
        }
    }
}
