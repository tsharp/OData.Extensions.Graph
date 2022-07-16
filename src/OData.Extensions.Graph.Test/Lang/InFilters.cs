
using HotChocolate.Execution;
using HotChocolate.Language;
using OData.Extensions.Graph.Lang;
using Snapshooter.Xunit;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace OData.Extensions.Graph.Test.Lang
{
    public class InFilters
    {
        [Fact]
        public static void InString()
        {
            var query = "{user(where: { Id: { in: [ \"Milk\", \"Cheese\" ] } }) { items { Id } } }";

            Utf8GraphQLParser.Parse(query).ToString(true).MatchSnapshot();

            // Arrange
            var translator = new QueryTranslator(Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=Id in ('Milk', 'Cheese')");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }
    }
}
