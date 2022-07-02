using Microsoft.OData;
using OData.Extensions.Graph.Lang;
using Snapshooter.Xunit;
using Xunit;

namespace OData.Extensions.Graph.Test.Lang
{
    public class ODataTranslationTests
    {
        [Fact]
        public static void EmptySelectClause()
        {
            // Arrange
            var translator = new QueryTranslator(Common.GetEdmModel());

            // Act

            // Assert
            Assert.Throws<ODataException>(() => translator.Translate("/user?$select"));
            Assert.Throws<ODataException>(() => translator.Translate("/user?$expand"));
            Assert.Throws<ODataException>(() => translator.Translate("/user?$select&$expand"));
        }

        [Fact]
        public static void EntitySetTranslation()
        {
            // Arrange
            var translator = new QueryTranslator(Common.GetEdmModel());

            // Act
            var basic_selectUserId = translator.Translate("/user?$select=Id");

            // Assert
            Snapshot.Match(basic_selectUserId);
        }
    }
}
