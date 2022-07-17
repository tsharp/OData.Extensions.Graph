using GraphQLSample.Api.Dto;
using HotChocolate.Types;

namespace GraphQLSample.Api.Core
{
    public class ClassObjectType : ObjectType<Class>
    {
        protected override void Configure(IObjectTypeDescriptor<Class> descriptor)
        {
            descriptor.Ignore(x => x.HiddenField);
        }
    }
}
