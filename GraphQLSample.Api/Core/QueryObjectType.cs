using GraphQLSample.Api.Dto;
using System;
using System.Linq;

namespace GraphQLSample.Api.Core
{
    public class QueryObjectType
    {
        public IQueryable<User> users()
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