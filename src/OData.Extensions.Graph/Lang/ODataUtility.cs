using HotChocolate.Language;
using Microsoft.OData.UriParser;
using System;
using System.Linq;

namespace OData.Extensions.Graph.Lang
{
    internal static class ODataUtility
    {
        public static string GetIdentifierFromSelectedPath(ODataSelectPath oDataPathSegment)
        {
            return oDataPathSegment.FirstSegment == null ? string.Empty : oDataPathSegment.FirstSegment.Identifier;
        }

        public static string GetIdentifierFromSelectedPath(ODataPath oDataPathSegment)
        {
            return oDataPathSegment.FirstSegment == null ? string.Empty : oDataPathSegment.FirstSegment.Identifier;
        }

        public static ArgumentNode[] GetKeyArguments(ODataPath oDataPathSegment)
        {
            var segment = oDataPathSegment.ElementAtOrDefault(1) as KeySegment;

            if (segment == null)
            {
                return Array.Empty<ArgumentNode>();
            }

            var key = segment.Keys.SingleOrDefault();

            if (key.Value is int)
            {
                return new[] { new ArgumentNode(key.Key, (int)key.Value) };
            }

            return new[] { new ArgumentNode(key.Key, key.Value.ToString()) };
        }
    }
}
