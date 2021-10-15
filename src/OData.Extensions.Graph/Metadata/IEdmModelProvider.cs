using Microsoft.AspNetCore.Http;
using Microsoft.OData.Edm;
using System.Threading.Tasks;

namespace OData.Extensions.Graph.Metadata
{
    public interface IEdmModelProvider
    {
        Task<IEdmModel> GetModelAsync(HttpRequest request);
        Task<string> GetModelCacheIdAsync(HttpRequest request);
    }
}
