using OData.Extensions.Graph.Lang;
using Snapshooter.Xunit;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace OData.Extensions.Graph.Test.Lang
{
    public class BasicNeFilters
    {
        [Fact]
        public static void NeEnum()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Status&$filter=Status ne 'Undefined'");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void NeString()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=Id ne '1234'");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void NeInt()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=Age ne 100");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void SpeedTest()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            Stopwatch timer = new Stopwatch();
            timer.Start();

            for(int idx = 0; idx <= 10000; idx++)
            {
                translator.Translate("/user?$select=Id&$filter=Longitude ne 1.0");
            }

            timer.Stop();

            var perConvert = timer.ElapsedMilliseconds / 10000.0;

            // Assert
            // This will be variable depending on the cpu speed / architecture
            double maxTime = .25;
            Assert.True(perConvert <= maxTime, $"Expected Conversion Avg to be less than {maxTime} ms per: {perConvert}");
        }

        [Fact]
        public static void NeDouble()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=Longitude ne 1.0");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void NeBool()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=IsActive ne true");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void NeNull()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=Id ne null");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }
    }
}
