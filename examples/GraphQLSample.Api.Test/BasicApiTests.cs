using HotChocolate.Language;
using Microsoft.AspNetCore.Mvc.Testing;
using Snapshooter.Xunit;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace GraphQLSample.Api.Test
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-6.0
    /// </summary>
    public class BasicApiTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> factory;

        public BasicApiTests(WebApplicationFactory<Startup> factory)
        {
            this.factory = factory;
        }

        [Theory]
        [InlineData("/api/$metadata", HttpStatusCode.OK, "application/xml; charset=utf-8")]
        [InlineData("/api/$metadata/bad", HttpStatusCode.NotFound)]
        [InlineData("/api/$metadata/schema", HttpStatusCode.OK, "application/graphql; charset=utf-8")]
        public async Task GetAndVerifyResponseCode(string url, HttpStatusCode statusCode = HttpStatusCode.OK, string contentType = "application/json; charset=utf-8")
        {
            // Arrange
            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(statusCode, response.StatusCode);
            Assert.Equal(contentType,
                response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task CreateUser()
        {
            // Arrange
            var client = factory.CreateClient();
            var data = new Dictionary<string, object>()
            {
                ["name"] = "Bob"
            };

            var json = JsonSerializer.Serialize(data, OData.Extensions.Graph.Constants.Serialization.Reading);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("/api/users?$select=id", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}
