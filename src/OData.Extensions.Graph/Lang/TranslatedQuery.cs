using HotChocolate.Language;
using Microsoft.OData;
using System.Collections.Generic;

namespace OData.Extensions.Graph.Lang
{
    public class TranslatedQuery
    {
        public DocumentNode DocumentNode { get; set; }

        public string PathSegment { get; set; }
    }
}
