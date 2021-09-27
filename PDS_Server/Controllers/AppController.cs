using AspNetCore.Yandex.ObjectStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PDS_Server.Elements;
using PDS_Server.Repositories;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PDS_Server.Api
{
    [Route("api/application")]
    [ApiController]
    public class AppController : ControllerBase
    {
        private readonly IMongoRepository<DbApplication> _appRepository;
        private readonly YandexStorageService _yandexOptions;
        public AppController(IMongoRepository<DbApplication> appRepository, IOptions<YandexStorageOptions> yandexOptions)
        {
            _appRepository = appRepository;
            _yandexOptions = yandexOptions.Value.CreateYandexObjectService();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> List()
        {
            try
            {
                var list = (await _appRepository.Get()).ToList();
                return new OkObjectResult(list.Select(x => x.ToResponse()));
            }
            catch { return BadRequest(); }
        }

        [HttpGet]
        [Authorize(Roles = AccessGroups.APPROVED)]
        [Route("download/{name?}")]
        public async Task<IActionResult> Download(string name)
        {
            try
            {
                return File(await LoadFile(name), "application/octet-stream");
            }
            catch { return NotFound(); }
        }

        #region Private
        private async Task<Stream> LoadFile(string name)
        {
            return await _yandexOptions.GetAsStreamAsync($"Releases/{name}");
        }
        #endregion
    }
}
