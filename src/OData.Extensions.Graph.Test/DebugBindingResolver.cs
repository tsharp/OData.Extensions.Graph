using HotChocolate;
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

        public OperationBinding Resolve(NameString entitySet, NameString schemaName = default)
        {
            return null;
        }
    }
}
