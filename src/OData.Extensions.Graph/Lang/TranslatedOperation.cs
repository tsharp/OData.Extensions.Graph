using HotChocolate.Language;

namespace OData.Extensions.Graph.Lang
{
    public class TranslatedOperation
    {
        public DocumentNode DocumentNode { get; set; }

        public string PathSegment { get; set; }
    }
}
