using Microsoft.OData.UriParser;
using HotChocolate.Language;
using System;
using System.Collections.Generic;
using Microsoft.OData;
using System.Linq;

namespace OData.Extensions.Graph.Lang
{
    public class GraphQueryNodeVisitor : QueryNodeVisitor<ISyntaxNode>
    {
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
            string binaryOp = string.Empty;

            switch(nodeIn.OperatorKind)
            {
                case BinaryOperatorKind.GreaterThan:
                    binaryOp = "gt";
                    break;
                case BinaryOperatorKind.GreaterThanOrEqual:
                    binaryOp = "gte";
                    break;
                case BinaryOperatorKind.LessThan:
                    binaryOp = "lt";
                    break;
                case BinaryOperatorKind.LessThanOrEqual:
                    binaryOp = "lte";
                    break;
                case BinaryOperatorKind.NotEqual:
                    binaryOp = "neq";
                    break;
                case BinaryOperatorKind.Equal:
                    binaryOp = "eq";
                    break;
                case BinaryOperatorKind.Has:
                // Only Valid to Compare Enum Flags, Currently Unsupported
                default:
                    throw new NotSupportedException($"Unsupported $filter Operator: {nodeIn.OperatorKind}");
            }

            var left = nodeIn.Left.Accept(this);
            var right = nodeIn.Right.Accept(this);

            var leftValue = left as IValueNode;
            var rightValue = right as IValueNode;

            var leftName = left as StructuredNameNode;
            var rightName = right as StructuredNameNode;

            var filter = new ObjectFieldNode(binaryOp, rightValue ?? leftValue);
            var value = new ObjectValueNode(filter);

            return (leftName ?? rightName).WrapNode(value);

        }
        #endregion

        #region Not Implemented
        public override ISyntaxNode Visit(ConstantNode nodeIn)
        {
            if (nodeIn.Value == null)
            {
                return new NullValueNode(null);
            }

            if (nodeIn.Value is string)
            {
                return new StringValueNode(nodeIn.Value as string);
            }

            if (nodeIn.Value is double)
            {
                return new FloatValueNode((double)nodeIn.Value);
            }

            if (nodeIn.Value is bool)
            {
                return new BooleanValueNode((bool)nodeIn.Value);
            }

            if (nodeIn.Value is int)
            {
                return new IntValueNode((int)nodeIn.Value);
            }

            if (nodeIn.Value is DateTime || 
                nodeIn.Value is DateTimeOffset || 
                nodeIn.Value is Microsoft.OData.Edm.Date)
            {
                return new StringValueNode(nodeIn.Value.ToString());
            }

            if (nodeIn.Value is ODataEnumValue)
            {
                var value = (nodeIn.Value as ODataEnumValue).Value.ToUpper().Replace(" ", "_");

                return  new EnumValueNode(value);
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
            var values = nodeIn
                .Collection
                .Select(v => (IValueNode)v.Accept(this))
                .ToArray();

            return new ListValueNode(values);
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
            var left = nodeIn.Left.Accept(this);
            var right = nodeIn.Right.Accept(this);

            var leftValue = left as IValueNode;
            var rightValue = right as IValueNode;

            var leftName = left as StructuredNameNode;
            var rightName = right as StructuredNameNode;

            var filter = new ObjectFieldNode("in", rightValue ?? leftValue);
            var value = new ObjectValueNode(filter);

            return (leftName ?? rightName).WrapNode(value);
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
            StructuredNameNode parent = null;

            if (nodeIn.Source is SingleNavigationNode)
            {
                parent = nodeIn.Source.Accept(this) as StructuredNameNode;
            }

            return new StructuredNameNode(nodeIn.NavigationProperty.Name, parent as StructuredNameNode);
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
            StructuredNameNode parent = null;

            if (nodeIn.Source is SingleNavigationNode)
            {
                parent = nodeIn.Source.Accept(this) as StructuredNameNode;
            }

            return new StructuredNameNode(nodeIn.Property.Name, parent);
        }

        public override ISyntaxNode Visit(UnaryOperatorNode nodeIn)
        {
            return base.Visit(nodeIn);
        }
        #endregion
    }
}
