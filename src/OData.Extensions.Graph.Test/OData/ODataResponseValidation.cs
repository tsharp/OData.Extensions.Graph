using Microsoft.OData;
using Snapshooter.Xunit;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;

namespace OData.Extensions.Graph.Test.OData
{
    public class ODataResponseValidation
    {
        [Fact]
        public async void TestContext()
        {
            using (var stream = new MemoryStream())
            {
                // Arrange
                var response = new ODataResponse()
                    .WithContext($"https://localhost:5000/odata/v1/");

                // Act
                await response.WriteAsync(stream, CancellationToken.None);
                stream.Position = 0;
                var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                // Assert
                content.MatchSnapshot();
            }
        }

        [Fact]
        public async void TestCount()
        {
            using (var stream = new MemoryStream())
            {
                // Arrange
                var response = new ODataResponse()
                    .WithCount(10);

                // Act
                await response.WriteAsync(stream, CancellationToken.None);
                stream.Position = 0;
                var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                // Assert
                content.MatchSnapshot();
            }
        }

        [Fact]
        public async void TestNext()
        {
            using (var stream = new MemoryStream())
            {
                // Arrange
                var response = new ODataResponse()
                    .WithNext("https://localhost:5000/odata/v1/");

                // Act
                await response.WriteAsync(stream, CancellationToken.None);
                stream.Position = 0;
                var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                // Assert
                content.MatchSnapshot();
            }
        }

        [Fact]
        public async void TestProperties()
        {
            using (var stream = new MemoryStream())
            {
                // Arrange
                var response = new ODataResponse()
                    .WithProperties(new Dictionary<string, object>
                    {
                        { "foo", "bar" },
                        { "baz", "qux" }
                    });

                // Act
                await response.WriteAsync(stream, CancellationToken.None);
                stream.Position = 0;
                var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                // Assert
                content.MatchSnapshot();
            }
        }

        [Fact]
        public async void TestError()
        {
            using (var stream = new MemoryStream())
            {
                // Arrange
                var response = new ODataResponse()
                    .WithErrors(new object[]
                    {
                        new {
                            message = "set",
                            type = "InternalError"
                        }
                    });

                // Act
                await response.WriteAsync(stream, CancellationToken.None);
                stream.Position = 0;
                var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                // Assert
                content.MatchSnapshot();
            }
        }

        [Fact]
        public async void TestValues()
        {
            using (var stream = new MemoryStream())
            {
                // Arrange
                var response = new ODataResponse()
                    .WithValues(new object[]
                    {
                        new {
                            id = "1",
                            name = "foo"
                        },
                        new {
                            id = "2",
                            name = "bar"
                        }
                    });

                // Act
                await response.WriteAsync(stream, CancellationToken.None);
                stream.Position = 0;
                var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                // Assert
                content.MatchSnapshot();
            }
        }
    }
}
