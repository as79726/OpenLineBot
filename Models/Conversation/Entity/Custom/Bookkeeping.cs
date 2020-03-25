using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Google.Cloud.Firestore;
using OpenLineBot.Service;

namespace OpenLineBot.Models.Conversation.Entity.Custom {
    public class Bookkeeping : ConversationEntity {
        BotService _bot = null;
        FirestoreDb _db = null;
        DatabaseService service = null;
        public Bookkeeping (BotService bot, FirestoreDb db) : base (bot, db) {
            _bot = bot;
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

        [Order (1)]
        [TextQuestion ("品名")]
        public string item { get; set; }

        [Order (2)]
        [TextQuestion ("金額")]
        [Answer (typeof (MoneyFilter), "給個數目好嗎!")]
        public string money { get; set; }

        [Order (3)]
        [DateTemplateQuestion ("記帳日期", @"https://d26hyti2oua2hb.cloudfront.net/600/arts/201904291424-BqA1d.jpg")]
        [Answer (typeof (DateFilter), "選日期, 不要自己打")]
        public string bookDate { get; set; }
        /* [Order(4)]
         [ConfirmQuestion("要提交了嗎?")]
         [Answer(typeof(SubmitFilter), "用選的，不要自己亂回!")]
         public string Submit { get; set; }*/

        public override void Save () {
            service.AddFoodRecord(_bot.UserInfo.userId, this.item, this.money, this.bookDate);
        }
    }
}