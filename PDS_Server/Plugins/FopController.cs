using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PDS_Server.Plugins
{
    [Route("api/fop")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class FopController : ControllerBase
    {
    }
}
