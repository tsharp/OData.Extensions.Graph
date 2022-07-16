using HotChocolate.Language;
using Microsoft.OData;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OData.Extensions.Graph.Lang
{
    public class GraphQueryNodeVisitor : QueryNodeVisitor<ISyntaxNode>
    {
        private static IReadOnlyDictionary<string, string> functionMap = new Dictionary<string, string>()
        {
            ["contains"] = "contains",
            ["ncontains"] = "ncontains",
            ["startswith"] = "startsWith",
            ["nstartswith"] = "nstartsWith",
            ["endswith"] = "endsWith",
            ["nendswith"] = "nendsWith"
        };

        #region Entry
        public override ISyntaxNode Visit(BinaryOperatorNode nodeIn)
        {
            string binaryOp = string.Empty;
            bool isNestedBinaryOp = false;
            switch (nodeIn.OperatorKind)
            {
                case BinaryOperatorKind.Equal:
                    binaryOp = "eq";
                    break;
                case BinaryOperatorKind.NotEqual:
                    binaryOp = "neq";
                    break;
                case BinaryOperatorKind.And:
                    isNestedBinaryOp = true;
                    binaryOp = "and";
                    break;
                case BinaryOperatorKind.Or:
                    isNestedBinaryOp = true;
                    binaryOp = "or";
                    break;
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
                case BinaryOperatorKind.Has:
                // Only Valid to Compare Enum Flags, Currently Unsupported
                default:
                    throw new NotSupportedException($"Unsupported $filter Operator: {nodeIn.OperatorKind}");
            }

            var left = nodeIn.Left.Accept(this);
            var right = nodeIn.Right.Accept(this);

            if (!isNestedBinaryOp)
            {
                var leftValue = left as IValueNode;
                var rightValue = right as IValueNode;

                var leftName = left as StructuredNameNode;
                var rightName = right as StructuredNameNode;

                var filter = new ObjectFieldNode(binaryOp, rightValue ?? leftValue);
                var value = new ObjectValueNode(filter);

                return (leftName ?? rightName).WrapNode(value);
            }

            var leftField = left as ObjectFieldNode;
            var rightField = right as ObjectFieldNode;

            var complexValue = new ObjectValueNode(leftField, rightField);
            return new StructuredNameNode(binaryOp).WrapNode(complexValue);
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

                return new EnumValueNode(value);
            }

            throw new InvalidOperationException($"Unsupported $filter Data Type: {nodeIn.Value?.GetType()} - {nodeIn.Value}");
        }

        public override ISyntaxNode Visit(CollectionConstantNode nodeIn)
        {
            var values = nodeIn
                .Collection
                .Select(v => (IValueNode)v.Accept(this))
                .ToArray();

            return new ListValueNode(values);
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

        public override ISyntaxNode Visit(SingleNavigationNode nodeIn)
        {
            StructuredNameNode parent = null;

            if (nodeIn.Source is SingleNavigationNode)
            {
                parent = nodeIn.Source.Accept(this) as StructuredNameNode;
            }

            return new StructuredNameNode(nodeIn.NavigationProperty.Name, parent as StructuredNameNode);
        }

        public override ISyntaxNode Visit(ConvertNode nodeIn)
        {
            return nodeIn.Source.Accept(this);
        }

        public override ISyntaxNode Visit(SingleValueFunctionCallNode nodeIn)
        {
            var functionName = functionMap
                .FirstOrDefault(m => m.Key.Equals(nodeIn.Name, StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrWhiteSpace(functionName.Value))
            {
                throw new InvalidOperationException($"Unknown function call: `{nodeIn.Name}`");
            }

            var parameters = nodeIn.Parameters.Select(p => p.Accept(this)).ToArray();

            var left = parameters[0] as StructuredNameNode;
            var right = parameters[1] as IValueNode;

            if (left == null || right == null)
            {
                throw new InvalidOperationException($"The first argument of `{nodeIn.Name}` must be a property.");
            }

            var filter = new ObjectFieldNode(functionName.Value, right);
            var value = new ObjectValueNode(filter);

            return left.WrapNode(value);

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
        #endregion
    }
}
