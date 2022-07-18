using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using OData.Extensions.Graph.Annotations;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace OData.Extensions.Graph.Conventions
{
    public class ODataGraphNamingConventions : DefaultNamingConventions
    {
        private readonly ServiceNamespaceProvider serviceNamespaceProvider;

        public ODataGraphNamingConventions(ServiceNamespaceProvider serviceNamespaceProvider)
        {
            this.serviceNamespaceProvider = serviceNamespaceProvider;
        }

        public override NameString GetMemberName(MemberInfo member, MemberKind kind)
        {
            var accessModifier = member.GetCustomAttribute<AccessModifierAttribute>();
            var applyNamespace = member.DeclaringType.GetCustomAttribute<ApplyServiceNamespaceAttribute>() != null;
            var @namespace = serviceNamespaceProvider.ServiceName;

            if (accessModifier == null)
            {
                accessModifier = member.DeclaringType.GetCustomAttribute<AccessModifierAttribute>();
            }

            if( (!applyNamespace && accessModifier == null) ||
                (@namespace == default && accessModifier == null))
            {
                return base.GetMemberName(member, kind);
            }

            var nameBuilder = new StringBuilder();

            if (applyNamespace && @namespace != default)
            {
                nameBuilder.Append(@namespace);
                nameBuilder.Append("_");
            }

            if (accessModifier != null)
            {
                nameBuilder.Append(accessModifier);
                nameBuilder.Append("_");
            }

            nameBuilder.Append(base.GetMemberName(member, kind));

            return nameBuilder.ToString();
        }
    }
}
