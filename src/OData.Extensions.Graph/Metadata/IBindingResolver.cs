using HotChocolate;

namespace OData.Extensions.Graph.Metadata
{
    public interface IBindingResolver
    {
        OperationBinding Resolve(NameString entitySet, NameString schemaName = default);
        void Register(OperationBinding binding, NameString schemaName = default);
    }
}
