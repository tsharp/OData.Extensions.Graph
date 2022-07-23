using GraphQLSample.Api.Dto;
using HotChocolate;
using HotChocolate.Subscriptions;
using OData.Extensions.Graph.Annotations;
using OData.Extensions.Graph.Security;
using System;
using System.Threading.Tasks;

namespace GraphQLSample.Api.Core
{
    [ApplyServiceNamespace]
    public class MutationObjectType
    {
        [AccessModifier(OperationAccessModifier.Public)]
        public async Task<User> CreateUser(string name)
        {
            await Task.CompletedTask;

            var user = new User()
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
            };

            return user;
        }

        [AccessModifier(OperationAccessModifier.Public)]
        public async Task<bool> DeleteUser(string id)
        {
            await Task.CompletedTask;

            return true;
        }

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