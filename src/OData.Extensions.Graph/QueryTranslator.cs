using HotChocolate.Language;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OData.Extensions.Graph
{
    public class TranslatedQuery
    {
        public DocumentNode DocumentNode { get; set; }

        public string PathSegment { get; set; }
    }

    public class QueryTranslator
    {
        private readonly IEdmModel _edmModel;

        public QueryTranslator(IEdmModel model)
        {
            _edmModel = model;
        }

        public TranslatedQuery Translate(ODataUriParser parser)
        {
            // identifier
            ODataPath path = parser.ParsePath();
            var pathSegment = GetIdentifierFromSelectedPath(path);
            IEdmEntitySet entitySet = GetEntitySet(pathSegment);

            //handle select
            var selectClause = parser.ParseSelectAndExpand(); //parse $select, $expand
            var selectionSetNode = BuildFromSelectExpandClause(entitySet, selectClause);

            var querySelectionSet = new SelectionSetNode(new ISelectionNode[] {
                new FieldNode(
                    null,
                    new NameNode(entitySet.Name),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    selectionSetNode) });

            var queryOp = new OperationDefinitionNode(
               null,
               default,
               OperationType.Query,
               new VariableDefinitionNode[] { },
               new DirectiveNode[] { },
               querySelectionSet);

            var document = new DocumentNode(new IDefinitionNode[] { queryOp });

            return new TranslatedQuery()
            {
                DocumentNode = document,
                PathSegment = pathSegment
            };
        }

        public SelectionSetNode BuildFromSelectExpandClause(IEdmEntitySet entitySet, SelectExpandClause selectionClause)
        {
            var selections = new List<ISelectionNode>();

            if (selectionClause != null)
            {
                var selectedPaths = ParseSelectedPathsFromClause(selectionClause);
                foreach (var astNode in selectedPaths)
                {
                    if (astNode is ODataSelectPath fieldSelection)
                    {
                        var selectionName = GetIdentifierFromSelectedPath(fieldSelection);
                        IEdmProperty edmProperty = FindEdmProperty(entitySet.EntityType(), selectionName);

                        if (edmProperty.PropertyKind == EdmPropertyKind.Structural)
                        {
                            var fieldNode = new FieldNode(null, new NameNode(selectionName),
                                null, Array.Empty<DirectiveNode>(), Array.Empty<ArgumentNode>(), null);
                            selections.Add(fieldNode);
                        }
                        else if (edmProperty.PropertyKind == EdmPropertyKind.Navigation)
                        {
                            //var navigationProperty = (IEdmNavigationProperty)edmProperty;
                        }
                    }
                }
            }

            var selectionSet = new SelectionSetNode(selections);

            return selectionSet;
        }

        private string GetIdentifierFromSelectedPath(ODataSelectPath oDataPathSegment)
        {
            return (oDataPathSegment.FirstSegment == null) ? string.Empty : oDataPathSegment.FirstSegment.Identifier;
        }

        private string GetIdentifierFromSelectedPath(ODataPath oDataPathSegment)
        {
            return (oDataPathSegment.FirstSegment == null) ? string.Empty : oDataPathSegment.FirstSegment.Identifier;
        }

        private IEnumerable<ODataSelectPath> ParseSelectedPathsFromClause(SelectExpandClause clause)
        {
            IEnumerable<SelectItem> selectItems = clause.SelectedItems;

            // make sure that there are already selects for this level, otherwise we change the select semantics.
            bool anyPathSelectItems = selectItems.Any(x => x is PathSelectItem);

            // if there are selects for this level, then we need to add nav prop select items for each
            // expanded nav prop
            IEnumerable<ODataSelectPath> selectedPaths = selectItems.OfType<PathSelectItem>().Select(item => item.SelectedPath);

            //var res= selectedPaths.FirstOrDefault();
            return selectedPaths;
        }


        // Edm helpers 

        private IEdmEntitySet GetEntitySet(string selection)
        {
            // foreach (FieldType fieldType in _context.Schema.Query.Fields)
            //  if (String.Compare(fieldType.Name, selection, StringComparison.OrdinalIgnoreCase) == 0)
            return GetEntitySet(_edmModel, selection);

            throw new InvalidOperationException("Field name " + selection + " not found in schema query");
        }

        public static IEdmEntitySet GetEntitySet(IEdmModel edmModel, String entitySetName, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            IEdmEntitySet? entitySet = GetEntitySetOrNull(edmModel, entitySetName, comparison);
            if (entitySet == null)
                throw new InvalidOperationException();
            return entitySet;
        }

        public static IEdmEntitySet? GetEntitySetOrNull(IEdmModel edmModel, String entitySetName, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            foreach (IEdmEntityContainerElement element in edmModel.EntityContainer.Elements)
                if (element is IEdmEntitySet edmEntitySet &&
                    String.Compare(edmEntitySet.Name, entitySetName, comparison) == 0)
                    return edmEntitySet;

            foreach (IEdmModel refModel in edmModel.ReferencedModels)
                if (refModel.EntityContainer != null && refModel is EdmModel)
                {
                    IEdmEntitySet? entitySet = GetEntitySetOrNull(refModel, entitySetName);
                    if (entitySet != null)
                        return entitySet;
                }

            return null;
        }

        private static IEdmProperty FindEdmProperty(IEdmStructuredType edmType, String name)
        {
            foreach (IEdmProperty edmProperty in edmType.Properties())
                if (String.Compare(edmProperty.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                    return edmProperty;

            throw new InvalidOperationException("Property " + name + " not found in edm type " + edmType.FullTypeName());
        }
    }
}
