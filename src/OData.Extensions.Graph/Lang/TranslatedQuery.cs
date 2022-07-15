using HotChocolate.Language;

namespace OData.Extensions.Graph.Lang
{
    public class TranslatedQuery
    {
        public DocumentNode DocumentNode { get; set; }

        public string PathSegment { get; set; }
    }
}
