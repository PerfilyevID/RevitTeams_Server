using AspNetCore.Yandex.ObjectStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PDS_Server.Elements;
using PDS_Server.Repositories;
using PDS_Server.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PDS_Server.Controllers
{
    [ApiController]
    [Route("")]
    public class HomeController : Controller
    {
        private readonly IMongoRepository<DbApplication> _applicationRepository;
        private readonly IMongoRepository<DbPlugin> _pluginRepository;
        private readonly IMongoRepository<DbNews> _newsRepository;
        private readonly YandexStorageService _yandexOptions;
        private readonly IBot _bot;
        public HomeController(IBot bot, IMongoRepository<DbNews> newsRepository, IMongoRepository<DbPlugin> pluginRepository, IMongoRepository<DbApplication> applicationRepository, IOptions<YandexStorageOptions> yandexOptions)
        {
            _bot = bot;
            _pluginRepository = pluginRepository;
            _applicationRepository = applicationRepository;
            _newsRepository = newsRepository;
            _yandexOptions = yandexOptions.Value.CreateYandexObjectService();
        }
        [Route("/")]
        [Route("index")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var appData = (await _applicationRepository.Get()).ToList();
            appData.Reverse();
            ViewBag.Applications = appData;
            var pluginData = (await _pluginRepository.Get());
            pluginData.Reverse();
            ViewBag.Plugins = pluginData.ToList();
            ViewBag.News = (await _newsRepository.Get()).ToList();
            return View("index");
        }

        [HttpGet]
        [Authorize(Roles = AccessGroups.APPROVED)]
        [Route("download/{name?}")]
        public async Task<IActionResult> Download(string name)
        {
            try
            {
                Stream stream = await LoadFile(name);
                if(stream != null) { return File(stream, "application/octet-stream"); }
            }
            catch (Exception e) { await _bot.SendException(e); }
            return NotFound();
        }

        [HttpPost]
        [Authorize(Roles = AccessGroups.ADMIN)]
        [Route("addversion")]
        public async Task<IActionResult> AddVersion([FromForm] IFormFile file, [FromForm] string version, [FromForm] string changelog)
        {
            if (file != null && !string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(changelog))
            {
                if (file.FileName.Split('.').Last().ToLower() == "zip")
                {
                    DbApplication application = new DbApplication() { Version = version, Changelog = changelog };
                    application.Id = await _applicationRepository.Create(application);
                    await SaveFile(file, application);
                }
            }
            return RedirectToAction("index", "home");
        }

        [HttpGet]
        [Authorize(Roles = AccessGroups.ADMIN)]
        [Route("deleteversion/{id?}")]
        public async Task<IActionResult> DeleteVersion(string id)
        {
            DbApplication application = await _applicationRepository.Get(new MongoDB.Bson.ObjectId(id));
            if (application != null)
            {
                await _applicationRepository.Delete(application.Id);
                await DeleteFile(application);
            }   
            return RedirectToAction("index", "home");
        }

        #region Private
        private async Task<bool> DeleteFile(DbApplication application)
        {
            var result = await _yandexOptions.DeleteObjectAsync($"Releases/{application.Link}.zip");
            if (result.IsSuccess)
            {
                application.Link = null;
                if (await _applicationRepository.Update(application))
                {
                    return true;
                }
            }
            return false;
        }
        private async Task<string> SaveFile(IFormFile file, DbApplication application)
        {
            string name = string.Format("{0}", Guid.NewGuid().ToString());
            var result = await _yandexOptions.PutObjectAsync(file.OpenReadStream(), $"Releases/{name}.zip");
            if (result.IsSuccessStatusCode)
            {
                application.Link = name;
                if (await _applicationRepository.Update(application))
                {
                    return name;
                }
            }
            return null;
        }
        private async Task<Stream> LoadFile(string name)
        {
            try
            {
                return await _yandexOptions.GetAsStreamAsync($"Releases/{name}");
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
                return null;
            }
            
        }
        #endregion
    }
}
