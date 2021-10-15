using HotChocolate;
using Microsoft.OData.Edm;

namespace OData.Extensions.Graph.Metadata
{
    public interface ISchemaTranslator
    {
        IEdmModel Translate(ISchema schema);
    }
}
