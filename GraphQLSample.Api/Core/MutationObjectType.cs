using GraphQLSample.Api.Dto;
using HotChocolate;
using HotChocolate.Subscriptions;
using System.Threading.Tasks;

namespace GraphQLSample.Api.Core
{
    public class MutationObjectType
    {
        public async Task<string> AddUser([Service] ITopicEventSender eventSender, User model)
        {
            // add your own logic to saving data into some data store.
            await eventSender.SendAsync(nameof(SubscriptionObjectType.SubscribeUser), model);
            return model.Id;
        }

        public async Task<string> AddClass([Service] ITopicEventSender eventSender, Class model)
        {
            // add your own logic to saving data into some data store.
            await eventSender.SendAsync(nameof(SubscriptionObjectType.SubscribeClass), model);
            return model.Id;
        }

        public async Task<string> AddConference([Service] ITopicEventSender eventSender, Conference model)
        {
            // add your own logic to saving data into some data store.
            await eventSender.SendAsync(nameof(SubscriptionObjectType.SubscribeConference), model);
            return model.Id;
        }
    }
}