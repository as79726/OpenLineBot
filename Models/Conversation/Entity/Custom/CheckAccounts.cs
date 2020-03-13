using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Google.Cloud.Firestore;
using isRock.LineBot;
using OpenLineBot.Models;
using OpenLineBot.Models.System;
using OpenLineBot.Service;
namespace OpenLineBot.Models.Conversation.Entity.Custom {
    public class CheckAccounts : ConversationEntity {
        BotService _Bot = null;

        private readonly FirestoreDb _db;
        public CheckAccounts (BotService bot, FirestoreDb db) : base (bot, db) {
            _Bot = bot;
            _db = db;
        }

        [Order (1)]
        [DateTemplateQuestion ("記帳日期", @"https://d26hyti2oua2hb.cloudfront.net/600/arts/201904291424-BqA1d.jpg")]
        [Answer (typeof (DateFilter), "選日期, 不要自己打")]
        public string bookDate { get; set; }
        public override void NextQuestion () {
            DatabaseService service = new DatabaseService (_Bot, _db);

            string text = "";
            try {
                // Call successor
                switch (_Bot.LineEvent.type) {
                    case "message":
                        if (_Bot.LineEvent.message.type.Equals ("text")) {
                            text = _Bot.LineEvent.message.text;
                        } else {
                            throw new Exception (new Error (ErrCode.S010).Message);
                        }
                        break;
                    case "postback":
                        text = _Bot.LineEvent.postback.Params != null ? ((_Bot.LineEvent.postback.Params.datetime != null) ? _Bot.LineEvent.postback.Params.datetime : ((_Bot.LineEvent.postback.Params.date != null) ? _Bot.LineEvent.postback.Params.date : (_Bot.LineEvent.postback.Params.time != null) ? _Bot.LineEvent.postback.Params.time : _Bot.LineEvent.postback.data)) : _Bot.LineEvent.postback.data;
                        break;
                    default:
                        throw new Exception (new Error (ErrCode.S002).Message);
                }

                if (service.LastQuestionNumber (_Bot.UserInfo.userId, this.GetType ().FullName) == 0) {
                    service.AddRecord (_Bot.UserInfo.userId, 1, this.GetType ().FullName);
                    this.PushQuestion (1);
                } else {
                    int lastQuestionNumber = service.LastQuestionNumber (_Bot.UserInfo.userId, this.GetType ().FullName);
                    bool flag = this.IsAnswerPassed (lastQuestionNumber, text);
                    if (flag) {
                        foreach (PropertyInfo pi in this.GetType ().GetProperties ()) {
                            Order order = pi.GetCustomAttribute<Order> ();
                            if (order != null && order.Id == lastQuestionNumber) {
                                pi.SetValue (this, text);
                            }

                        }
                        service.Update (_Bot.UserInfo.userId, lastQuestionNumber, text, this.GetType ().FullName);

                        if (this.MaxOrder == lastQuestionNumber) {
                            DocumentSnapshot document = _db.Collection (_Bot.UserInfo.userId).Document (this.bookDate).GetSnapshotAsync ().Result;
                            if (document.Exists) {
                                List<Dictionary<string, object>> list = document.GetValue<List<Dictionary<string, object>>> ("list");
                                List<MessageBase> messages = list.Select (a => {
                                    string message = @"品項: " + a["Name"] + @" 金額: " + a["Money"];
                                    return new TextMessage (message);
                                }).ToList<MessageBase> ();

                                _Bot.ReplyMessage (_Bot.LineEvent.replyToken, messages);
                            } else {
                                _Bot.ReplyMessage (_Bot.LineEvent.replyToken, @"當日無消費");
                            }
                            service.Remove (_Bot.UserInfo.userId, this.GetType ().FullName);
                        } else {
                            service.AddRecord (_Bot.UserInfo.userId, lastQuestionNumber + 1, this.GetType ().FullName);
                            this.PushQuestion (lastQuestionNumber + 1);
                        }
                    } else {
                        this.PushComplaint (lastQuestionNumber);
                    }

                }
            } catch (Exception ex) {
                Console.WriteLine (ex.StackTrace);
                _Bot.Notify (ex);
            }
        }

    }
}