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
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act

            // Assert
            Assert.Throws<ODataException>(() => translator.TranslateQuery("/user?$select"));
            Assert.Throws<ODataException>(() => translator.TranslateQuery("/user?$expand"));
            Assert.Throws<ODataException>(() => translator.TranslateQuery("/user?$select&$expand"));
        }

        [Fact]
        public static void EntitySetTranslation()
        {
            // Arrange
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var basic_selectUserId = translator.TranslateQuery("/user?$select=Id");

            // Assert
            basic_selectUserId.MatchSnapshot();
        }
    }
}
