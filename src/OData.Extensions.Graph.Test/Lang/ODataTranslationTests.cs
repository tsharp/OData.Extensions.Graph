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
            Assert.Throws<ODataException>(() => translator.Translate("/user?$select", true));
            Assert.Throws<ODataException>(() => translator.Translate("/user?$expand", true));
            Assert.Throws<ODataException>(() => translator.Translate("/user?$select&$expand", true));
        }

        [Fact]
        public static void EntitySetTranslation()
        {
            // Arrange
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var basic_selectUserId = translator.Translate("/user?$select=Id", true);

            // Assert
            basic_selectUserId.MatchSnapshot();
        }
    }
}
