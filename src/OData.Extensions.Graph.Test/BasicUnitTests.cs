using HotChocolate.Language;
using System;
using Xunit;

namespace OData.Extensions.Graph.Test
{
    public class BasicUnitTests
    {
        //private static IEdmModel GetModel()
        //{
        //    var builder = new GraphConventionModelBuilder();
        //    new ODataModelConfig().Apply(builder);

        //    //return builder.GetEdmModel();
        //}

        [Fact]
        public void RunSimpleTest()
        {
            //var model = GetModel();

            // To parser

            // To AST
        }

        [Fact]
        public void GraphQLAstGenerator()
        {
            //// arrange
            //var fragment = new FragmentDefinitionNode(
            //    null, new NameNode("foo"),
            //    Array.Empty<VariableDefinitionNode>(),
            //    new NamedTypeNode("foo"),
            //    Array.Empty<DirectiveNode>(),
            //    new SelectionSetNode(Array.Empty<ISelectionNode>()));

            //// act
            //var document = new DocumentNode(new IDefinitionNode[] { fragment });

            //var query = "{ foo(s: \"String\") { bar @foo " +
            //    "{ baz @foo @bar } } }";

            //var document = Utf8GraphQLParser.Parse(query);

            var variableDefinitions = new VariableDefinitionNode[]
            {
            };

            var directives = new DirectiveNode[]
            {
                // Adds query allFilms @films { }
                // new DirectiveNode(new NameNode("films"), Array.Empty<ArgumentNode>())
            };

            var selections = new ISelectionNode[]
            {
                new FieldNode(null, new NameNode("actor"), null, null, Array.Empty<DirectiveNode>(), Array.Empty<ArgumentNode>(), null),
                new FieldNode(null, new NameNode("createdOn"), null, null, Array.Empty<DirectiveNode>(), Array.Empty<ArgumentNode>(), null),
                new FieldNode(null, new NameNode("createdBy"), null, null, Array.Empty<DirectiveNode>(), Array.Empty<ArgumentNode>(), null),
                new FieldNode(null, new NameNode("things"), null, null, Array.Empty<DirectiveNode>(), Array.Empty<ArgumentNode>(), null)
            };

            var selectionSet = new SelectionSetNode(selections);

            var queryOp = new OperationDefinitionNode(
                null,
                new NameNode(null, "allFilms"),
                OperationType.Query,
                variableDefinitions,
                directives,
                selectionSet);

            var document = new DocumentNode(new IDefinitionNode[] { queryOp });

            var queryString = document.ToString(true);
        }
    }
}
