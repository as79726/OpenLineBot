using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Google.Cloud.Firestore;
using isRock.LineBot;
using OpenLineBot.Models;
using OpenLineBot.Models.Conversation;
using OpenLineBot.Models.System;
using OpenLineBot.Service;
namespace OpenLineBot.Models.Conversation.Entity.Custom {
    public class Calculation : ConversationEntity {
        public Calculation (BotService bot, FirestoreDb db) : base (bot, db) {
            _Bot = bot;
            _db = db;
            service = new DatabaseService (bot, _db);
            if (service.IsAny (bot.UserInfo.userId)) {
                foreach (PropertyInfo pi in this.GetType ().GetProperties ()) {
                    Order order = pi.GetCustomAttribute<Order> ();
                    if (order != null) {
                        string value = service.QueryAnswer (bot.UserInfo.userId, order.Id, this.GetType ().FullName);
                        if (!string.IsNullOrEmpty (value)) {

                            pi.SetValue (this, value);

                        }
                    }

                }
            }
        }

        BotService _Bot = null;
        private readonly FirestoreDb _db;
        DatabaseService service = null;
        [Order (1)]
        [DateTemplateQuestion ("開始時間,從選擇的日期月份開始", @"https://d26hyti2oua2hb.cloudfront.net/600/arts/201904291424-BqA1d.jpg")]
        [Answer (typeof (DateFilter), "選日期, 不要自己打")]
        public string startDate { get; set; }

        [Order (2)]
        [DateTemplateQuestion ("結束時間,從選擇的日期月份結束", @"https://d26hyti2oua2hb.cloudfront.net/600/arts/201904291424-BqA1d.jpg")]
        [Answer (typeof (DateFilter), "選日期, 不要自己打")]
        public string endDate { get; set; }

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
                            QuerySnapshot query = _db.Collection (_Bot.UserInfo.userId).GetSnapshotAsync ().Result;
                            DateTime startDate = DateTime.Parse (this.startDate);
                            DateTime endDate = DateTime.Parse (this.endDate);
                            if (query.Count > 0) {
                                List<MessageBase> messages = query.Where (a => DateTime.Parse (a.Id) >= new DateTime (startDate.Year, startDate.Month, 1) && DateTime.Parse (a.Id) <= endDate.AddMonths (1).AddDays (-1)).Select (a => new { key = a.Id, value = a.GetValue<List<Dictionary<string, object>>> ("list").Sum (a => Decimal.Parse(a["Money"].ToString())) })
                                    .GroupBy (a => DateTime.Parse (a.key).ToString ("yyyy/MM")).Select (a => {
                                        Decimal total = a.Sum (a => a.value);
                                        string text = a.Key + " 總金額:" + total;
                                        return new TextMessage (text);
                                    }).ToList<MessageBase> ();

                                _Bot.ReplyMessage (_Bot.LineEvent.replyToken, messages);
                            } else {
                                _Bot.ReplyMessage (_Bot.LineEvent.replyToken, @"範圍區間無消費");
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
                _Bot.Notify (ex);
            }
        }
    }
}