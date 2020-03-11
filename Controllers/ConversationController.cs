using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using isRock.LineBot;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenLineBot.Models.Conversation.Entity.Custom;
using OpenLineBot.Service;
namespace OpenLineBot.Controllers {
    [ApiController]
    [Route ("[controller]")]
    public class ConversationController : ControllerBase {
        private readonly ILogger<ConversationController> _logger;
        private readonly IConfiguration _config;

        private readonly SecretInfo _secretInfo;
        public ConversationController (ILogger<ConversationController> logger, IConfiguration config, SecretInfo secretInfo) {
            _logger = logger;
            _config = config;
            _secretInfo = secretInfo;
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
                db = new DatabaseService (bot);
                if (db.IsAny (bot.UserInfo.userId)) {
                    var tempClass = Type.GetType (db.QueryClassName (bot.UserInfo.userId)).GetConstructor (new [] { typeof (BotService) }).Invoke (new object[] { bot });
                    tempClass.GetType ().GetMethod ("NextQuestion").Invoke (tempClass, null);
                } else {
                    if ((bot.LineEvent.type.Equals ("message")) && (bot.LineEvent.message.text.Equals ("記帳"))) {
                        new Bookkeeping (bot).NextQuestion ();
                    }
                }

                return Ok ();
            } catch (Exception ex) {

                bot.Notify (ex);
                return Ok ();
            }
        }
    }

    

}