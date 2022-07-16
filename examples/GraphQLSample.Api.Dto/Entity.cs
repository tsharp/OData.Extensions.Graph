using System;
using System.ComponentModel.DataAnnotations;

namespace GraphQLSample.Api.Dto
{
    public abstract class Entity
    {
        [Key]
        public string Id { get; set; }

        public DateTimeOffset CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTimeOffset ModifiedOn { get; set; } = DateTime.UtcNow;
        public DateTime LastAutomation { get; set; } = DateTime.MinValue;
    }
}
