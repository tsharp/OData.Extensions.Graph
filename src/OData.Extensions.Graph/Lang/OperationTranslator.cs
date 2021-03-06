using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using OData.Extensions.Graph.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OData.Extensions.Graph.Lang
{
    public class OperationTranslator
    {
        private readonly IEdmModel model;
        private readonly IBindingResolver bindingResolver;
        private readonly NameString schemaName;

        public OperationTranslator(
            IBindingResolver bindingResolver,
            IEdmModel model,
            NameString schemaName = default)
        {
            this.bindingResolver = bindingResolver;
            this.model = model;
            this.schemaName = schemaName;
        }

        public TranslatedOperation TranslateQuery(string query, params string[] requestArguments)
        {
            var parser = new ODataUriParser(model, new Uri($"{query}", UriKind.Relative));

            // Parse path and initial segment
            ODataPath path = parser.ParsePath();
            var pathSegment = ODataUtility.GetIdentifierFromSelectedPath(path);

            IEdmEntitySet entitySet = model.GetEntitySet(pathSegment);
            OperationBinding operationBinding = bindingResolver.ResolveQuery(entitySet.Name, schemaName);

            return TranslateQuery(
                parser,
                path,
                entitySet,
                operationBinding,
                requestArguments);
        }

        public TranslatedOperation TranslateQuery(
            ODataUriParser parser,
            ODataPath path,
            IEdmEntitySet entitySet,
            OperationBinding operationBinding,
            params string[] requestArguments)
            => Translate(
                OperationType.Query,
                parser,
                path,
                entitySet,
                operationBinding,
                true,
                requestArguments);

        public TranslatedOperation TranslateMutation(string query, params string[] requestVariables)
        {
            var parser = new ODataUriParser(model, new Uri($"{query}", UriKind.Relative));

            // Parse path and initial segment
            ODataPath path = parser.ParsePath();
            var pathSegment = ODataUtility.GetIdentifierFromSelectedPath(path);

            IEdmEntitySet entitySet = model.GetEntitySet(pathSegment);
            OperationBinding operationBinding = bindingResolver.ResolveQuery(entitySet.Name, schemaName);

            return TranslateMutation(
                parser,
                path,
                entitySet,
                operationBinding,
                requestVariables);
        }

        public TranslatedOperation TranslateMutation(
            ODataUriParser parser,
            ODataPath path,
            IEdmEntitySet entitySet,
            OperationBinding operationBinding,
            params string[] requestVariables)
            => Translate(
                OperationType.Mutation,
                parser,
                path,
                entitySet,
                operationBinding,
                false,
                requestVariables);

        private TranslatedOperation Translate(
            OperationType operationType,
            ODataUriParser parser,
            ODataPath path,
            IEdmEntitySet entitySet,
            OperationBinding operationBinding,
            bool allowFiltering,
            params string[] requestVariables)
        {
            var operation = new TranslatedOperation();
            operation.PathSegment = entitySet.Name;

            // Handle select
            var selectClause = parser.ParseSelectAndExpand(); //parse $select, $expand
            var filterClause = parser.ParseFilter();
            var skip = parser.ParseSkip();
            var top = parser.ParseTop();
            var count = parser.ParseCount();
            var orderBy = parser.ParseOrderBy();

            var operationName = operationBinding?.Operation ?? entitySet.Name;

            if (!allowFiltering)
            {
                var hasFilters = filterClause != null ||
                    skip != null ||
                    top != null ||
                    count != null ||
                    orderBy != null;

                if (hasFilters)
                {
                    throw new ODataException("Filter and ordering operators are not allowed.");
                }
            }

            if (selectClause == null || !selectClause.SelectedItems.Any())
            {
                throw new Microsoft.OData.ODataException("You must specify the $select or $expand clause with your request. One of these options must not be empty.");
            }

            var hasFiltering = false;

            // An operation binding has direct information regarding filtering
            if (operationBinding != null)
            {
                hasFiltering = operationBinding.CanFilter;
            }
            // An operation binding can't help here and we must try to make a best
            // effort to infer whether or not we have filtering available
            else
            {
                hasFiltering = filterClause != null;
            }

            var selectionSetNode = BuildFromSelectExpandClause(entitySet, false, hasFiltering, count.HasValue && count.Value, selectClause);

            var arguments = new List<ArgumentNode>();
            arguments.AddRange(ODataUtility.GetKeyArguments(path));

            foreach (var param in parser.CustomQueryOptions)
            {
                var paramClean = param.Value.Trim();

                if (paramClean.StartsWith("{") && paramClean.EndsWith("}"))
                {
                    var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(paramClean, Constants.Serialization.Reading);

                    // TODO: Better parsing?
                    var fields = obj.Select(field => new ObjectFieldNode(field.Key, field.Value.ToString())).ToArray();

                    arguments.Add(new ArgumentNode(param.Key, new ObjectValueNode(fields)));

                    continue;
                }

                arguments.Add(new ArgumentNode(param.Key, param.Value));
            }

            if (filterClause != null)
            {
                var filterArgument = filterClause.Expression.Accept(GraphQueryNodeVisitor.Instance) as ObjectFieldNode;
                arguments.Add(new ArgumentNode("where", new ObjectValueNode(filterArgument)));
            }

            if (skip.HasValue && skip.Value > 0)
            {
                arguments.Add(new ArgumentNode("skip", new IntValueNode(skip.Value)));
            }

            if (top.HasValue && top.Value > 0)
            {
                arguments.Add(new ArgumentNode("take", new IntValueNode(top.Value)));
            }

            var variables = new List<VariableDefinitionNode>();

            // Add variables from the body
            foreach (var variable in requestVariables)
            {
                var arg = operationBinding?.Arguments?.Where(arg => arg.Name == variable)?.SingleOrDefault();

                if (arg == null)
                {
                    throw new ODataException($"Argument Does Not Exist: `{variable}`");
                }

                var variableNode = new VariableNode(variable);
                arguments.Add(new ArgumentNode(variable, variableNode));
                variables.Add(new VariableDefinitionNode(null, variableNode, arg.Type.ToTypeNode(), null, Array.Empty<DirectiveNode>()));
            }

            var querySelectionSet = new SelectionSetNode(new ISelectionNode[] {
                new FieldNode(
                    null,
                    new NameNode(operationName),
                    null,
                    null,
                    Array.Empty<DirectiveNode>(),
                    arguments,
                    selectionSetNode) });

            var operationDefinition = new OperationDefinitionNode(
               null,
               requestVariables != null && requestVariables.Any() ? new NameNode(entitySet.Name) : default,
               operationType,
               variables.ToArray(),
               new DirectiveNode[] { },
               querySelectionSet);

            operation.DocumentNode = new DocumentNode(new IDefinitionNode[] { operationDefinition });

            return operation;
        }

        private SelectionSetNode BuildFromSelectExpandClause(IEdmEntitySet entitySet, bool allowFiltering, bool isFilterable, bool includeCount, SelectExpandClause selectionClause)
        {
            var selections = new List<ISelectionNode>();

            if (selectionClause != null)
            {
                var selectedPaths = ParseSelectedPathsFromClause(selectionClause);
                var expanded = ParseExpandedItems(selectionClause);

                foreach (var astNode in selectedPaths)
                {
                    if (astNode is ODataSelectPath fieldSelection)
                    {
                        var selectionName = ODataUtility.GetIdentifierFromSelectedPath(fieldSelection);
                        IEdmProperty edmProperty = EdmUtility.FindEdmProperty(entitySet.EntityType(), selectionName);

                        if (edmProperty.PropertyKind == EdmPropertyKind.Navigation)
                        {
                            continue;
                        }

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
                }

                foreach (var astExpand in expanded)
                {
                    if (!allowFiltering)
                    {
                        var hasFilters = astExpand.FilterOption != null ||
                            astExpand.TopOption != null ||
                            astExpand.SkipOption != null ||
                            astExpand.CountOption != null ||
                            astExpand.OrderByOption != null;

                        if (hasFilters)
                        {
                            throw new ODataException("Filter and ordering operators are not allowed.");
                        }
                    }

                    // TODO: Add navigation property filtering and cleanup this implementation a bit
                    // Any nested filtering or junk is explicitly not allowed.
                    var expandSelections = BuildFromSelectExpandClause(entitySet, false, false, false, astExpand.SelectAndExpand);

                    var selectionName = ODataUtility.GetIdentifierFromSelectedPath(astExpand.PathToNavigationProperty);
                    IEdmProperty edmProperty = EdmUtility.FindEdmProperty(entitySet.EntityType(), selectionName);

                    if (edmProperty.PropertyKind != EdmPropertyKind.Navigation)
                    {
                        continue;
                    }

                    var fieldNode = new FieldNode(
                        null,
                        new NameNode(selectionName),
                        null,
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<ArgumentNode>(),
                        expandSelections);

                    selections.Add(fieldNode);
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

                if (includeCount)
                {
                    itemsSelections.Add(new FieldNode(null,
                        new NameNode("totalCount"),
                        null,
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<ArgumentNode>(),
                        null));
                }

                return new SelectionSetNode(itemsSelections);
            }

            if (includeCount)
            {
                selections.Add(new FieldNode(null,
                    new NameNode("totalCount"),
                    null,
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null));
            }

            return new SelectionSetNode(selections);
        }

        private IEnumerable<ExpandedNavigationSelectItem> ParseExpandedItems(SelectExpandClause clause)
        {
            IEnumerable<SelectItem> selectItems = clause.SelectedItems;

            // make sure that there are already selects for this level, otherwise we change the select semantics.
            bool anyPathSelectItems = selectItems.Any(x => x is ExpandedNavigationSelectItem);

            // if there are selects for this level, then we need to add nav prop select items for each
            // expanded nav prop
            IEnumerable<ExpandedNavigationSelectItem> selectedPaths = selectItems.OfType<ExpandedNavigationSelectItem>();

            return selectedPaths;
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
