using Microsoft.OData.Edm;
using System;

namespace OData.Extensions.Graph.Lang
{
    internal static class EdmUtility
    {
        public static IEdmEntitySet GetEntitySet(this IEdmModel model, string entitySetName, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            IEdmEntitySet entitySet = model.GetEntitySetOrNull(entitySetName, false, comparison);

            if (entitySet == null)
            {
                throw new InvalidOperationException("Entity Set `" + entitySetName + "` not found in schema query");
            }

            return entitySet;
        }

        public static IEdmEntitySet GetEntitySetOrNull(this IEdmModel model, string entitySetName, bool allowLookupByType = false, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            foreach (IEdmEntityContainerElement element in model.EntityContainer.Elements)
            {
                if (element is IEdmEntitySet edmEntitySet &&
                    string.Compare(edmEntitySet.Name, entitySetName, comparison) == 0)
                {
                    return edmEntitySet;
                }
            }

            IEdmSchemaType refType = model.FindType(entitySetName);

            if (allowLookupByType && refType != null)
            {
                foreach (IEdmEntityContainerElement element in model.EntityContainer.Elements)
                {
                    if (element is IEdmEntitySet edmEntitySet)
                    {
                        var entitySet = element as IEdmEntitySet;
                        var elementType = entitySet.Type.AsElementType();

                        // TODO: Filter out invalid entity sets
                        if(elementType.FullTypeName() == refType.FullTypeName())
                        {
                            return entitySet;
                        }
                    }
                }
            }

            foreach (IEdmModel refModel in model.ReferencedModels)
            {
                if (refModel.EntityContainer != null && refModel is EdmModel)
                {
                    IEdmEntitySet entitySet = GetEntitySetOrNull(refModel, entitySetName);

                    if (entitySet != null)
                    {
                        return entitySet;
                    }
                }
            }

            return null;
        }

        public static IEdmProperty FindEdmProperty(IEdmStructuredType edmType, string name)
        {
            foreach (IEdmProperty edmProperty in edmType.Properties())
                if (string.Compare(edmProperty.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                    return edmProperty;

            throw new InvalidOperationException("Property " + name + " not found in edm type " + edmType.FullTypeName());
        }
    }
}
