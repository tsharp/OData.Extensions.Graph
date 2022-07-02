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
                MaxDepth = 15,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
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
