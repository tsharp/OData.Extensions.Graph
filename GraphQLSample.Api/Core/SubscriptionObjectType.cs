using GraphQLSample.Api.Dto;
using HotChocolate;
using HotChocolate.Types;

namespace GraphQLSample.Api.Core
{
    public class SubscriptionObjectType
    {

        [Topic]
        [Subscribe]
        public User SubscribeUser([EventMessage] User user)
        {
            return user;
        }

        [Topic]
        [Subscribe]
        public Class SubscribeClass([EventMessage] Class @class)
        {
            return @class;
        }

        [Topic]
        [Subscribe]
        public Conference SubscribeConference([EventMessage] Conference conference)
        {
            return conference;
        }
    }
}