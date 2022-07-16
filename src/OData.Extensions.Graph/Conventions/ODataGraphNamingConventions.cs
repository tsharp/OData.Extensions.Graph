using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using System;
using System.Reflection;

namespace OData.Extensions.Graph.Conventions
{
    public class ODataGraphNamingConventions : DefaultNamingConventions
    {
        public override string GetMemberDescription(MemberInfo member, MemberKind kind)
        {
            var description = base.GetMemberDescription(member, kind);

            System.Diagnostics.Debug.WriteLine($"MEMBER_DESCRIPTION => {description}");

            if (description == "users")
            {

            }

            return description;
        }

        public override NameString GetMemberName(MemberInfo member, MemberKind kind)
        {
            var typeExtensionAttribute = member.DeclaringType.GetCustomAttribute<ExtendObjectTypeAttribute>();
            var objectTypeAttribute = member.DeclaringType.GetCustomAttribute<ObjectTypeAttribute>();

            var name = base.GetMemberName(member, kind);
            var namePrefix = string.Empty;

            System.Diagnostics.Debug.WriteLine($"MEMBER_NAME => {name}");

            if (typeExtensionAttribute != null || objectTypeAttribute != null)
            {
                namePrefix = $"pub_az_";
            }

            return $"{namePrefix}{name}";
        }
    }
}
