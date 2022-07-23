using HotChocolate.Language;
using OData.Extensions.Graph.Lang;
using Snapshooter.Xunit;

namespace OData.Extensions.Graph.Test.Lang
{
    public class IdParameter
    {
        // [Fact]
        public static void SimpleIdParam()
        {
            var query = "{user(Id: { eq: \"abc\" }) { items { Id } } }";

            Utf8GraphQLParser.Parse(query).ToString(true).MatchSnapshot();

            // Arrange
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.TranslateQuery("/user('abc')?$select=Id");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }
    }
}
