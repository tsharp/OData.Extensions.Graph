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
                MaxDepth = 8,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
#if DEBUG
                WriteIndented = true,
#else
                WriteIndented = false,
#endif
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };
        }
    }
}
