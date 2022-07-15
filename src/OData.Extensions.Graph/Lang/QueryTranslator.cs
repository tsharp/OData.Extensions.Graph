using HotChocolate.Language;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OData.Extensions.Graph.Lang
{
    public class QueryTranslator
    {
        private readonly IEdmModel model;

        public QueryTranslator(IEdmModel model)
        {
            this.model = model;
        }

        public TranslatedQuery Translate(string query)
        {
            return Translate(new ODataUriParser(model, new Uri($"{query}", UriKind.Relative)));
        }

        public TranslatedQuery Translate(ODataUriParser parser)
        {
            var query = new TranslatedQuery();

            // Identifier
            ODataPath path = parser.ParsePath();

            var pathSegment = ODataUtility.GetIdentifierFromSelectedPath(path);
            query.PathSegment = pathSegment;

            IEdmEntitySet entitySet = model.GetEntitySet(pathSegment);

            // Handle select
            var selectClause = parser.ParseSelectAndExpand(); //parse $select, $expand
            var filterClause = parser.ParseFilter();
            var skip = parser.ParseSkip();
            var top = parser.ParseTop();

            if (selectClause == null || !selectClause.SelectedItems.Any())
            {
                throw new Microsoft.OData.ODataException("You must specify the $select or $expand clause with your request. One of these options must not be empty.");
            }

            var selectionSetNode = BuildFromSelectExpandClause(entitySet, filterClause != null, selectClause);

            var arguments = new List<ArgumentNode>();
            arguments.AddRange(ODataUtility.GetKeyArguments(path));

            foreach (var param in parser.CustomQueryOptions)
            {
                var paramClean = param.Value.Trim();

                if (paramClean.StartsWith("{") && paramClean.EndsWith("}"))
                {
                    var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(paramClean, Constants.Serialization.Options);

                    // TODO: Better parsing?
                    var fields = obj.Select(field => new ObjectFieldNode(field.Key, field.Value.ToString())).ToArray();

                    arguments.Add(new ArgumentNode(param.Key, new ObjectValueNode(fields)));

                    continue;
                }

                arguments.Add(new ArgumentNode(param.Key, param.Value));
            }

            if (filterClause != null)
            {
                var visitor = new GraphQueryNodeVisitor();
                var filterArguments = filterClause.Expression.Accept(visitor) as ObjectValueNode;
                arguments.Add(new ArgumentNode("where", filterArguments));
            }

            var querySelectionSet = new SelectionSetNode(new ISelectionNode[] {
                new FieldNode(
                    null,
                    new NameNode(entitySet.Name),
                    null,
                    null,
                    Array.Empty<DirectiveNode>(),
                    arguments,
                    selectionSetNode) });

            var queryOp = new OperationDefinitionNode(
               null,
               default,
               OperationType.Query,
               new VariableDefinitionNode[] { },
               new DirectiveNode[] { },
               querySelectionSet);

            query.DocumentNode = new DocumentNode(new IDefinitionNode[] { queryOp });

            return query;
        }

        private SelectionSetNode BuildFromSelectExpandClause(IEdmEntitySet entitySet, bool isFilterable, SelectExpandClause selectionClause)
        {
            var selections = new List<ISelectionNode>();

            if (selectionClause != null)
            {
                var selectedPaths = ParseSelectedPathsFromClause(selectionClause);
                foreach (var astNode in selectedPaths)
                {
                    if (astNode is ODataSelectPath fieldSelection)
                    {
                        var selectionName = ODataUtility.GetIdentifierFromSelectedPath(fieldSelection);
                        IEdmProperty edmProperty = EdmUtility.FindEdmProperty(entitySet.EntityType(), selectionName);

                        if (edmProperty.PropertyKind == EdmPropertyKind.Structural)
                        {
                            var fieldNode = new FieldNode(
                                null,
                                new NameNode(selectionName),
                                null,
                                null,
                                Array.Empty<DirectiveNode>(),
                                Array.Empty<ArgumentNode>(),
                                null);
                            selections.Add(fieldNode);
                        }
                        else if (edmProperty.PropertyKind == EdmPropertyKind.Navigation)
                        {
                            //var navigationProperty = (IEdmNavigationProperty)edmProperty;
                        }
                    }
                }
            }

            if (isFilterable)
            {
                var itemsSelections = new List<ISelectionNode>()
                {
                    new FieldNode(
                        null,
                        new NameNode("items"),
                        null,
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<ArgumentNode>(),
                        new SelectionSetNode(selections))
                };

                return new SelectionSetNode(itemsSelections);
            }

            return new SelectionSetNode(selections);
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
    }
}
