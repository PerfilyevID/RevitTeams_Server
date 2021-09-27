using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using PDS_Server.Elements;
using PDS_Server.Elements.Communications;
using PDS_Server.Elements.Revit;
using PDS_Server.Repositories;
using PDS_Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PDS_Server.Api
{
    [Route("api/chat")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IMongoRepository<DbAccount> _accountRepository;
        private readonly IMongoRepository<DbClashResult> _clashResultRepository;
        private readonly IMongoRepository<DbChat> _chatRepository;
        private readonly IBot _bot;
        public ChatController(IBot bot, IMongoRepository<DbChat> chatRepository, IMongoRepository<DbClashResult> documentRepository, IMongoRepository<DbAccount> accountRepository)
        {
            _bot = bot;
            _chatRepository = chatRepository;
            _clashResultRepository = documentRepository;
            _accountRepository = accountRepository;
        }

        [Route("{id?}")]
        [HttpGet]
        public async Task<IActionResult> Chat(string id)
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                var result = await _chatRepository.Get(new ObjectId(id), user.Team.ToString());
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

        [Route("{id?}")]
        [HttpPost]
        public async Task<IActionResult> PostMessage(string id, [FromForm] string to, [FromForm] string message)
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                DbChat result = await _chatRepository.Get(new ObjectId(id), user.Team.ToString());
                if (result != null)
                {
                    List<DbMessage> messages = result.Messages;
                    if (messages == null) messages = new List<DbMessage>();
                    messages.Add(new DbMessage()
                    {
                        From = user.Id,
                        Message = message,
                        Time = DateTime.UtcNow,
                        To = string.IsNullOrEmpty(to) ? null : new ObjectId(to)
                    });
                    result.Messages = messages;
                    result.LastChange = DateTime.UtcNow;
                    await _chatRepository.Update(result, user.Team.ToString());
                    return Ok();
                }
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
            }
            return BadRequest();
        }

        [Route("delete/{id?}")]
        [HttpPost]
        public async Task<IActionResult> DeleteMessage(string id, [FromForm] int number)
        {
            try
            {
                DbAccount user = await GetUser(User.Identity.Name);
                var result = await _chatRepository.Get(new ObjectId(id), user.Team.ToString());
                if (result != null)
                {
                    List<DbMessage> messages = result.Messages;
                    if (messages == null) messages = new List<DbMessage>();
                    if (messages[number].From == user.Id)
                    {
                        messages.RemoveAt(number);
                    }
                    result.Messages = messages;
                    await _chatRepository.Update(result, user.Team.ToString());
                    return Ok();
                }
            }
            catch (Exception e)
            {
                await _bot.SendException(e);
            }
            return BadRequest();
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
