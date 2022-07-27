using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GraphQLSample.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        [HttpGet]
        public IHeaderDictionary EchoHeaders()
        {
            return Request.Headers;
        }
    }
}
