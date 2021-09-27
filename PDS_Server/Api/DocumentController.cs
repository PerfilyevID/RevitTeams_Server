using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using PDS_Server.Elements;
using PDS_Server.Elements.Revit;
using PDS_Server.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PDS_Server.Api
{
    [Route("api/document")]
    [ApiController]
    [Authorize(Roles = AccessGroups.APPROVED)]
    public class DocumentController : Controller
    {
        private readonly IMongoRepository<DbAccount> _accountRepository;
        private readonly IMongoRepository<DbDocument> _documentRepository;
        public DocumentController(IMongoRepository<DbDocument> documentRepository, IMongoRepository<DbAccount> accountRepository)
        {
            _documentRepository = documentRepository;
            _accountRepository = accountRepository;
        }

        [Route("list")]
        [HttpGet]
        public async Task<IActionResult> List()
        {
            DbAccount user = await GetUser(User.Identity.Name);
            var docs = (await _documentRepository.Get(user.Team.ToString())).Select(x => x.ToResponse()).ToArray();
            return Ok(docs);
        }

        [Route("{id?}")]
        [HttpGet]
        public async Task<IActionResult> Document(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                DbAccount user = await GetUser(User.Identity.Name);
                DbDocument doc = await _documentRepository.Get(new ObjectId(id), user.Team.ToString());
                return Ok(doc.ToResponse());
            }
            return NotFound();
        }

        [Route("delete/{id?}")]
        [HttpPost]
        [Authorize(Roles = AccessGroups.MODERATOR)]
        public async Task<IActionResult> Delete(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                DbAccount user = await GetUser(User.Identity.Name);
                await _documentRepository.Delete(new ObjectId(id), user.Team.ToString());
                return Ok();
            }
            return NotFound();
        }

        [Route("insert")]
        [HttpPost]
        public async Task<IActionResult> Insert([FromForm] string guid, [FromForm] string fullPath, [FromForm] string name, [FromForm] bool isCloud)
        {
            try
            {
                if(!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(fullPath))
                {
                    DbAccount user = await GetUser(User.Identity.Name);
                    var documents = await _documentRepository.Get(user.Team.ToString());
                    bool exist = false;
                    if(isCloud)
                    {
                        if (documents.Where(x => x.IsCloudModel == isCloud && x.ServerGuid == guid).Any()) exist = true;
                    }
                    else
                    {
                        if (documents.Where(x => x.IsCloudModel == isCloud && x.FullPath == fullPath).Any()) exist = true;
                    }
                    if(!exist)
                    {
                        await _documentRepository.Create(new DbDocument()
                        {
                            Department = null,
                            FoundByUser = user.Id,
                            FullPath = fullPath,
                            Name = name,
                            Project = null,
                            ServerGuid = guid,
                            SyncCount = 0,
                            IsCloudModel = isCloud
                        }, user.Team.ToString());
                        return Ok();
                    }
                    return Ok();
                }
            }
            catch (Exception) { }
            return NotFound();
        }

        [Route("synchronized")]
        [HttpPost]
        public async Task<IActionResult> Insert([FromForm] string fullPath, [FromForm] string name, [FromForm] string isCloud)
        {
            try
            {
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(fullPath))
                {
                    DbAccount user = await GetUser(User.Identity.Name);
                    await _documentRepository.Create(new DbDocument()
                    {
                        Department = null,
                        FoundByUser = user.Id,
                        FullPath = fullPath,
                        Name = name,
                        Project = null
                    }, user.Team.ToString());
                    return Ok();
                }

            }
            catch { }
            return NotFound();
        }

        #region Private
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
