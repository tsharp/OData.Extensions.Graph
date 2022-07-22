using HotChocolate;

namespace OData.Extensions.Graph
{
    public class ServiceNamespaceProvider
    {
        public readonly NameString ServiceName;
        public ServiceNamespaceProvider(NameString serviceName = default)
        {
            this.ServiceName = serviceName;
        }
    }
}
