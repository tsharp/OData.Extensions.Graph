using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OData.Extensions.Graph
{
    internal class ODataResponse
    {
        private readonly IDictionary<string, object> responseData = new Dictionary<string, object>();
        private int statusCode = 200;
        
        public ODataResponse WithContext(string value)
        {
            responseData.Add("@odata.context", value);

            return this;
        }

        public ODataResponse WithCount(long value)
        {
            responseData.Add("@odata.count", value);

            return this;
        }

        public ODataResponse WithNext(string value)
        {
            responseData.Add("@odata.next", value);

            return this;
        }

        public ODataResponse WithError(string value)
        {
            responseData.Add("error", value);

            return this;
        }

        public ODataResponse WithErrors<T>(IEnumerable<T> values)
        {
            responseData.Add("errors", values);

            return this;
        }

        public ODataResponse WithValues<T>(IEnumerable<T> value)
        {
            responseData.Add("value", value);

            return this;
        }

        public ODataResponse WithProperties(IDictionary<string, object> properties)
        {
            foreach (var property in properties)
            {
                responseData.Add(property.Key, property.Value);
            }

            return this;
        }

        public ODataResponse WithDebug(string name, object value)
        {
            responseData.Add($"@odata.debug.{name}", value);

            return this;
        }

        public ODataResponse WithStatusCode(int statusCode)
        {
            this.statusCode = statusCode;

            return this;
        }

        public async Task WriteAsync(HttpResponse response, CancellationToken cancellationToken)
        {
            response.ContentType = "application/json; charset=utf-8";
            response.StatusCode = statusCode;

            await JsonSerializer.SerializeAsync(
                response.Body,
                responseData,
                typeof(object),
                Constants.Serialization.Writing,
                cancellationToken);
        }
    }
}
