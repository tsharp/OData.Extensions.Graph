using HotChocolate;
using Microsoft.OData.Edm;

namespace Microsoft.AspNetCore.OData.Extensions.GraphQL.Metadata
{
    public interface ISchemaTranslator
    {
        IEdmModel Translate(ISchema schema);
    }
}
