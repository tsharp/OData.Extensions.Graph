namespace GraphQLSample.Api.Dto
{
    public class User : Entity
    {
        public string Name { get; set; }
        public string EmailAddress { get; set; }
        public Conference[] Conferences { get; set; }
    }
}
