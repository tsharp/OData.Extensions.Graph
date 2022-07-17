using HotChocolate;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OData.Extensions.Graph.Metadata
{
    internal static class Extensions
    {
        public static bool TryGetCollectionType(this Type type, out Type collectionType)
        {
            string[] collections = new[]
            {
                nameof(IEnumerable),
                nameof(IOrderedQueryable),
                nameof(IQueryable),
                nameof(IList),
                typeof(IEnumerable<>).Name,
                typeof(IOrderedEnumerable<>).Name,
                typeof(IQueryable<>).Name,
                typeof(IOrderedQueryable<>).Name,
                typeof(IList<>).Name
            };

            if (type.IsArray)
            {
                collectionType = type.GetElementType();
                return true;
            }

            if (type.IsGenericType && type.GetInterfaces().Select(i => i.Name).Intersect(collections).Any())
            {
                collectionType = type.GenericTypeArguments[0];
                return true;
            }

            collectionType = null;
            return false;
        }

        public static bool IsCollectionType(this Type type)
        {
            string[] collections = new[]
            {
                nameof(IEnumerable),
                nameof(IOrderedQueryable),
                nameof(IQueryable),
                nameof(IList),
                typeof(IEnumerable<>).Name,
                typeof(IOrderedEnumerable<>).Name,
                typeof(IQueryable<>).Name,
                typeof(IOrderedQueryable<>).Name,
                typeof(IList<>).Name
            };

            if (type.IsArray)
            {
                return true;
            }

            if (type.IsGenericType && type.GetInterfaces().Select(i => i.Name).Intersect(collections).Any())
            {
                return true;
            }

            return false;
        }

        public static void BindEntitySet(this ODataModelBuilder builder, Type baseType, string entitySet)
        {
            MethodInfo entitySetInfo = typeof(ODataModelBuilder)
                .GetMethod("EntitySet", BindingFlags.Public | BindingFlags.Instance,
                null, new Type[] { typeof(string) }, null);

            dynamic result = entitySetInfo.MakeGenericMethod(baseType).Invoke(builder, new[] { entitySet });
        }

        public static void BindEntityType(this ODataModelBuilder builder, Type baseType)
        {
            MethodInfo entitySetInfo = typeof(ODataModelBuilder)
                .GetMethod("EntityType", BindingFlags.Public | BindingFlags.Instance,
                null, new Type[] { }, null);

            dynamic result = entitySetInfo.MakeGenericMethod(baseType).Invoke(builder, null);
        }
    }
}
