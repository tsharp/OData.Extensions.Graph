using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using OData.Extensions.Graph.Metadata;

namespace OData.Extensions.Graph.Test
{
    public class DebugBindingResolver : IBindingResolver
    {
        public static IBindingResolver Instance => new DebugBindingResolver();

        public DebugBindingResolver()
        {
        }

        public void Register(OperationBinding binding, NameString schemaName = default)
        {
        }

        public OperationBinding ResolveQuery(NameString entitySet, NameString schemaName = default)
        {
            return null;
        }

        public OperationBinding ResolveMutation(string method, NameString entitySet, NameString schemaName = default)
        {
            return null;
        }
    }
}
