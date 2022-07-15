using GraphQLSample.Api.Dto;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using System;
using System.Linq;

namespace GraphQLSample.Api.Core
{
    [ObjectType("Query")]
    public class QueryObjectType
    {
        [Authorize(Roles = new[] { "X" })]
        public IQueryable<User> Users()
        {
            return (new User[] {
                new User()
                {
                    Id = Guid.NewGuid().ToString("N")
                }
            }).AsQueryable();
        }

        public IQueryable<Class> classes()
        {
            return (new Class[] {
                new Class()
                {
                    Id = Guid.NewGuid().ToString("N")
                }
            }).AsQueryable();
        }

        public IQueryable<Conference> conferences()
        {
            return (new Conference[] {
                new Conference()
                {
                    Id = Guid.NewGuid().ToString("N")
                }
            }).AsQueryable();
        }
    }
}