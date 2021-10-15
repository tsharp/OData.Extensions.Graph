using System.ComponentModel.DataAnnotations;

namespace GraphQLSample.Api.Dto
{
    public abstract class Entity
    {
        [Key]
        public string Id { get; set; }
    }
}
