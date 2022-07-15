using Microsoft.OData.UriParser;
using HotChocolate.Language;
using System;
using System.Collections.Generic;

namespace OData.Extensions.Graph.Lang
{
    public class GraphQueryNodeVisitor : QueryNodeVisitor<ISyntaxNode>
    {
        private Stack<string> currentOp = new Stack<string>();
        private Stack<string> currentPropName = new Stack<string>();

        #region Catch Alls
        public override ISyntaxNode Visit(AllNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(AnyNode nodeIn)
        {
            return base.Visit(nodeIn);
        }
        #endregion

        #region Entry
        public override ISyntaxNode Visit(BinaryOperatorNode nodeIn)
        {
            switch(nodeIn.OperatorKind)
            {
                case BinaryOperatorKind.NotEqual:
                    currentOp.Push("neq");
                    var neqLeft = nodeIn.Left.Accept(this);
                    var neqRight = nodeIn.Right.Accept(this);
                    return new ObjectValueNode(new ObjectFieldNode(currentPropName.Pop(), new ObjectValueNode((neqRight ?? neqLeft) as ObjectFieldNode)));
                case BinaryOperatorKind.Equal:
                    currentOp.Push("eq");
                    var eqLeft = nodeIn.Left.Accept(this);
                    var eqRight = nodeIn.Right.Accept(this);
                    return new ObjectValueNode(new ObjectFieldNode(currentPropName.Pop(), new ObjectValueNode((eqRight ?? eqLeft) as ObjectFieldNode)));
                default:
                    throw new NotSupportedException($"Unsupported $filter Operator: {nodeIn.OperatorKind}");
            }
        }
        #endregion

        #region Not Implemented
        public override ISyntaxNode Visit(ConstantNode nodeIn)
        {
            if (nodeIn.Value == null)
            {
                return new ObjectFieldNode(currentOp.Pop(), new NullValueNode(null));
            }

            if (nodeIn.Value is string)
            {
                return new ObjectFieldNode(currentOp.Pop(), nodeIn.Value as string);
            }

            if (nodeIn.Value is double)
            {
                return new ObjectFieldNode(currentOp.Pop(), (double)nodeIn.Value);
            }

            if (nodeIn.Value is bool)
            {
                return new ObjectFieldNode(currentOp.Pop(), (bool)nodeIn.Value);
            }

            if (nodeIn.Value is int)
            {
                return new ObjectFieldNode(currentOp.Pop(), (int)nodeIn.Value);
            }

            throw new InvalidOperationException($"Unsupported $filter Data Type: {nodeIn.Value?.GetType()} - {nodeIn.Value}");
        }

        public override ISyntaxNode Visit(AggregatedCollectionPropertyNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(CollectionComplexNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(CollectionConstantNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(CollectionFunctionCallNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(CollectionNavigationNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(CollectionOpenPropertyAccessNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(CollectionPropertyAccessNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(CollectionResourceCastNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(CollectionResourceFunctionCallNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(ConvertNode nodeIn)
        {
            return nodeIn.Source.Accept(this);
        }

        public override ISyntaxNode Visit(CountNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(InNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(NamedFunctionParameterNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(NonResourceRangeVariableReferenceNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(ParameterAliasNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(ResourceRangeVariableReferenceNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(SearchTermNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(SingleComplexNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(SingleNavigationNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(SingleResourceCastNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(SingleResourceFunctionCallNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(SingleValueCastNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(SingleValueFunctionCallNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(SingleValueOpenPropertyAccessNode nodeIn)
        {
            return base.Visit(nodeIn);
        }

        public override ISyntaxNode Visit(SingleValuePropertyAccessNode nodeIn)
        {
            currentPropName.Push(nodeIn.Property.Name);

            return null;
        }

        public override ISyntaxNode Visit(UnaryOperatorNode nodeIn)
        {
            return base.Visit(nodeIn);
        }
        #endregion
    }
}
