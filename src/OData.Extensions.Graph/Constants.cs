using System.Text.Json;
using System.Text.Json.Serialization;

namespace OData.Extensions.Graph
{
    internal class Constants
    {
        public class Serialization
        {
            public static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };
        }
    }
}
