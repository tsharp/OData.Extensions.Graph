using HotChocolate.Language;
using Microsoft.OData;
using OData.Extensions.Graph.Lang;
using Snapshooter.Xunit;
using Xunit;

namespace OData.Extensions.Graph.Test.Lang
{
    public class AdvancedFilters
    {
        [Fact]
        public static void NestedPropertyEq()
        {
            var query = "{user(where: { ClassRef: { Id: { eq: \"Milk\" } } }) { items { Id } } }";

            Utf8GraphQLParser.Parse(query).ToString(true).MatchSnapshot();

            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=ClassRef/Id eq 'Milk'");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void DoubleNestedPropertyEq()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=ClassRef/In/Id eq 'Milk'");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void AndFilter()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=Id ne 'Milk' and Id ne 'Sour'");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void OrFilter()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=Id ne 'Milk' or Id ne 'Sour'");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void Skip()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$skip=5");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void Top()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$top=5");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void NavigationProperty()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$expand=Conferences($select=Id)");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void Count()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$count=true");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }


        [Fact]
        public static void ComplexFilter()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=Id ne 'Milk' or Id ne 'Sour' and (Id eq 'bob' or CreatedOn lt 2022-01-05)");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void ContainsFilter()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=contains(Id, 'Milk')");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void StartsWithFilter()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=startswith(Id, 'Milk')");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }

        [Fact]
        public static void NotStartsWithFilter()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            Assert.Throws<ODataException>(() =>
            {
                var filerByUserId = translator.Translate("/user?$select=Id&$filter=nstartswith(Id, 'Milk')");

                // Assert
                filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
            });
        }

        [Fact]
        public static void EndsWithFilter()
        {
            // Arrange
            var translator = new QueryTranslator(DebugBindingResolver.Instance, Common.GetEdmModel());

            // Act
            var filerByUserId = translator.Translate("/user?$select=Id&$filter=endswith(Id, 'Milk')");

            // Assert
            filerByUserId.DocumentNode.ToString(true).MatchSnapshot();
        }
    }
}
