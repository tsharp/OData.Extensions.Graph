using HotChocolate;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OData.Extensions.Graph.Metadata
{
    internal class BindingResolver : IBindingResolver
    {
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

        public OperationBinding Resolve(NameString entitySet, NameString schemaName = default)
        {
            if (schemaName == null)
            {
                throw new ArgumentNullException(nameof(schemaName));
            }

            if(schemaName == default)
            {
                return dynamicSchemaBindings[schemaName].Where(b => b.EntitySet == entitySet).FirstOrDefault();
            }


            if (!dynamicSchemaBindings.ContainsKey(schemaName))
            {
                return null;
            }

            return dynamicSchemaBindings[schemaName].Where(b => b.EntitySet == entitySet).FirstOrDefault();
        }
    }
}
