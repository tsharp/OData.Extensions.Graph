using HotChocolate;

namespace OData.Extensions.Graph.Metadata
{
    public interface IBindingResolver
    {
        OperationBinding ResolveQuery(NameString entitySet, NameString schemaName = default);
        OperationBinding ResolveMutation(string method, NameString entitySet, NameString schemaName = default);
        void Register(OperationBinding binding, NameString schemaName = default);
    }
}
