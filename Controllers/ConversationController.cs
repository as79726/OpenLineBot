using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using isRock.LineBot;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenLineBot.Models.Conversation.Entity.Custom;
using OpenLineBot.Service;
namespace OpenLineBot.Controllers {
    [ApiController]
    [Route ("[controller]")]
    public class ConversationController : ControllerBase {
        private readonly ILogger<ConversationController> _logger;
        private readonly IConfiguration _config;
        private readonly FirestoreDb _db;
        private readonly SecretInfo _secretInfo;
        public ConversationController (ILogger<ConversationController> logger, IConfiguration config, SecretInfo secretInfo, FirestoreDb db) {
            _logger = logger;
            _config = config;
            _secretInfo = secretInfo;
            _db = db;
        }

        [HttpPost]
        public IActionResult POST () {
            BotService bot = null;
            DatabaseService db = null;
            try {
                string postData = "";
                using (StreamReader reader = new StreamReader (Request.Body, System.Text.Encoding.UTF8)) {
                    postData = reader.ReadToEndAsync ().Result;
                }
                var receivedMessage = Utility.Parsing (postData);
                var evt = receivedMessage.events.FirstOrDefault ();
                bot = new BotService (_secretInfo.ChannelAccessToken, _secretInfo.AdminId, evt);
                db = new DatabaseService (bot, _db);
                if (db.IsAny (bot.UserInfo.userId)) {
                    var tempClass = Type.GetType (db.QueryClassName (bot.UserInfo.userId)).GetConstructor (new [] { typeof (BotService), typeof(FirestoreDb) }).Invoke (new object[] { bot, _db });
                    tempClass.GetType ().GetMethod ("NextQuestion").Invoke (tempClass, null);
                } else {
                    if ((bot.LineEvent.type.Equals ("message")) && (bot.LineEvent.message.text.Equals ("記帳"))) {
                        new Bookkeeping (bot, _db).NextQuestion ();
                    }
                }

                return Ok ();
            } catch (Exception ex) {
                _logger.LogError (ex.StackTrace);
                bot.Notify (ex);
                return Ok ();
            }
        }
    }

}