using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Cloud.Firestore;
using isRock.LineBot;
using Newtonsoft.Json;
using OpenLineBot.Models.System;
using OpenLineBot.Service;

namespace OpenLineBot.Models.Conversation.Entity {
    abstract public class ConversationEntity : IConversationEntity {
        int _MaxOrder = 0;
        BotService _Bot = null;

        private readonly FirestoreDb _db;
        public ConversationEntity (BotService bot, FirestoreDb db) {
            _db = db;
            _Bot = bot;
            // if (!HasLastConfirm()) throw new Exception(new Error(ErrCode.S007).Message);
        }

        public int MaxOrder {
            get {
                var props = GetType ().GetProperties ();

                foreach (var prop in props) {
                    var orderAttr = ((Order[]) prop.GetCustomAttributes (typeof (Order), false)).FirstOrDefault ();
                    if (orderAttr != null) {
                        var id = orderAttr.Id;
                        if (id > _MaxOrder) {
                            _MaxOrder = id;
                        }
                    }
                }
                return _MaxOrder;
            }
        }

        public void PushQuestion (int order) {
            if (order > MaxOrder || order <= 0) throw new Exception (new Error (ErrCode.S005).Message);

            var props = GetType ().GetProperties ();
            foreach (var prop in props) {
                var orderAttr = ((Order[]) prop.GetCustomAttributes (typeof (Order), false)).FirstOrDefault ();
                if (orderAttr.Id == order) {
                    var questionAttr = ((IQuestion[]) prop.GetCustomAttributes (typeof (IQuestion), false)).FirstOrDefault ();
                    switch (questionAttr.PushType) {
                        case BotPushType.Text:
                            _Bot.PushMessage (((TextQuestion) questionAttr).TextResponse);
                            break;
                        case BotPushType.TextPicker:
                            _Bot.PushMessage (((TextPickerQuestion) questionAttr).ButtonTemplateResponse);
                            break;
                        case BotPushType.DataTimePicker:
                            _Bot.PushMessage (((DateTimePickerQuestion) questionAttr).ButtonTemplateResponse);
                            break;
                        case BotPushType.Confirm:
                            _Bot.PushMessage (((ConfirmQuestion) questionAttr).ConfirmTemplateResponse);
                            break;
                        case BotPushType.ImageCarousel:
                            _Bot.PushMessage (((DateTemplateQuestionAttribute) questionAttr).imageCarouselTemplate);
                            break;
                        default:
                            throw new Exception (new Error (ErrCode.S004).Message);
                    }
                    break;
                }
            }
        }

        public string GetQuestionTextOnly (int order) {
            if (order > MaxOrder || order <= 0) throw new Exception (new Error (ErrCode.S005).Message);

            var props = GetType ().GetProperties ();
            foreach (var prop in props) {
                var orderAttr = ((Order[]) prop.GetCustomAttributes (typeof (Order), false)).FirstOrDefault ();
                if (orderAttr.Id == order) {
                    var questionAttr = ((IQuestion[]) prop.GetCustomAttributes (typeof (IQuestion), false)).FirstOrDefault ();
                    return questionAttr.Question;
                }
            }

            throw new Exception (new Error (ErrCode.S011).Message);
        }

        public bool IsAnswerPassed (int order, string text) {
            if (order > MaxOrder || order <= 0) throw new Exception (new Error (ErrCode.S005).Message);

            bool pass = true;

            var props = GetType ().GetProperties ();
            foreach (var prop in props) {
                var orderAttr = ((Order[]) prop.GetCustomAttributes (typeof (Order), false)).FirstOrDefault ();
                if ((orderAttr != null) && (orderAttr.Id == order)) {
                    var answerAttr = ((Answer[]) prop.GetCustomAttributes (typeof (Answer), false)).FirstOrDefault ();
                    if (answerAttr != null) {
                        var filterType = answerAttr.Filter;
                        var filterObject = filterType.GetConstructors () [0].Invoke (null);
                        pass = (bool) filterType.GetMethod ("Pass").Invoke (filterObject, new object[] { text });
                        break;
                    }
                }

            }

            return pass;
        }

        public void PushComplaint (int order) {
            if (order > MaxOrder || order <= 0) throw new Exception (new Error (ErrCode.S005).Message);

            var props = GetType ().GetProperties ();
            foreach (var prop in props) {
                var orderAttr = ((Order[]) prop.GetCustomAttributes (typeof (Order), false)).FirstOrDefault ();
                if ((orderAttr != null) && (orderAttr.Id == order)) {
                    var answerAttr = ((Answer[]) prop.GetCustomAttributes (typeof (Answer), false)).FirstOrDefault ();
                    if (answerAttr != null) {
                        var complaint = answerAttr.Complaint;
                        _Bot.PushMessage (complaint);
                        break;
                    }
                }
            }
        }

        public bool HasLastConfirm () {
            var props = GetType ().GetProperties ();
            foreach (var prop in props) {
                var orderAttr = ((Order[]) prop.GetCustomAttributes (typeof (Order), false)).FirstOrDefault ();
                if (orderAttr.Id == MaxOrder) {
                    var confirmQuestionAttr = ((ConfirmQuestion[]) prop.GetCustomAttributes (typeof (ConfirmQuestion), false)).FirstOrDefault ();
                    if (confirmQuestionAttr == null) throw new Exception (new Error (ErrCode.S007).Message);
                    else return true;
                }
            }
            return false;
        }

        public virtual void NextQuestion () {
            DatabaseService service = new DatabaseService (_Bot, _db);

            string text = "";
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
                    service.AddRecord (_Bot.UserInfo.userId, lastQuestionNumber + 1, this.GetType ().FullName);
                    if (this.MaxOrder == lastQuestionNumber) {
                        List<TemplateActionBase> actions = new List<TemplateActionBase> ();
                        for (int i = 1; i <= this.MaxOrder; i++) {
                            foreach (PropertyInfo pi in this.GetType ().GetProperties ()) {
                                Order order = pi.GetCustomAttribute<Order> ();
                                if (order != null && order.Id == i) {
                                    actions.Add (new MessageAction () { label = this.GetQuestionTextOnly (i) + " " + pi.GetValue (this), text = pi.GetValue (this).ToString () });
                                }

                            }
                        }
                        Save();
                        Column column = new Column () { thumbnailImageUrl = new Uri ("https://beauty-upgrade.tw/wp-content/uploads/2019/06/6-12.jpg"), title = "明細", actions = actions, text = "你的明細如下" };
                        CarouselTemplate template = new CarouselTemplate () { columns = new List<Column> () { column } };
                        service.Remove (_Bot.UserInfo.userId, this.GetType ().FullName);
                        TemplateMessage a = new TemplateMessage (template);
                        _Bot.PushMessage (a);
                    } else {
                        this.PushQuestion (lastQuestionNumber + 1);
                    }
                } else {
                    this.PushComplaint (lastQuestionNumber);
                }

            }

        }

        public abstract void Save();
    }
}