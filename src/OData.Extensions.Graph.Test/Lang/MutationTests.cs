using Microsoft.OData;
using OData.Extensions.Graph.Lang;
using Snapshooter.Xunit;
using Xunit;

namespace OData.Extensions.Graph.Test.Lang
{
    public class MutationTests
    {
        [Fact]
        public static void SimpleUpdate()
        {
            // Arrange
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Status", false, "Id");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void SimpleUpdateWithFilter ()
        {
            // Arrange
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act & Assert
            Assert.Throws<ODataException>(() => translator.Translate("/user?$select=Status&$filter=Id eq null", false, "Id"));
        }
    }
}
