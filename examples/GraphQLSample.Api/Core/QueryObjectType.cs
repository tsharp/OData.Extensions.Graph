using GraphQLSample.Api.Dto;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Data;
using HotChocolate.Types;
using System;
using System.Linq;

namespace GraphQLSample.Api.Core
{
    [ObjectType("Query")]
    public class QueryObjectType
    {
        [UseOffsetPaging(IncludeTotalCount = true, MaxPageSize = 100, DefaultPageSize = 20)]
        [UseFiltering]
        public IQueryable<User> GetUsers()
        {
            return (new User[] {
                new User()
                {
                    Id = Guid.NewGuid().ToString("N")
                }
            }).AsQueryable();
        }

        [Authorize(Roles = new[] { "X" })]
        public IQueryable<Class> GetClasses()
        {
            return (new Class[] {
                new Class()
                {
                    Id = Guid.NewGuid().ToString("N")
                }
            }).AsQueryable();
        }

        [Authorize(Roles = new[] { "Y" })]
        public IQueryable<Conference> GetConferences()
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