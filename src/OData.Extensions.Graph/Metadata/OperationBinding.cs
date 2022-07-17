using HotChocolate;
using HotChocolate.Types;
using Microsoft.OData.ModelBuilder;
using OData.Extensions.Graph.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace OData.Extensions.Graph.Metadata
{
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

        public static void Bind(ODataModelBuilder builder, ObjectType objectType)
        {
            builder.BindEntityType(objectType.RuntimeType);
            var entityType = builder.StructuralTypes
                .OfType<EntityTypeConfiguration>()
                .Where(t => t.ClrType == objectType.RuntimeType)
                .Single();

            foreach (var property in objectType.RuntimeType.GetProperties())
            {
                var hasRemovable = property.GetCustomAttribute<GraphQLIgnoreAttribute>() != null ||
                        property.GetCustomAttribute<IgnoreDataMemberAttribute>() != null ||
                        property.GetCustomAttribute<NotMappedAttribute>() != null ||
                        property.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() != null ||
                        property.GetCustomAttribute<Newtonsoft.Json.JsonIgnoreAttribute>() != null;

                if (!hasRemovable && objectType.Fields.Where(f => f.ResolverMember == property).Any())
                {
                    continue;
                }

                entityType.RemoveProperty(property);
            }
        }

        public static OperationBinding Bind(ODataModelBuilder builder, ObjectField objectField, bool useNamespaces = false, bool useAccessModifiers = false)
        {
            var binding = new OperationBinding();
            var methodInfo = (objectField.Member ?? objectField.ResolverMember) as MethodInfo;
            var returnType = methodInfo.ReturnType;

            binding.Arguments.AddRange(objectField.Arguments.Select(arg => arg.Name));
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
                    binding.AccessModifier = ParseModifier(objectField.Name);
                }

                if (useNamespaces)
                {
                    binding.Namespace = ParseNamespace(objectField.Name, useAccessModifiers);
                }

                binding.EntitySet = ParseEntitySetName(objectField.Name, useNamespaces, useAccessModifiers);
                binding.Operation = objectField.Name;

                // This EntitySet shouldn't be exposed
                if (binding.EntitySet == default || binding.EntitySet == null)
                {
                    return null;
                }

                builder.BindEntitySet(entityType, binding.EntitySet);

                return binding;
            }

            return null;
        }
    }
}
