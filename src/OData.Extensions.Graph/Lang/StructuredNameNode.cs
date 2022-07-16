using HotChocolate.Language;
using System;
using System.Collections.Generic;

namespace OData.Extensions.Graph.Lang
{
    public class StructuredNameNode : ISyntaxNode
    {
        private readonly string name;
        private readonly StructuredNameNode parent;

        public StructuredNameNode(string name, StructuredNameNode parent = null)
        {
            this.parent = parent;
            this.name = name;
        }

        public SyntaxKind Kind => SyntaxKind.Name;

        public Location Location => null;

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            throw new NotImplementedException();
        }

        public string ToString(bool indented)
        {
            if (parent != null)
            {
                return $"{parent.ToString()}.{name}";
            }

            return name;
        }

        public ObjectFieldNode WrapNode(ObjectValueNode value)
        {
            var field = new ObjectFieldNode(name, value);

            if (parent == null)
            {
                return field;
            }

            return parent.WrapNode(new ObjectValueNode(field));
        }
    }
}
