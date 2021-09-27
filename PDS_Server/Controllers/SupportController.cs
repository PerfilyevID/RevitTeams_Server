using AspNetCore.Yandex.ObjectStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PDS_Server.Elements;
using PDS_Server.Humanity;
using PDS_Server.Repositories;
using PDS_Server.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PDS_Server.Controllers
{
    [Route("support")]
    [ApiController]
    public class SupportController : Controller
    {
        private readonly IMongoRepository<DbReport> _reportRepository;
        private readonly IMongoRepository<DbAccount> _accountRepository;
        private readonly IMongoRepository<DbException> _exceptionRepository;
        private readonly IBot _bot;
        private readonly YandexStorageService _yandexOptions;
        private readonly IEmailSender _mailClient;
        public SupportController(IBot bot, IMongoRepository<DbException> exceptionRepository, IMongoRepository<DbReport> reportRepository, IEmailSender mailClient, IMongoRepository<DbAccount> accountRepository, IOptions<YandexStorageOptions> yandexOptions)
        {
            _bot = bot;
            _exceptionRepository = exceptionRepository;
            _reportRepository = reportRepository;
            _accountRepository = accountRepository;
            _mailClient = mailClient;
            _yandexOptions = yandexOptions.Value.CreateYandexObjectService();
        }

        [Route("bug")]
        [HttpPost]
        public async Task<IActionResult> Bug([FromForm] string user, [FromForm] string time, [FromForm] string data, [FromForm] string plugin)
        {
            try
            {
                var ddd = new HashSet<string>((await _exceptionRepository.Get()).Select(x => x.Data));
                if(!ddd.Contains(data))
                {
                    await _exceptionRepository.Create(new DbException() { User = user, Time = time, Data = data, Plugin = plugin });
                    try { await _bot.SendMessage($"☠️ От:{user}\n{data}\n{plugin}", BuiltInChatId.Channel_Errors); }
                    catch { }
                }
                return Ok();
            }
            catch { }
            return BadRequest();
        }

        [Route("download/{name?}")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Download(string name)
        {
            try
            {
                Stream stream = await LoadFile(name);
                if (stream != null) { return File(stream, "application/octet-stream"); }
            }
            catch { }
            return NotFound();
        }

        [Route("report/{id?}")]
        [HttpGet]
        public async Task<IActionResult> Report(string id)
        {
            var report = await _reportRepository.Get(new MongoDB.Bson.ObjectId(id));
            if (report == null) return RedirectToAction("index", "home");
            return View(report);
        }

        [Route("delete")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var report = await _reportRepository.Get(new MongoDB.Bson.ObjectId(id));
            if(report != null)
            {
                await _reportRepository.Delete(report.Id);
                await DeleteFile(report);
            }
            return RedirectToAction("console");
        }

        [Route("answer/{id?}")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Answer([FromForm] string comment, string id)
        {
            try
            {
                DbReport report = await _reportRepository.Get(new MongoDB.Bson.ObjectId(id));
                report.Comment = comment;
                report.IsClosed = true;
                await _reportRepository.Update(report);
                await _mailClient.SendMessageAsync(Person.GetRandomPerson(), "Поддержка", Local.EmailTemplate.LayoutSupportEvent, $"http://{Request.Host.Value}/support/report/{report.Id}", report.User, "support");
            }
            catch { }
            return RedirectToAction("console");
        }

        [Route("console")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Console()
        {
            var data = (await _reportRepository.Get()).Where(x => x.IsClosed == false).Take(5).ToList();
            if(data.Count == 0)
            {
                data = (await _reportRepository.Get()).ToList();
            }
            return View(data);
        }

        [Route("index")]
        [Route("")]
        [HttpGet]
        public IActionResult Index(int? message)
        {
            if (message != null)
            {
                switch (message)
                {
                    case 0:
                        ViewBag.Alert = "Неизвестный формат!";
                        break;
                    case 1:
                        ViewBag.Alert = "Формат не поддерживается!";
                        break;
                    case 2:
                        ViewBag.Success = "Обращение отправлено в поддержку!";
                        break;
                    case 3:
                        ViewBag.Alert = "Файл слишком большой!";
                        break;
                    case 4:
                        ViewBag.Alert = "Неизвестная ошибка!";
                        break;
                    default:
                        break;
                }
            }
            return View();
        }

        [Route("send")]
        [Route("")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Send([FromForm] string issue, [FromForm] string description, [FromForm] IFormFile file)
        {
            string[] formats = new string[] { "jpeg", "png", "bmp", "gif", "tga", "jpg", "tif", "tiff" };
            if (file != null)
            {
                if(file.Length >= 500000)
                {
                    return RedirectToAction("index", "support", new { message = 3 });
                }
                if (!file.FileName.Contains('.'))
                {
                    return RedirectToAction("index", "support", new { message = 0 });
                }
                if (!formats.Contains(file.FileName.Split('.').Last()))
                {
                    return RedirectToAction("index", "support", new { message = 1 });
                }
            }
            DbAccount user = await GetUser(User.Identity.Name);
            DbReport report = new DbReport() { Issue = issue, Description = description, IsClosed = false, User = user.Login };
            report.Id = await _reportRepository.Create(report);
            if (file != null) await SaveFile(file, report);
            await _mailClient.SendMessageAsync(Person.GetRandomPerson(), "Поддержка", Local.EmailTemplate.LayoutSupportCreated, $"http://{Request.Host.Value}/support/report/{report.Id}", user.Login, "support");
            try { await _bot.SendMessage($"😱 Новое обращение: {issue}\n{description}", BuiltInChatId.Channel_Errors); }
            catch { }
            return RedirectToAction("index", "support", new { message = 2 });
        }

        [Route("sendguest")]
        [Route("")]
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 5242880)]//5mb
        public async Task<IActionResult> SendGuest([FromForm] string issue, [FromForm] string description, [FromForm] IFormFile file, [FromForm] string email)
        {
            string[] formats = new string[] { "jpeg", "png", "bmp", "gif", "tga", "jpg", "tif", "tiff" };
            if(file != null)
            {
                if (file.Length >= 500000)
                {
                    return RedirectToAction("index", "support", new { message = 3 });
                }
                if (!file.FileName.Contains('.'))
                {
                    return RedirectToAction("index", "support", new { message = 0 });
                }
                if (!formats.Contains(file.FileName.Split('.').Last()))
                {
                    return RedirectToAction("index", "support", new { message = 1 });
                }
            }

            DbReport report = new DbReport() { Issue = issue, Description = description, IsClosed = false, User = email };
            report.Id = await _reportRepository.Create(report);
            if (file != null) await SaveFile(file, report);
            try
            {
                await _mailClient.SendMessageAsync(Person.GetRandomPerson(), "Поддержка", Local.EmailTemplate.LayoutSupportCreated, $"http://{Request.Host.Value}/support/report/{report.Id}", email, "support");
                try { await _bot.SendMessage($"😱 Новое обращение: {issue}\n{description}", BuiltInChatId.Channel_Errors); }
                catch { }
                return RedirectToAction("index", "support", new { message = 2 });
            }
            catch
            {
                if (file != null) await DeleteFile(report);
                await _reportRepository.Delete(report.Id);
                return RedirectToAction("index", "support", new { message = 4 });
            }
        }
        #region Private
        private async Task<string> SaveFile(IFormFile file, DbReport report)
        {
            string name = string.Format("{0}_{1}", Guid.NewGuid().ToString(), file.FileName);
            var result = await _yandexOptions.PutObjectAsync(file.OpenReadStream(), $"Reports/{name}");
            if (result.IsSuccessStatusCode)
            {
                report.Link = name;
                if (await _reportRepository.Update(report))
                {
                    return name;
                }
            }
            return null;
        }
        private async Task<bool> DeleteFile(DbReport report)
        {
            await _yandexOptions.DeleteObjectAsync($"Reports/{report.Link}");
            return true;
        }
        private async Task<Stream> LoadFile(string name)
        {
            return await _yandexOptions.GetAsStreamAsync($"Reports/{name}");
        }
        private async Task<DbAccount> GetUser(string email)
        {
            string login;
            if (!email.ValidateEmail(out login)) return null;
            List<DbAccount> collection = (await _accountRepository.Get()).Where(x => x.Login == login).ToList();
            if (collection.Count == 1)
            {
                DbAccount person = collection.First();
                if (person != null) return person;
            }
            return null;
        }
        #endregion
    }
}
