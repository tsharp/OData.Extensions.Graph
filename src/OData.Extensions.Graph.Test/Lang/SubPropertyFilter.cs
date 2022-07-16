using HotChocolate.Language;
using OData.Extensions.Graph.Lang;
using Snapshooter.Xunit;
using Xunit;

namespace OData.Extensions.Graph.Test.Lang
{
    public class SubPropertyFilter
    {
        [Fact]
        public static void NestedPropertyEq()
        {
            var query = "{user(where: { ClassRef: { Id: { eq: \"Milk\" } } }) { items { Id } } }";

            Utf8GraphQLParser.Parse(query).ToString(true).MatchSnapshot();

            // Arrange
            var translator = new QueryTranslator(Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=ClassRef/Id eq 'Milk'");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }
    }
}
