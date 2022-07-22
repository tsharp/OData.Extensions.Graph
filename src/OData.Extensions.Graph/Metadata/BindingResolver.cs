using HotChocolate;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OData.Extensions.Graph.Metadata
{
    internal class BindingResolver : IBindingResolver
    {
        private static IDictionary<string, string[]> httpMethodPrefixes = new Dictionary<string, string[]>()
        {
            ["GET"] = new [] { "", "all" },
            ["POST"] = new[] { "", "create", "set", "change" },
            ["PATCH"] = new [] { "update", "set", "change" },
            ["DELETE"] = new [] { "delete", "remove" }
        };

        private readonly IDictionary<string, List<OperationBinding>> dynamicSchemaBindings = new ConcurrentDictionary<string, List<OperationBinding>>();
        private readonly ConcurrentBag<OperationBinding> defaultSchemaBindings = new ConcurrentBag<OperationBinding>();
        
        public void Register(OperationBinding binding, NameString schemaName = default)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            #region Default Schema Bindings
            if (schemaName == default && 
                defaultSchemaBindings.Where(s => s.EntitySet == binding.EntitySet).Any())
            {
                return;
                // throw new InvalidOperationException($"Duplicate Binding Registration For Schema: [{schemaName}] {binding.EntitySet}");
            }

            if (schemaName == default)
            {
                defaultSchemaBindings.Add(binding);
                return;
            }
            #endregion

            #region Dynamic Schema Bindings
            if (!dynamicSchemaBindings.ContainsKey(schemaName))
            {
                dynamicSchemaBindings.Add(schemaName, new List<OperationBinding>());
            }

            if(dynamicSchemaBindings[schemaName].Where(s => s.EntitySet == binding.EntitySet).Any())
            {
                // throw new InvalidOperationException($"Duplicate Binding Registration For Schema: [{schemaName}] {binding.EntitySet}");
                return;
            }

            dynamicSchemaBindings[schemaName].Add(binding);
            #endregion
        }

        // NOTE: These resolvers will be very expensive for large scale applications. This should be fixed such that
        // there should be no dynamic lookups - it should be a static binding and a report of the bindings should be
        // given. It should also be noted that a binding override option should be given in the event that the best
        // effort translation picks the wrong mutations - which would allow the selection of a more appropriate mutation
        // for things like entity sets, updates, creates, etc.
        public OperationBinding ResolveMutation(string method, NameString entitySet, NameString schemaName = default)
        {
            if(!httpMethodPrefixes.ContainsKey(method))
            {
                throw new InvalidOperationException($"Unsupported Method! `{method}`");
            }

            var prefixes = httpMethodPrefixes[method];

            foreach(var prefix in prefixes)
            {
                var operationName = $"{prefix}{entitySet}".RemovePluralization();
                var resolved = ResolveQuery(operationName, schemaName);

                if(resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }

        public OperationBinding ResolveQuery(NameString entitySet, NameString schemaName = default)
        {
            if (schemaName == null)
            {
                throw new ArgumentNullException(nameof(schemaName));
            }

            if(schemaName == default)
            {
                return defaultSchemaBindings.Where(b => b.EntitySet.Value.Equals(entitySet, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            }


            if (!dynamicSchemaBindings.ContainsKey(schemaName))
            {
                return null;
            }

            return dynamicSchemaBindings[schemaName].Where(b => b.EntitySet.Value.Equals(entitySet, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }
    }
}
