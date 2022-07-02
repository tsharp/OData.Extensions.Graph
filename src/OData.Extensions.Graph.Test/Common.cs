using GraphQLSample.Api.Dto;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace OData.Extensions.Graph.Test
{
    public static class Common
    {
        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataModelBuilder();
            var entity = builder.EntityType<Entity>();
            entity.Abstract();

            var user = builder.EntitySet<User>("user");
            user.EntityType.DerivesFrom<Entity>();
            user.EntityType.HasKey(e => e.Id);
            user.EntityType.Property(e => e.EmailAddress);
            user.EntityType.Property(e => e.Name);

            return builder.GetEdmModel();
        }
    }
}
