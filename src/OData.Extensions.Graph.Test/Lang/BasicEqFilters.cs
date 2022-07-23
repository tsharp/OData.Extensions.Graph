using HotChocolate.Language;
using OData.Extensions.Graph.Lang;
using Snapshooter.Xunit;
using Xunit;

namespace OData.Extensions.Graph.Test.Lang
{
    public class BasicEqFilters
    {
        [Fact]
        public static void EqEnum()
        {
            // Arrange
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.TranslateQuery("/user?$select=Status&$filter=Status eq 'Undefined'");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void EqString()
        {
            // Arrange
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.TranslateQuery("/user?$select=Id&$filter=Id eq '1234'");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void EqDate()
        {
            // Arrange
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.TranslateQuery("/user?$select=Id&$filter=CreatedOn eq 2022-01-07");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void EqInt()
        {
            // Arrange
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.TranslateQuery("/user?$select=Id&$filter=Age eq 100");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void EqDouble()
        {
            // Arrange
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.TranslateQuery("/user?$select=Id&$filter=Longitude eq 1.0");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void EqBool()
        {
            // Arrange
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.TranslateQuery("/user?$select=Id&$filter=IsActive eq true");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void EqNull()
        {
            // Arrange
            var translator = new OperationTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.TranslateQuery("/user?$select=Id&$filter=Id eq null");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }
    }
}
