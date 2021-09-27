using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using PDS_Server.Elements;
using PDS_Server.Elements.Revit;
using PDS_Server.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace PDS_Server.Controllers
{
    [Route("team")]
    [ApiController]
    [Authorize(Roles = AccessGroups.APPROVED)]
    public class TeamController : Controller
    {
        private readonly IMongoRepository<DbAccount> _accountRepository;
        private readonly IMongoRepository<DbDocument> _documentRepository;
        private readonly IMongoRepository<DbTeam> _teamRepository;
        private readonly IMongoRepository<DbFamilyCategory> _categoryRepository;
        public TeamController(IMongoRepository<DbFamilyCategory> categoryRepository, IMongoRepository<DbDocument> documentRepository, IMongoRepository<DbTeam> teamRepository, IMongoRepository<DbAccount> accountRepository)
        {
            _categoryRepository = categoryRepository;
            _documentRepository = documentRepository;
            _teamRepository = teamRepository;
            _accountRepository = accountRepository;
        }

        [Route("index")]
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await SetViewBagUserTeam(this);
            return View("index", "team");
        }

        private async Task SetViewBagUserTeam(Controller controller)
        {
            DbAccount user = (await _accountRepository.Get()).Where(x => x.Login == controller.User.Identity.Name).First();
            controller.ViewBag.User = user;
            controller.ViewBag.Team = await _teamRepository.Get((ObjectId)user.Team);
        }

        [Route("addfamily")]
        [HttpGet]
        public async Task<IActionResult> AddFamily()
        {
            var categories = await _categoryRepository.Get();
            if(!categories.Any())
            {
                await _categoryRepository.Create(new DbFamilyCategory() 
                {
                    Name = "Архитектура", Children = new DbFamilyCategory[] 
                    { 
                        new DbFamilyCategory() { Name = "Двери" },
                        new DbFamilyCategory() { Name = "Окна" },
                        new DbFamilyCategory() { Name = "Мебель" },
                        new DbFamilyCategory() { Name = "Кровля" },
                        new DbFamilyCategory() { Name = "Фасад" },
                        new DbFamilyCategory() { Name = "Паркинг" },
                        new DbFamilyCategory() { Name = "Витражи" },
                        new DbFamilyCategory() { Name = "Отверстия" },
                        new DbFamilyCategory() { Name = "Лестницы" },
                        new DbFamilyCategory() { Name = "Оборудование" },
                        new DbFamilyCategory() { Name = "Антураж" }
                    }
                });
                await _categoryRepository.Create(new DbFamilyCategory()
                {
                    Name = "Оформление",
                    Children = new DbFamilyCategory[]
                    {
                        new DbFamilyCategory() { Name = "Марки" },
                        new DbFamilyCategory() { Name = "Листы" },
                        new DbFamilyCategory() { Name = "Аннотации" }
                    }
                });
            }
            await SetViewBagUserTeam(this);
            ViewBag.Categories = await _categoryRepository.Get();
            return View();
        }

        [Route("addfamily")]
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]//50mb
        public async Task<IActionResult> AddFamily(
            [FromForm] string category,
            [FromForm] string name,
            [FromForm] string description,
            [FromForm] string lod,
            [FromForm] IFormFile revit2016,
            [FromForm] IFormFile revit2017,
            [FromForm] IFormFile revit2018,
            [FromForm] IFormFile revit2019,
            [FromForm] IFormFile revit2020,
            [FromForm] IFormFile revit2021,
            [FromForm] IFormFile revit2022)
        {
            await SetViewBagUserTeam(this);
            return View();
        }

        [Route("addproject")]
        [HttpGet]
        public async Task<IActionResult> AddProject()
        {
            await SetViewBagUserTeam(this);
            return View();
        }

        [Route("addtask")]
        [HttpGet]
        public async Task<IActionResult> AddTask()
        {
            await SetViewBagUserTeam(this);
            return View();
        }

        [Route("document/{id?}")]
        [HttpGet]
        public async Task<IActionResult> Document(string id)
        {
            await SetViewBagUserTeam(this);
            return View();
        }
        [Route("projects")]
        [HttpGet]
        public async Task<IActionResult> Projects()
        {
            await SetViewBagUserTeam(this);
            return View();
        }

        [Route("project/{id?}")]
        [HttpGet]
        public async Task<IActionResult> Project(string id)
        {
            await SetViewBagUserTeam(this);
            return View();
        }

        [Route("library")]
        [HttpGet]
        public async Task<IActionResult> Library()
        {
            await SetViewBagUserTeam(this);
            return View();
        }

        [Route("standart")]
        [HttpGet]
        public async Task<IActionResult> Standart()
        {
            await SetViewBagUserTeam(this);
            return View();
        }

        [Route("task/{id>}")]
        [HttpGet]
        public async Task<IActionResult> Task(string id)
        {
            await SetViewBagUserTeam(this);
            return View();
        }
    }
}
