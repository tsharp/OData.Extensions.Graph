using HotChocolate;
using HotChocolate.Types;
using Microsoft.OData.ModelBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OData.Extensions.Graph.Metadata
{
    public enum OperationAccessModifier
    {
        Unknown,
        Public,
        Internal,
        System
    }

    public class OperationBinding
    {
        public bool IsSet { get; set; } = false;
        public bool CanFilter { get; set; } = false;
        public bool CanSort { get; set; } = false;
        public bool CanPage { get; set; } = false;
        public bool CanSelectSingle { get; set; } = false;
        public List<NameString> Arguments { get; set; } = new List<NameString>();
        public OperationAccessModifier AccessModifier { get; set; } = OperationAccessModifier.Unknown;
        public NameString EntityName { get; set; }
        public NameString Namespace { get; set; }
        public NameString EntitySet { get; set; }
        public NameString Operation { get; set; }

        private static OperationAccessModifier ParseModifier(string name)
        {
            var setNameParts = name.Split("_");

            if (setNameParts.Length < 2)
            {
                return OperationAccessModifier.Unknown;
            }

            switch(setNameParts.First())
            {
                case "pub":
                    return OperationAccessModifier.Public;
                case "int":
                    return OperationAccessModifier.Internal;
                case "sys":
                    return OperationAccessModifier.System;
                default:
                    return OperationAccessModifier.Unknown;
            }
        }

        private static NameString ParseNamespace(string name, bool useAccessModifiers)
        {
            var setNameParts = name.Split("_");

            if (useAccessModifiers && setNameParts.Length < 3)
            {
                return default;
            }

            return setNameParts[useAccessModifiers ? 1 : 0];
        }

        private static NameString ParseEntitySetName(string name, bool useNamespaces, bool useAccessModifiers)
        {
            var setNameParts = name.Split("_");
            var skip = 0;

            if (useNamespaces)
            {
                skip++;
            }

            if (useAccessModifiers)
            {
                skip++;
            }

            if (setNameParts.Length == 1)
            {
                return name;
            }

            if (setNameParts.Length < skip)
            {
                throw new InvalidOperationException($"[`{name}`] Unable to determine the entity set name. Please sure that you have your access modifiers and naming correctly set.");
            }

            return string.Join('_', setNameParts.Skip(skip));
        }

        public static OperationBinding Bind(ODataModelBuilder builder, ObjectField objectType, bool useNamespaces = false, bool useAccessModifiers = false)
        {
            var binding = new OperationBinding();
            var methodInfo = (objectType.Member ?? objectType.ResolverMember) as MethodInfo;
            var returnType = methodInfo.ReturnType;

            binding.Arguments.AddRange(objectType.Arguments.Select(arg => arg.Name));
            binding.CanPage = binding.Arguments.Any(arg => arg == "skip");
            binding.CanFilter = binding.Arguments.Any(arg => arg == "where");
            binding.CanSort = binding.Arguments.Any(arg => arg == "order");
            binding.CanSelectSingle = binding.Arguments.Any(arg => arg == "id" || arg == "key");

            if(!useAccessModifiers)
            {
                binding.AccessModifier = OperationAccessModifier.Public;
            }

            if (returnType.TryGetCollectionType(out Type entityType))
            {
                binding.EntityName = entityType.Name;
                binding.IsSet = true;

                if (useAccessModifiers)
                {
                    binding.AccessModifier = ParseModifier(objectType.Name);
                }

                if (useNamespaces)
                {
                    binding.Namespace = ParseNamespace(objectType.Name, useAccessModifiers);
                }

                binding.EntitySet = ParseEntitySetName(objectType.Name, useNamespaces, useAccessModifiers);
                binding.Operation = objectType.Name;

                builder.BindEntitySet(entityType, binding.EntitySet);

                return binding;
            }

            builder.BindEntityType(returnType);
            binding.EntityName = returnType.Name;
            return binding;
        }
    }
}
