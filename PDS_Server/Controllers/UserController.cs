using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using PDS_Server.Repositories;
using PDS_Server.Humanity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using PDS_Server.Services;
using PDS_Server.Models;
using System.IO;
using AspNetCore.Yandex.ObjectStorage;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Routing;
using PDS_Server.Elements.Revit;
using PDS_Server.Elements;

namespace PDS_Server.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : Controller
    {
        private readonly IMongoRepository<DbAccount> _accountRepository;
        private readonly IMongoRepository<DbDepartment> _departmentRepository;
        private readonly IMongoRepository<DbTeam> _teamRepository;
        private readonly IEmailSender _mailClient;
        private readonly IBot _bot;
        private readonly YandexStorageService _yandexOptions;
        public UserController(IBot bot, IMongoRepository<DbDepartment> departmentRepository, IMongoRepository<DbTeam> teamRepository, IMongoRepository<DbAccount> accountRepository, IEmailSender mailClient, IOptions<YandexStorageOptions> yandexOptions) 
        {
            _bot = bot;
            _departmentRepository = departmentRepository;
            _teamRepository = teamRepository;
            _accountRepository = accountRepository;
            _mailClient = mailClient;
            _yandexOptions = yandexOptions.Value.CreateYandexObjectService();
        }

        [HttpGet]
        [Authorize]
        [Route("")]
        public async Task<IActionResult> Info()
        {
            try
            {
                return new OkObjectResult((await GetUser(User.Identity.Name)).ToResponse());
            }
            catch { }
            return NotFound();
        }

        [HttpGet]
        [Authorize]
        [Route("All")]
        public async Task<IActionResult> All()
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                return new OkObjectResult((await _accountRepository.Get()).Where(x => x.Team == user.Team).Select(x => x.ToResponse()));
            }
            catch { }
            return NotFound();
        }

        [HttpGet]
        [Authorize(Roles = AccessGroups.ADMIN)]
        [Route("list")]
        public async Task<IActionResult> List()
        {
            try
            {
                return new OkObjectResult(await _accountRepository.Get());
            }
            catch { }
            return NotFound();
        }

        [HttpGet]
        [Route("login")]
        public IActionResult Login(int? message)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("index", "home");
            if (message != null)
            {
                switch (message)
                {
                    case 0:
                        ViewBag.Alert = "Пользователь не найден!";
                        break;
                    default:
                        break;
                }
            }
            return View();
        }

        [HttpGet]
        [Route("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("JWToken");
            return RedirectToAction("index", "home");
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password)
        {
            try
            {
                var identity = await GetAccount(email, password);
                if (identity == null)
                {
                    return RedirectToAction("login", "user", new { message = 0 });
                }
                DateTime now = DateTime.UtcNow;
                DateTime expires = DateTime.UtcNow.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME));
                JwtSecurityToken jwt = new JwtSecurityToken(
                        issuer: AuthOptions.ISSUER,
                        audience: AuthOptions.AUDIENCE,
                        notBefore: now,
                        claims: identity.Claims,
                        expires: expires,
                        signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
                string encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
                HttpContext.Session.SetString("JWToken", encodedJwt);
                return RedirectToAction("index", "home");
            }
            catch { }
            return RedirectToAction("index", "home");
        }

        [HttpGet]
        [Route("register")]
        public async Task<IActionResult> Register(int? message)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("index", "home");
            var departments = await _departmentRepository.Get();
            ViewBag.Departments = await _departmentRepository.Get();
            if (message != null)
            {
                switch(message)
                {
                    case 0:
                        ViewBag.Alert = "Неизвестная ошибка!";
                        break;
                    case 1:
                        ViewBag.Alert = "Команда не найдена!";
                        break;
                    case 2:
                        ViewBag.Alert = "Превышен лимит пользователей на одну команду!";
                        break;
                    case 3:
                        ViewBag.Alert = "Пользователь с таким адресом уже существует!";
                        break;
                    case 4:
                        ViewBag.Alert = "Проверьте правильность электронного адреса!";
                        break;
                    default:
                        break;
                }
            }
            return View(await GetAgreement("agreement.txt", _yandexOptions));
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromForm] string team, [FromForm] string password, [FromForm] string email, [FromForm] string firstName, [FromForm] string lastName, [FromForm] string department)
        {
            var depSearch = (await _departmentRepository.Get()).Where(x => x.Name == department);
            if(!depSearch.Any())
            {
                return RedirectToAction("register", "user", new { message = 0 });
            }
            DbDepartment dep = depSearch.First();
            DbTeam joinTeam = null;
            try { joinTeam = (await _teamRepository.Get()).Where(x => team.ToLower() == $"@{x.Name.ToLower()}").First(); }
            catch { }
            if (joinTeam == null)
            {
                return RedirectToAction("register", "user", new { message = 1 });
            }
            if (joinTeam.MaxCount <= joinTeam.Users.Length)
            {
                return RedirectToAction("register", "user", new { message = 2 });
            }
            try
            {
                string normalized;
                if (email.ValidateEmail(out normalized))
                {
                    if (await UserExist(normalized))
                    {
                        return RedirectToAction("register", "user", new { message = 3 });
                    }
                    DbAccount account = new DbAccount()
                    {
                        Login = normalized,
                        Password = password.ConvertToHash(),
                        FirstName = firstName,
                        SecondName = lastName,
                        VerifyLink = Guid.NewGuid().ToString(),
                        Department = dep.Id,
                        Team = joinTeam.Id,
                        Role = AccessGroups.GetRole(AccessGroups.Roles.UnVerified)
                    };
                    await _mailClient.SendMessageAsync(Person.GetRandomPerson(), "Подтверждение регистрации", Local.EmailTemplate.LayoutConfirm, $"http://{Request.Host.Value}/user/verify/{account.VerifyLink}", account.Login, "noreply");
                    ObjectId id = await _accountRepository.Create(account);
                    var list = joinTeam.Users.ToList();
                    list.Add(id);
                    joinTeam.Users = list.ToArray();
                    await _teamRepository.Update(joinTeam);
                    try { await _bot.SendMessage($"Регистрация 💪: @{joinTeam.Name}\n{normalized} ({lastName} {firstName})", BuiltInChatId.Channel_Errors); }
                    catch { }
                    return View("redirect", new RedirectModel() { Title = "Успешная регистрация", Header = "Спасибо за регистрацию!", Quote= "Ураа!", Sticker= "business/018-trophy.svg", Body = string.Format("Мы отправили ссылку для подтверждения вашего адреса электронной почты: {0}", account.Login), Action="", Controller="" });
                }
                return RedirectToAction("register", "user", new { message = 4 });
            }
            catch { }
            return RedirectToAction("register", "user");
        }
        [HttpPost]
        [Route("token")]
        public async Task<IActionResult> Token(string email, string password)
        {
            var identity = await GetAccount(email, password);
            if (identity == null) return NotFound();
            return new OkObjectResult(GetToken(identity));
        }

        [HttpGet]
        [Authorize(Roles = AccessGroups.APPROVED)]
        [Route("token")]
        public async Task<IActionResult> Token()
        {
            var identity = await GetAccount(User.Identity.Name);
            if (identity == null) return NotFound();
            return new OkObjectResult(GetToken(identity));
        }

        [HttpGet]
        [Route("verify/{link?}")]
        public async Task<IActionResult> Verify(string link)
        {
            try
            {
                DbAccount account = (await _accountRepository.Get()).Where(x => x.VerifyLink == link).First();
                if (account != null)
                {
                    bool wasVerifyed = !account.IsVerified;
                    if (!account.IsVerified)
                    {
                        account.IsVerified = true;
                        account.Role = AccessGroups.GetRole(AccessGroups.Roles.TeamMate);
                        account.Access = DateTime.UtcNow.AddMonths(1);
                        await _accountRepository.Update(account);
                        Person person = Person.Persons[0];
                        await _mailClient.SendMessageAsync(person, "Добро пожаловать!", Local.EmailTemplate.LayoutWelcome, account.FirstName, account.Login, "noreply");
                        return View("Redirect", new RedirectModel() { 
                            Title = "Подтверждено", 
                            Header = "Вжууух!", 
                            Quote= ". . . и доступ открыт!",
                            Sticker = "business/001-love message.svg",
                            Body = "Спасибо, что подтвердили адрес своей электронной почты!" +
                            " Мы очень стараемся запустить сервис как можно скорее!" +
                            " Сейчас вы будете перенаправлены на главную страницу сайта.",
                            Action = "index", Controller = "home"
                        });

                    }
                    else
                    {
                        return View("Redirect", new RedirectModel() { 
                            Title = "Подтверждено",
                            Header = "Вжууух!",
                            Quote = ". . . но ничего не произошло!", 
                            Sticker = "business/010-chess game.svg",
                            Body = "Возможно, что вы уже подтвердили адрес своей электронной почты!" +
                            " Мы очень стараемся запустить сервис как можно скорее! Сейчас вы будете перенаправлены на главную страницу сайта.",
                            Action = "index", Controller = "home" });
                    }
                }
            }
            catch { }
            return NotFound();
        }

        #region Private
        private static Dictionary<string, List<string>> Agreement { get; set; }
        private static int GetType(string row)
        {
            int type = 3;
            if (row.StartsWith("####")) type = 0;
            else if (row.StartsWith("###")) type = 1;
            else if (row.StartsWith("##")) type = 2;
            else if (row.StartsWith("#")) type = 3;
            return type;
        }
        private static async Task<List<string>> GetAgreement(string path, YandexStorageService yandexOptions)
        {
            List<string> rows = new List<string>();
            List<string> _rows = new List<string>();
            List<string> result = new List<string>();
            List<string> dict;
            if(Agreement != null)
            {
                if (Agreement.TryGetValue(string.Format("{0}-{1}-{2}", DateTime.Now.Year, DateTime.Now.Year, DateTime.Now.Year), out dict))
                {
                    return dict;
                }
            }

            using (StreamReader sr = new StreamReader(await yandexOptions.GetAsStreamAsync(path)))
            {
                while (!sr.EndOfStream)
                {
                    rows.Add(sr.ReadLine());
                }
            }
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            foreach (string row in rows)
            {
                if (row.Contains("="))
                {
                    var data = row.Split('=');
                    dictionary.Add(data[0], data[1]);
                }
                if (row.Contains("#")) break;
            }
            string[] class_open = new string[] { "<br/><br/><h4>", "<p style=\"margin-left:0rem;margin-top:0rem;margin-bottom:1rem;\">", "<br/><p style=\"margin-left:2rem;margin-top:0rem;margin-bottom:1rem;color=#9DADBC;\">", "<p style=\"margin-left:3rem;margin-top:0rem;margin-bottom:1rem;color=#9DADBC;\">" };
            string[] class_end = new string[] { "</h4>", "</p>", "</p>", "</p>" };
            _rows = rows.Where(x => x.Contains("#")).ToList();
            for (var i = 0; i < _rows.Count; i++)
            {
                string row = _rows[i];
                if (row.StartsWith("#"))
                {
                    int type = GetType(row);
                    if (row.Contains("{"))
                    {
                        foreach (var key in dictionary.Keys)
                        {
                            string value;
                            if (dictionary.TryGetValue(key, out value))
                            {
                                string key_opt = '{' + key + '}';
                                row = row.Replace(key_opt, "<strong>" + value + "</strong>");
                            }
                        }
                    }
                    if (i < _rows.Count - 1)
                    {
                        int type_next = GetType(_rows[i + 1]);
                        if (type < type_next && type != 0) row += ":";
                        else if (type == type_next && type != 0) row += ";";
                        else if (type > type_next && type != 0 && row[row.Length-1] != '.') row += ".";
                    }
                    else
                    {
                        if (type != 0 && row[row.Length - 1] != '.') row += ".";
                    }
                    row = $"{class_open[type]}{row}{class_end[type]}";
                    row = row.Replace("#", "");
                    result.Add(row);
                }
            }
            Agreement = new Dictionary<string, List<string>>() { { string.Format("{0}-{1}-{2}", DateTime.Now.Year, DateTime.Now.Year, DateTime.Now.Year), result } };
            return result;
        }

        private static string GetToken(ClaimsIdentity identity)
        {
            var now = DateTime.UtcNow;

            JwtSecurityToken jwt = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                notBefore: now,
                claims: identity.Claims,
                expires: now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
        private async Task<ClaimsIdentity> GetAccount(string email, string password = null)
        {
            string login;
            List<DbAccount> collection;

            if (!email.ValidateEmail(out login)) return null;

            if (password == null)
                collection = (await _accountRepository.Get()).Where(x => x.Login == login).ToList();

            else
                collection = (await _accountRepository.Get()).Where(x => (x.Password == password.ConvertToHash()) && x.Login == login).ToList();

            if (collection.Count == 1)
            {
                DbAccount person = collection.First();
                if (person != null)
                {
                    if (!person.IsVerified) return null;
                    List<Claim> claims = new List<Claim>
                    {
                        new Claim(ClaimsIdentity.DefaultNameClaimType, person.Login),
                        new Claim(ClaimsIdentity.DefaultRoleClaimType, person.Role)
                    };
                    ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
                    return claimsIdentity;
                }
            }
            return null;
        }
        private async Task<bool> UserExist(string email)
        {
            return (await GetUser(email)) != null;
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
