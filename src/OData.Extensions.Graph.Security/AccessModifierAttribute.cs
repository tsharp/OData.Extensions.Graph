using System;

namespace OData.Extensions.Graph.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AccessModifierAttribute : Attribute
    {
        public readonly OperationAccessModifier AccessModifier;

        public AccessModifierAttribute(OperationAccessModifier accessModifier)
        {
            AccessModifier = accessModifier;
        }

        public override string ToString()
        {
            switch (AccessModifier)
            {
                case OperationAccessModifier.Public:
                    return "pub";
                case OperationAccessModifier.Internal:
                    return "int";
                case OperationAccessModifier.System:
                    return "sys";
                default:
                    return string.Empty;
            }
        }
    }
}
