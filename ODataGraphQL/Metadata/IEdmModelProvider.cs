using Microsoft.AspNetCore.Http;
using Microsoft.OData.Edm;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.OData.Extensions.GraphQL.Metadata
{
    public interface IEdmModelProvider
    {
        Task<IEdmModel> GetModelAsync(HttpRequest request);
        Task<string> GetModelCacheIdAsync(HttpRequest request);
    }
}
