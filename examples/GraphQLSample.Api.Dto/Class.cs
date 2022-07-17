namespace GraphQLSample.Api.Dto
{
    public class Class : Entity
    {
        public string Name { get; set; }
        public User Instructor { get; set; }
        public User[] Students { get; set; }
        public string HiddenField { get; set; }
    }
}
