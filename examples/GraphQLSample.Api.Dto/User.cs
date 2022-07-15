namespace GraphQLSample.Api.Dto
{
    public class User : Entity
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public bool IsActive { get; set; }
        public string EmailAddress { get; set; }
        public Conference[] Conferences { get; set; }
    }
}
