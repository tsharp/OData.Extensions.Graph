using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OData.Extensions.Graph.Metadata
{
    public class ODataModelSchemaTranslator : ISchemaTranslator, IEdmModelProvider
    {
        private readonly IRequestExecutorResolver executorResolver;
        private readonly NameString schemaName;

        public ODataModelSchemaTranslator(
            IRequestExecutorResolver executorResolver,
            NameString schemaName = default)
        {
            this.schemaName = schemaName;
            this.executorResolver = executorResolver;
        }

        public async Task<IEdmModel> GetModelAsync(HttpRequest request)
        {
            var executor = await executorResolver.GetRequestExecutorAsync(schemaName, request.HttpContext.RequestAborted);

            using (MemoryStream stream = new MemoryStream())
            {
                await SchemaSerializer.SerializeAsync(executor.Schema, stream);

                var txt = Encoding.UTF8.GetString(stream.ToArray());
            }

            return Translate(executor.Schema);
        }

        public async Task<string> GetModelCacheIdAsync(HttpRequest request)
        {
            await Task.CompletedTask;
            /*
#if DEBUG
            return default;
#endif
            */

            var baseSchemaName = schemaName.HasValue ? schemaName.Value : "default";
            var subType = request.HttpContext.User.Identity.IsAuthenticated ? "anonymous" : "authenticated";
            return $"{baseSchemaName}.{subType}";
        }

        // TODO: Fix schema stitch where a downstream api attempts to stitch in a new type ... how to handle?
        public IEdmModel Translate(ISchema schema)
        {
            EdmModel model = new EdmModel(true);

            // This will parse and add models from the localized api, stitched schemas
            // need a separate pass on this.
            var local = ParseLocalSchema(new GraphConventionModelBuilder(), schema).GetEdmModel();
            var @delegate = ParseDelegateSchema(schema);

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
        private void BindEntitySet(ODataModelBuilder builder, Type baseType, string entitySet)
        {
            MethodInfo entitySetInfo = typeof(ODataModelBuilder)
                .GetMethod("EntitySet", BindingFlags.Public | BindingFlags.Instance,
                null, new Type[] { typeof(string) }, null);

            entitySetInfo.MakeGenericMethod(baseType).Invoke(builder, new[] { entitySet });
        }

        private void BindEntityType(ODataModelBuilder builder, Type baseType)
        {
            MethodInfo entitySetInfo = typeof(ODataModelBuilder)
                .GetMethod("EntityType", BindingFlags.Public | BindingFlags.Instance,
                null, new Type[] { }, null);

            entitySetInfo.MakeGenericMethod(baseType).Invoke(builder, null);
        }

        public bool TryGetCollectionType(Type type, out Type collectionType)
        {
            string[] collections = new[]
            {
                nameof(IEnumerable),
                nameof(IOrderedQueryable),
                nameof(IQueryable),
                nameof(IList),
                typeof(IEnumerable<>).Name,
                typeof(IOrderedEnumerable<>).Name,
                typeof(IQueryable<>).Name,
                typeof(IOrderedQueryable<>).Name,
                typeof(IList<>).Name
            };

            if (type.IsArray)
            {
                collectionType = type.GetElementType();
                return true;
            }

            if (type.IsGenericType && type.GetInterfaces().Select(i => i.Name).Intersect(collections).Any())
            {
                collectionType = type.GenericTypeArguments[0];
                return true;
            }

            collectionType = null;
            return false;
        }

        public bool IsCollectionType(Type type)
        {
            string[] collections = new[]
            {
                nameof(IEnumerable),
                nameof(IOrderedQueryable),
                nameof(IQueryable),
                nameof(IList),
                typeof(IEnumerable<>).Name,
                typeof(IOrderedEnumerable<>).Name,
                typeof(IQueryable<>).Name,
                typeof(IOrderedQueryable<>).Name,
                typeof(IList<>).Name
            };

            if (type.IsArray)
            {
                return true;
            }

            if (type.IsGenericType && type.GetInterfaces().Select(i => i.Name).Intersect(collections).Any())
            {
                return true;
            }

            return false;
        }

        private IEdmModel ParseDelegateSchema(ISchema schema)
        {
            var model = new EdmModel(true);
            var container = model.AddEntityContainer("Default", "Container");

            var unresolvedFields = new List<Tuple<EdmEntityType, ObjectField>>();

            // Process Reflection Only Remote Schema
            foreach (var objectType in schema.Types.OfType<ObjectType>()
                .Where(t =>
                    t.IsNamedType() &&
                    t.IsObjectType() &&
                    !t.Name.Value.StartsWith("__") &&
                    t.Name != schema.MutationType?.Name &&
                    t.Name != schema.QueryType?.Name))
            {
                var entityType = new EdmEntityType("Delegated.Entity", objectType.Name.Value);
                model.AddElement(entityType);

                foreach (var field in objectType.Fields.Where(f => !f.Name.Value.StartsWith("__")))
                {
                    var resolved = model.FindType(field.RuntimeType.Name);

                    if (resolved != null && (resolved as IEdmPrimitiveType) != null)
                    {
                        var primitive = resolved as IEdmPrimitiveType;
                        entityType.AddStructuralProperty(field.Name.Value, primitive.PrimitiveKind);
                        continue;
                    }

                    // TODO: Deeper inspection of properties to infer if it's a navigation property
                    // or if it's a complex type ...
                    unresolvedFields.Add(new Tuple<EdmEntityType, ObjectField>(entityType, field));
                }
            }

            foreach (var unresolved in unresolvedFields)
            {
                var resolved = model.FindType($"Delegated.Entity.{unresolved.Item2.Type.TypeName()}");

                if (resolved == null)
                {
                    resolved = model.FindType($"Delegated.Type.{unresolved.Item2.Type.TypeName()}");
                }

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
                    unresolved.Item1.AddStructuralProperty(unresolved.Item2.Name.Value, EdmPrimitiveTypeKind.None);
                }
            }

            // Now parse out and resolve entity sets
            foreach (var objectType in schema.QueryType.Fields
                .Where(f =>
                    f.Name.HasValue &&
                    !f.Name.Value.StartsWith("__") &&
                    f.Member == null))
            {
                var resolvedEntity = model.FindType($"Delegated.Entity.{objectType.Type.TypeName()}") as IEdmEntityType;

                if (resolvedEntity == null)
                {
                    continue;
                }

                if (IsCollectionType(objectType.RuntimeType))
                {
                    container.AddEntitySet(objectType.Name, resolvedEntity);
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unable to Map Entity Endpoint: {objectType.Name}");
                Console.ForegroundColor = ConsoleColor.White;
            }

            return model;
        }

        private ODataModelBuilder ParseLocalSchema(ODataModelBuilder builder, ISchema schema)
        {
            // Process Local Schema
            foreach (var objectType in schema.QueryType.Fields
                .Where(f =>
                    f.Name.HasValue &&
                    !f.Name.Value.StartsWith("__") &&
                    f.Member != null))
            {
                var methodInfo = objectType.Member as MethodInfo;

                var returnType = methodInfo.ReturnType;

                if (TryGetCollectionType(returnType, out Type entityType))
                {
                    BindEntitySet(builder, entityType, methodInfo.Name);
                    continue;
                }

                BindEntityType(builder, returnType);
            }

            return builder;
        }
    }
}
