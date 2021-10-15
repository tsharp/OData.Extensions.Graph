namespace GraphQLSample.Api.Dto
{
    public class Conference : Entity
    {
        public string Name { get; set; }

        public User[] Attendees { get; set; }
    }
}
