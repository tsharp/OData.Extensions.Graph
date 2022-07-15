using OData.Extensions.Graph.Lang;
using Snapshooter.Xunit;
using Xunit;

namespace OData.Extensions.Graph.Test.Lang
{
    public class BasicNeFilters
    {
        [Fact]
        public static void NeString()
        {
            // Arrange
            var translator = new QueryTranslator(Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=Id ne '1234'");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void NeInt()
        {
            // Arrange
            var translator = new QueryTranslator(Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=Age ne 100");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void NeDouble()
        {
            // Arrange
            var translator = new QueryTranslator(Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=Longitude ne 1.0");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void NeBool()
        {
            // Arrange
            var translator = new QueryTranslator(Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=IsActive ne true");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void NeNull()
        {
            // Arrange
            var translator = new QueryTranslator(Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=Id ne null");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }
    }
}
