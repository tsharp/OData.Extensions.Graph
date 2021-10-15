using Microsoft.OData.ModelBuilder;

namespace OData.Extensions.Graph
{
    public class GraphConventionModelBuilder : ODataConventionModelBuilder
    {
        // https://github.com/ChilliCream/hotchocolate/blob/124200ec58d21563e49f238330e8d294e186f1ec/src/HotChocolate/AspNetCore/src/AspNetCore/Extensions/EndpointRouteBuilderExtensions.cs
        // 
        public GraphConventionModelBuilder() : base()
        {
            this.EnableLowerCamelCase(
                NameResolverOptions.ProcessReflectedPropertyNames |
                NameResolverOptions.ProcessExplicitPropertyNames |
                NameResolverOptions.ProcessDataMemberAttributePropertyNames);
        }
    }
}
