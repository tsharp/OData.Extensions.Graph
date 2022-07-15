using GraphQLSample.Api.Dto;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace OData.Extensions.Graph.Test
{
    public static class Common
    {
        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder = builder.EnableLowerCamelCase(
                    NameResolverOptions.ProcessEnumMemberNames &
                    NameResolverOptions.ProcessReflectedPropertyNames &
                    NameResolverOptions.ProcessDataMemberAttributePropertyNames &
                    NameResolverOptions.ProcessExplicitPropertyNames);

            var entity = builder.EntityType<Entity>();
            entity.Abstract();

            var user = builder.EntitySet<User>("user");
            user.EntityType.DerivesFrom<Entity>();

            return builder.GetEdmModel();
        }
    }
}
