using AspNetCore.Yandex.ObjectStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PDS_Server.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using PDS_Server;
using PDS_Server.Elements;

namespace RevitTeams_Server.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("store")]
    public class StoreController : Controller
    {
        private readonly IMongoRepository<DbPlugin> _pluginRepository;
        private readonly YandexStorageService _yandexOptions;
        public StoreController(IMongoRepository<DbPlugin> pluginRepository, IOptions<YandexStorageOptions> yandexOptions)
        {
            _pluginRepository = pluginRepository;
            _yandexOptions = yandexOptions.Value.CreateYandexObjectService();
        }

        [HttpGet]
        [Route("publish")]
        public async Task<IActionResult> Publish(string id, string number)
        {
            DbPlugin plugin = await _pluginRepository.Get(new MongoDB.Bson.ObjectId(id));
            if(plugin != null)
            {
                DbVersion ver = plugin.Versions.Where(x => x.Number == number).First();
                if(ver != null)
                {
                    ver.Published = true;
                }
            }
            await _pluginRepository.Update(plugin);
            return RedirectToAction("index", "store");
        }

        [HttpGet]
        [Route("hide")]
        public async Task<IActionResult> Hide(string id, string number)
        {
            DbPlugin plugin = await _pluginRepository.Get(new MongoDB.Bson.ObjectId(id));
            if (plugin != null)
            {
                DbVersion ver = plugin.Versions.Where(x => x.Number == number).First();
                if (ver != null)
                {
                    ver.Published = false;
                }
            }
            await _pluginRepository.Update(plugin);
            return RedirectToAction("index", "store");
        }

        [HttpPost]
        [Route("addversion/{target}")]
        public async Task<IActionResult> AddVersion([FromForm] string version, [FromForm] string changelog, string target)
        {
            try
            {
                if (!string.IsNullOrEmpty(target) && !string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(changelog))
                {
                    var p = await _pluginRepository.Get(new MongoDB.Bson.ObjectId(target));
                    if (!p.Versions.Select(x => x.Number).Contains(version))
                    {
                        var versions = new List<DbVersion>() {
                        new DbVersion()
                        {
                            Changelog = changelog,
                            Number = version,
                            Published = false
                        }
                    };
                        versions.AddRange(p.Versions.ToList());
                        p.Versions = versions.ToArray();
                        await _pluginRepository.Update(p);
                    }
                }
            }
            catch (Exception) { }
            return RedirectToAction("index", "store");
        }

        [HttpGet]
        [Route("delete")]
        public async Task<IActionResult> Delete(string id)
        {
            DbPlugin plugin = await _pluginRepository.Get(new MongoDB.Bson.ObjectId(id));
            foreach(var v in plugin.Versions)
            {
                foreach(var r in v.RevitVersions)
                {
                    await DeleteFile(plugin, r);
                }
            }
            await _pluginRepository.Delete(new MongoDB.Bson.ObjectId(id));
            return View("index", await _pluginRepository.Get());
        }

        [HttpPost]
        [Route("addplugin")]
        public async Task<IActionResult> AddPlugin([FromForm] string title, [FromForm] string description, [FromForm] string changelog, [FromForm] string target, [FromForm] string version, [FromForm] string publish)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(target) || string.IsNullOrEmpty(version)) 
            {
                return RedirectToAction("index", "store", new { message = 0 }); 
            }
            DbPlugin p = new DbPlugin()
            {
                Name = title,
                Description = description,
                Target = target, Versions = new DbVersion[]
                {
                    new DbVersion()
                    {
                         Number = version, Published = !string.IsNullOrEmpty(publish), Changelog = changelog
                    }
                }
                
            };
            await _pluginRepository.Create(p);
            return RedirectToAction("index", "store");
        }

        [HttpGet]
        [Route("deleterevitversion")]
        public async Task<IActionResult> DeleteRevitVersion(string plugin, string version, string revitVersion, string link)
        {
            DbPlugin target = await _pluginRepository.Get(new MongoDB.Bson.ObjectId(plugin));
            DbRevitVersionInstance versionToDelete = target.Versions.Where(x => x.Number == version).First().RevitVersions.Where(x => x.Number == revitVersion && x.Link == link).First();
            await DeleteFile(target, versionToDelete);
            return RedirectToAction("index", "store");
        }

        [HttpPost]
        [Route("addrevitversion/{target}")]
        public async Task<IActionResult> AddRevitVersion([FromForm] IFormFile file, string target)
        {
            if (file != null && !string.IsNullOrEmpty(target))
            {
                string[] data = target.Split(':');
                DbPlugin plugin = await _pluginRepository.Get(new ObjectId(data[0]));
                DbVersion version = plugin.Versions.Where(x => x.Number == data[1]).First();
                DbRevitVersionInstance revitVersion = version.RevitVersions.Where(x => x.Number == data[2]).First();
                if (file.FileName.Split('.').Last().ToLower() == "zip") 
                    revitVersion.Link = await SaveFile(file, plugin, version, revitVersion);
            }
            return RedirectToAction("index", "store");
        }

        [HttpGet]
        [Route("index")]
        [Route("")]
        public async Task<IActionResult> Index(int? message)
        {
            if(message != null)
            {
                switch(message)
                {
                    case 0:
                        ViewBag.Alert = "Необходимо заполнить все обязательные поля!";
                        break;
                    default:
                        break;
                }
            }
            return View(await _pluginRepository.Get());
        }

        #region Private
        private async Task<bool> DeleteFile(DbPlugin plugin, DbRevitVersionInstance revitVersion)
        {
            var result = await _yandexOptions.DeleteObjectAsync($"Plugins/{plugin.Id}/{revitVersion.Number}/{revitVersion.Link}.zip");
            if (result.IsSuccess)
            {
                revitVersion.Link = null;
                if (await _pluginRepository.Update(plugin))
                {
                    return true;
                }
            }
            return false;
        }
        private async Task<string> SaveFile(IFormFile file, DbPlugin plugin, DbVersion version, DbRevitVersionInstance revitVersion)
        {
            string name = string.Format("{0}_{1}", version.Number, Guid.NewGuid().ToString());
            var result = await _yandexOptions.PutObjectAsync(file.OpenReadStream(), $"Plugins/{plugin.Id}/{revitVersion.Number}/{name}.zip");
            if(result.IsSuccessStatusCode)
            {
                revitVersion.Link = name;
                if(await _pluginRepository.Update(plugin))
                {
                    return name;
                }
            }
            return null;
        }
        private async Task<Stream> LoadFile(DbPlugin plugin, DbRevitVersionInstance revitVersion)
        {
            return await _yandexOptions.GetAsStreamAsync($"Plugins/{plugin.Id}/{revitVersion.Number}/{revitVersion.Link}.zip");
        }
        #endregion
    }
}
