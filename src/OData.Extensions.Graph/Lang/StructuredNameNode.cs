using HotChocolate.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OData.Extensions.Graph.Lang
{
    public class StructuredNameNode : ISyntaxNode
    {
        private readonly string name;
        private readonly StructuredNameNode parent;

        public StructuredNameNode(string name, StructuredNameNode parent)
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
            if(parent != null)
            {
                return $"{parent.ToString()}.{name}";
            }

            return name;
        }

        public ObjectValueNode WrapNode(ObjectValueNode field)
        {
            var value = new ObjectValueNode(new ObjectFieldNode(name, field));

            if(parent == null)
            {
                return value;
            }

            return parent.WrapNode(value);
        }
    }
}
