using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using OData.Extensions.Graph.Lang;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OData.Extensions.Graph.Metadata
{
    public class ODataModelSchemaTranslator : ISchemaTranslator, IEdmModelProvider
    {
        private readonly IRequestExecutorResolver executorResolver;
        private readonly NameString schemaName;
        private readonly IBindingResolver bindingResolver;
        private readonly IMemoryCache memoryCache;

        public ODataModelSchemaTranslator(
            IMemoryCache memoryCache,
            IBindingResolver bindingResolver,
            IRequestExecutorResolver executorResolver,
            NameString schemaName = default)
        {
            this.memoryCache = memoryCache;
            this.bindingResolver = bindingResolver;
            this.schemaName = schemaName;
            this.executorResolver = executorResolver;
        }

        public async Task<IEdmModel> GetModelAsync(HttpRequest request)
        {

            return await memoryCache.GetOrCreateAsync(await GetModelCacheIdAsync(request), async entry =>
            {
                var executor = await executorResolver.GetRequestExecutorAsync(schemaName, request.HttpContext.RequestAborted);

                using (MemoryStream stream = new MemoryStream())
                {
                    await SchemaSerializer.SerializeAsync(executor.Schema, stream);

                    var txt = Encoding.UTF8.GetString(stream.ToArray());
                }

                entry.SetValue(Translate(executor.Schema));

                return entry.Value as IEdmModel;
            });
            
        }

        public async Task<string> GetModelCacheIdAsync(HttpRequest request)
        {
            await Task.CompletedTask;

            var baseSchemaName = schemaName.HasValue ? schemaName.Value : "default";
            var subType = request.HttpContext.User.Identity.IsAuthenticated ? "anonymous" : "authenticated";
            return $"{baseSchemaName}.{subType}.{schemaName}";
        }

        // TODO: Fix schema stitch where a downstream api attempts to stitch in a new type ... how to handle?
        public IEdmModel Translate(ISchema schema)
        {
            EdmModel model = new EdmModel(true);
            var builder = new GraphConventionModelBuilder();
            // This will parse and add models from the localized api, stitched schemas
            // need a separate pass on this.
            var local = ParseLocalSchema(builder, schema).GetEdmModel();
            var @delegate = ParseDelegateSchema(builder.Namespace, local, schema);

            if (local.SchemaElements.Any())
            {
                model.AddElements(local.SchemaElements
                    .Where(k => k.SchemaElementKind != EdmSchemaElementKind.EntityContainer));
            }

            if (@delegate.SchemaElements.Any())
            {
                model.AddElements(@delegate.SchemaElements
                    .Where(k => k.SchemaElementKind != EdmSchemaElementKind.EntityContainer));
            }

            var container = model.AddEntityContainer("OData.Graph", "Container");

            MergeEntityContainers(container, local);
            MergeEntityContainers(container, @delegate);

            return model;
        }

        private void MergeEntityContainers(EdmEntityContainer container, IEdmModel source)
        {
            if (source.EntityContainer.Elements.Any())
            {
                foreach (var element in source.EntityContainer.Elements)
                {
                    container.AddElement(element);
                }
            }
        }

        private IEdmModel ParseDelegateSchema(string @localNamespace, IEdmModel localModel, ISchema schema)
        {
            var remoteNamespace = @localNamespace;
            var model = new EdmModel(true);
            var container = model.AddEntityContainer("Default", "Container");

            var unresolvedFields = new List<Tuple<EdmEntityType, ObjectField>>();

            // Process Reflection Only Remote Schema
            foreach (var objectType in schema.Types.OfType<ObjectType>()
                .Where(t =>
                    t.IsNamedType() &&
                    t.IsObjectType() &&
                    !t.Name.Value.StartsWith("_") &&
                    t.Name != schema.MutationType?.Name &&
                    t.Name != schema.QueryType?.Name))
            {
                // Attempt to locate existing ...
                var entityResolved = localModel.FindType($"{@localNamespace}.{objectType.Name.Value}");

                if (entityResolved != null || objectType.RuntimeType.FullName.StartsWith("HotChocolate."))
                {
                    continue;
                }

                var entityType = new EdmEntityType(remoteNamespace, objectType.Name.Value);
                model.AddElement(entityType);
                var keys = new List<IEdmStructuralProperty>();

                foreach (var field in objectType.Fields.Where(f => !f.Name.Value.StartsWith("_")))
                {
                    var resolved = model.FindType(field.RuntimeType.Name);

                    if (resolved != null && (resolved as IEdmPrimitiveType) != null)
                    {
                        var primitive = resolved as IEdmPrimitiveType;
                        var property = entityType.AddStructuralProperty(field.Name.Value, primitive.PrimitiveKind);

                        if (field.Name.Value.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                            field.Name.Value.Equals($"{objectType.Name.Value}id", StringComparison.OrdinalIgnoreCase) ||
                            field.Name.Value.Equals($"{objectType.Name.Value}_id", StringComparison.OrdinalIgnoreCase))
                        {
                            keys.Add(property);
                        }

                        continue;
                    }

                    // TODO: Deeper inspection of properties to infer if it's a navigation property
                    // or if it's a complex type ...
                    unresolvedFields.Add(new Tuple<EdmEntityType, ObjectField>(entityType, field));
                }

                if (keys.Any())
                {
                    entityType.AddKeys(keys);
                }
            }

            foreach (var unresolved in unresolvedFields)
            {
                var resolved = model.FindType($"{remoteNamespace}.{unresolved.Item2.Type.TypeName()}");

                var resolvedEntity = resolved as IEdmEntityType;
                var resolvedComplex = resolved as IEdmComplexType;

                if (unresolved.Item2.Type.IsComplexType() && resolvedComplex != null)
                {
                    unresolved.Item1.AddProperty(new EdmStructuralProperty(
                        unresolved.Item1,
                        unresolved.Item2.Name.Value, new EdmComplexTypeReference(resolvedComplex, true)));
                }
                else if (resolvedEntity != null)
                {
                    unresolved.Item1.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
                    {
                        Name = unresolved.Item2.Name,
                        Target = resolvedEntity,
                        TargetMultiplicity = unresolved.Item2.Type.IsListType() ?
                                                EdmMultiplicity.Many : EdmMultiplicity.One
                    });
                }
                else
                {
                    // Unknown
                    // unresolved.Item1.AddStructuralProperty(unresolved.Item2.Name.Value, EdmPrimitiveTypeKind.None);
                }
            }

            if (schema.QueryType != null)
            {
                // Now parse out and resolve entity sets
                foreach (var objectType in schema.QueryType.Fields
                    .Where(f =>
                        f.Name.HasValue &&
                        !f.Name.Value.StartsWith("_") &&
                        f.Member == null && f.ResolverMember == null))
                {
                    var resolvedEntity = model.FindType($"{remoteNamespace}.{objectType.Type.TypeName()}") as IEdmEntityType;

                    if (resolvedEntity == null)
                    {
                        continue;
                    }

                    if (objectType.RuntimeType.IsCollectionType() &&
                        objectType.Name.Value.IsPluralOf(resolvedEntity.Name))
                    {
                        container.AddEntitySet(objectType.Name, resolvedEntity);
                        continue;
                    }

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Unable to Map Entity Endpoint: {objectType.Name}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

            // Add mutations!
            if (schema.MutationType != null)
            {
                foreach (var objectField in schema.MutationType.Fields.Where(f =>
                         f.Name.HasValue &&
                         !f.Name.Value.StartsWith("_")))
                {
                    var name = $"{remoteNamespace}.{objectField.Type.TypeName()}";
                    var resolved = model.GetEntitySetOrNull(name, true);

                    if (resolved == null)
                    {
                        resolved = localModel.GetEntitySetOrNull(name, true);
                    }

                    if (resolved == null)
                    {
                        continue;
                    }

                    var binding = OperationBinding.Bind(objectField, resolved, true, true);

                    // Is General Mutation?
                    if (binding.EntitySet.Value.StartsWith("Delete", StringComparison.InvariantCultureIgnoreCase) ||
                        binding.EntitySet.Value.StartsWith("Remove", StringComparison.InvariantCultureIgnoreCase) ||
                        binding.EntitySet.Value.StartsWith("Update", StringComparison.InvariantCultureIgnoreCase) ||
                        binding.EntitySet.Value.StartsWith("Set", StringComparison.InvariantCultureIgnoreCase) ||
                        binding.EntitySet.Value.StartsWith("Create", StringComparison.InvariantCultureIgnoreCase) ||
                        binding.EntitySet.Value.StartsWith("New", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (binding != null)
                        {
                            bindingResolver.Register(binding, schemaName);
                        }
                    }
                }
            }

            if(schema.SubscriptionType != null)
            {

            }

            return model;
        }

        private ODataModelBuilder ParseLocalSchema(ODataModelBuilder builder, ISchema schema)
        {
            // Process Entities
            foreach (var objectType in schema.Types.Where(t =>
                    t.Kind == TypeKind.Object &&
                    t.Name.HasValue &&
                    !t.Name.Value.StartsWith("_") &&
                    t.GetType().GenericTypeArguments.Any()).Cast<ObjectType>())
            {
                // No
                if(objectType.RuntimeType.FullName.StartsWith("HotChocolate."))
                {
                    continue;
                }
                Debug.WriteLine(objectType.RuntimeType.FullName);
                OperationBinding.Bind(builder, objectType);
            }

            // Process Entity Sets
            foreach (var objectField in schema.QueryType.Fields
                .Where(f =>
                    f.Name.HasValue &&
                    !f.Name.Value.StartsWith("_") &&
                    (f.Member != null || f.ResolverMember != null)))
            {
                var binding = OperationBinding.Bind(builder, objectField, true, true);

                if (binding != null)
                {
                    bindingResolver.Register(binding, schemaName);
                }
            }

            return builder;
        }
    }
}
