using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Newtonsoft.Json;
using PDS_Server.Elements;
using PDS_Server.Elements.Communications;
using PDS_Server.Elements.Revit;
using PDS_Server.Repositories;
using PDS_Server.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDS_Server.Api
{
    [Route("api/clash")]
    [ApiController]
    [Authorize(Roles = AccessGroups.APPROVED)]
    public class ClashController : ControllerBase
    {
        private readonly IMongoRepository<DbAccount> _accountRepository;
        private readonly IMongoRepository<DbClashResult> _clashResultRepository;
        private readonly IMongoRepository<DbGroupOfClashes> _clashGroupRepository;
        private readonly IMongoRepository<DbChat> _chatRepository;
        private readonly IMongoRepository<DbProject> _projectRepository;
        private readonly IBot _bot;
        public ClashController(IBot bot, IMongoRepository<DbProject> projectRepository, IMongoRepository<DbGroupOfClashes> clashGroupRepository, IMongoRepository<DbChat> chatRepository, IMongoRepository<DbClashResult> documentRepository, IMongoRepository<DbAccount> accountRepository)
        {
            _bot = bot;
            _projectRepository = projectRepository;
            _clashGroupRepository = clashGroupRepository;
            _chatRepository = chatRepository;
            _clashResultRepository = documentRepository;
            _accountRepository = accountRepository;
        }

        [Route("result/{id?}")]
        [HttpGet]
        public async Task<IActionResult> Result(string id)
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                if (string.IsNullOrEmpty(id) || string.IsNullOrWhiteSpace(id))
                {
                    return NotFound();
                }
                var result = await _clashResultRepository.Get(new ObjectId(id), user.Team.ToString());
                if (result != null)
                {
                    return Ok(result.ToResponse());
                }
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
            }
            return NotFound();
        }

        [Route("results")]
        [HttpGet]
        public async Task<IActionResult> Results()
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                var result = await _clashResultRepository.Get(user.Team.ToString());
                if (result != null)
                {
                    return Ok(result.Select(x => x.ToResponse()));
                }
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
            }
            return NotFound();
        }

        [Route("group/{id?}")]
        [HttpGet]
        public async Task<IActionResult> Group(string id)
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                if (string.IsNullOrEmpty(id) || string.IsNullOrWhiteSpace(id))
                {
                    return NotFound();
                }
                var result = await _clashGroupRepository.Get(new ObjectId(id), user.Team.ToString());
                if (result != null)
                {
                    return Ok(result.ToResponse());
                }
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
            }
            return NotFound();
        }

        [Route("groups")]
        [HttpGet]
        public async Task<IActionResult> Groups()
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                var result = await _clashGroupRepository.Get(user.Team.ToString());
                if (result != null)
                {
                    return Ok(result.Select(x => x.ToResponse()));
                }
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
            }
            return NotFound();
        }


        [Route("result/update/{id?}")]
        [HttpPost]
        public async Task<IActionResult> UpdateResult(string id, [FromForm] int status, [FromForm] int clashId)
        {
            try
            {
                int[] range = new int[] { -1, 0, 1 };
                if (!range.Contains(status)) return BadRequest();
                DbAccount user = await GetUser(User.Identity.Name);
                var result = await _clashResultRepository.Get(new ObjectId(id), user.Team.ToString());
                if (result != null)
                {
                    var clash = result.Clashes.Where(x => x.Id == clashId);
                    if (clash.Any())
                    {
                        clash.First().Status = status;
                    }
                    await _clashResultRepository.Update(result, user.Team.ToString());
                    if (result.Group != null)
                    {
                        await UpdateClashGroup((ObjectId)result.Group, user);
                    }
                    return Ok();
                }
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
            }
            return NotFound();
        }

        [Route("createClash")]
        [HttpPost]
        public async Task<IActionResult> CreateClash([FromForm] IFormFile data, [FromForm] string clashGroup, [FromForm] string description, [FromForm] string name)
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                using (Stream stream = data.OpenReadStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    DbGroupOfClashes group = await _clashGroupRepository.Get(new ObjectId(clashGroup), user.Team.ToString());
                    if (group == null)
                    {
                        return NotFound();
                    }
                    DbClash[] clashes = JsonConvert.DeserializeObject<DbClash[]>(await reader.ReadToEndAsync());
                    foreach (DbClash clash in clashes)
                    {
                        clash.ChatId = (await _chatRepository.Create(new DbChat() { LastChange = DateTime.UtcNow }, user.Team.ToString())).ToString();
                    }
                    ObjectId clashId = await _clashResultRepository.Create(new DbClashResult()
                    {
                        Clashes = clashes,
                        CreationTime = DateTime.UtcNow,
                        Description = description,
                        ItemsCount = clashes.Length,
                        ItemsDone = 0,
                        LastChange = DateTime.UtcNow,
                        Name = name,
                        Group = new ObjectId(clashGroup)
                    }, user.Team.ToString());

                    if (group.Items == null)
                    {
                        group.Items = new ObjectId[] { clashId };
                    }
                    else
                    {
                        List<ObjectId> list = group.Items.ToList();
                        list.Add(clashId);
                        group.Items = list.ToArray();
                    }
                    await _clashGroupRepository.Update(group, user.Team.ToString());
                    await UpdateClashGroup(group.Id, user);
                }
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
            }
            return BadRequest();
        }
        
        [Route("deleteClash")]
        [HttpPost]
        public async Task<IActionResult> DeleteClash([FromForm] string id)
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                DbClashResult result = await _clashResultRepository.Get(new ObjectId(id), user.Team.ToString());
                if (result == null)
                {
                    return NotFound();
                }
                if (result.Group != null)
                {
                    DbGroupOfClashes group = await _clashGroupRepository.Get((ObjectId)result.Group, user.Team.ToString());
                    if (group != null)
                    {
                        var items = group.Items.ToList();
                        items.Remove(result.Id);
                        await _clashGroupRepository.Update(group, user.Team.ToString());
                    }
                }
                await DeleteClashResult(result.Id, user);
                await _clashResultRepository.Delete(result.Id, user.Team.ToString());
                return Ok();
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
            }
            return BadRequest();
        }

        [Route("deleteGroup")]
        [HttpPost]
        public async Task<IActionResult> DeleteGroup([FromForm] string id)
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                DbGroupOfClashes result = await _clashGroupRepository.Get(new ObjectId(id), user.Team.ToString());
                if (result == null)
                {
                    return NotFound();
                }
                foreach (var i in result.Items)
                {
                    await DeleteClashResult(i, user);
                }
                await _clashResultRepository.Delete(result.Items, user.Team.ToString());
                await _clashGroupRepository.Delete(result.Id, user.Team.ToString());
                return Ok();
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
            }
            return BadRequest();
        }

        [Route("createGroup")]
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromForm] string project, [FromForm] string description, [FromForm] string name)
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                bool useProject = !string.IsNullOrEmpty(project) && !string.IsNullOrWhiteSpace(project);
                if (useProject)
                {
                    if (await _projectRepository.Get(new ObjectId(project), user.Team.ToString()) == null)
                    {
                        return BadRequest();
                    }
                }
                var id = await _clashGroupRepository.Create(new DbGroupOfClashes()
                {
                    Description = description,
                    Name = name,
                    IsClosed = false,
                    Items = new ObjectId[] { },
                    Progress = 0,
                    Project = useProject ? new ObjectId(project) : null
                }, user.Team.ToString());
                return Ok(id.ToString());
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
            }
            return BadRequest();
        }

        [Route("updateGroup/{id?}")]
        [HttpPost]
        public async Task<IActionResult> UpdateGroup(string id, [FromForm] string project, [FromForm] string description, [FromForm] string name, [FromForm] bool isClosed)
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                DbGroupOfClashes group = await _clashGroupRepository.Get(new ObjectId(id), user.Team.ToString());
                if (group == null)
                {
                    return BadRequest();
                }
                group.Project = new ObjectId(project);
                group.Description = description;
                group.Name = name;
                group.IsClosed = isClosed;
                await _clashGroupRepository.Update(group, user.Team.ToString());
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
            }
            return BadRequest();
        }

        #region Private
        private async Task DeleteClashResult(ObjectId id, DbAccount user)
        {
            DbClashResult result = await _clashResultRepository.Get(id, user.Team.ToString());
            if(result != null)
            {
                if (result.Clashes != null)
                {
                    await _chatRepository.Delete(result.Clashes.Select(x => new ObjectId(x.ChatId)).ToArray(), user.Team.ToString());
                }
            }
        }
        private async Task<Tuple<bool, DbClashResult>> UpdateClashResult(ObjectId id, DbAccount user)
        {
            var result = await _clashResultRepository.Get(id, user.Team.ToString());
            if (result == null) return null;
            bool changed = false;
            if (result.ItemsCount != result.Clashes.Length) 
            { 
                result.ItemsCount = result.Clashes.Length;
                changed = true;
            }
            if(result.ItemsDone != result.Clashes.Where(x => x.Status != -1).Count())
            {
                result.ItemsDone = result.Clashes.Where(x => x.Status != -1).Count();
                changed = true;
            }
            if(changed)
            {
                result.LastChange = DateTime.UtcNow;
                await _clashResultRepository.Update(result, user.Team.ToString());
            }
            return new Tuple<bool, DbClashResult>(changed, result);
        }
        private async Task UpdateClashGroup(ObjectId id, DbAccount user)
        {
            var group = await _clashGroupRepository.Get(id, user.Team.ToString());
            if (group == null) return;
            int max = 0;
            int done = 0;
            bool changed = false;
            if(group.Items != null)
            {
                foreach (ObjectId itemId in group.Items)
                {
                    Tuple<bool, DbClashResult> result = await UpdateClashResult(itemId, user);
                    if (result == null) continue;
                    if (result.Item1) changed = true;
                    max += result.Item2.ItemsCount;
                    done += result.Item2.ItemsDone;
                }
            }
            int progress = 0;
            try { progress = done / max * 100; }
            catch { }
            if (group.Progress != progress)
            {
                group.Progress = progress;
                changed = true;
            }
            if (changed)
            {
                await _clashGroupRepository.Update(group, user.Team.ToString());
            }
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
