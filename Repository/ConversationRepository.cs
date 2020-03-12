using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using OpenLineBot.Models.System;
using OpenLineBot.Service;
namespace OpenLineBot.Repository {
    public class ConversationRepository {
        private readonly FirestoreDb _db;
        private BotService Bot;

        public ConversationRepository (BotService bot, FirestoreDb db) {
            _db = db;
            Bot = bot;

        }

        public async void AddRecord (string userId, int questionNumber, string className) {

            try {
                DocumentReference docRef = _db.Collection ("records").Document (userId);
                Dictionary<string, object> record = new Dictionary<string, object> { { "QuestionNumber", questionNumber },
                    { "Answer", "" },
                    { "ClassName", className }
                };
                if (docRef.GetSnapshotAsync ().Result.GetValue<List<Dictionary<string, object>>> ("list").Count > 0) {
                    await docRef.UpdateAsync ("list", FieldValue.ArrayUnion (record));
                } else {

                    await docRef.SetAsync (new { list = new List<Dictionary<string, object>> () { record } });
                }

            } catch (Exception ex) {
                Bot.PushMessage (ex.StackTrace);
                // Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

        }

        public async void Remove (string userId, string className) {

            try {
                DocumentSnapshot query = await _db.Collection ("records").Document (userId).GetSnapshotAsync ();
                List<Dictionary<string, object>> items = query.GetValue<List<Dictionary<string, object>>> ("list");
                items = items.Where (a => !a["ClassName"].Equals (className)).ToList<Dictionary<string, object>> ();
                await _db.Collection ("records").Document (userId).SetAsync (new { list = items });
            } catch (Exception ex) {
                Bot.PushMessage (ex.StackTrace);
                // Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

        }

        public async void Update (string userId, int questionNumber, string answer, string className) {
            try {
                DocumentSnapshot query = await _db.Collection ("records").Document (userId).GetSnapshotAsync ();
                List<Dictionary<string, object>> items = query.GetValue<List<Dictionary<string, object>>> ("list");
                Dictionary<string, object> item = items.Where (a => Convert.ToInt32 (a["QuestionNumber"]) == questionNumber && a["ClassName"].Equals (className)).First ();
                item["Answer"] = answer;
                await _db.Collection ("records").Document (userId).SetAsync (new { list = items });
            } catch (Exception ex) {
                Bot.PushMessage (ex.StackTrace);
                // Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

        }

        public async Task<bool> IsAny (string userId) {
            bool ret = false;

            try {
                QuerySnapshot query = await _db.Collection ("records").GetSnapshotAsync ();
                ret = query.Count == 0 ? false : _db.Collection ("records").Document (userId).GetSnapshotAsync ().Result.Exists;
            } catch (Exception ex) {
                Bot.PushMessage (ex.StackTrace);
                // Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

            return ret;
        }

        public async Task<int> LastQuestionNumber (string userId, string className) {
            int ret = 0;

            try {
                QuerySnapshot query = await _db.Collection ("records").GetSnapshotAsync ();
                ret = query.Count == 0 ? 0 : _db.Collection ("records").Document (userId).GetSnapshotAsync ().Result.GetValue<List<Dictionary<string, object>>> ("list")
                    .Select (a => Convert.ToInt32 (a["QuestionNumber"])).ToList ().Max ();
            } catch (Exception ex) {
                Bot.PushMessage (ex.StackTrace);
                //Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

            return ret;
        }

        public async Task<bool> HasAnswer (string userId, int questionNumber, string className) {
            bool ret = false;

            try {
                DocumentSnapshot query = await _db.Collection ("records").Document (userId).GetSnapshotAsync ();
                string answer = query.GetValue<List<Dictionary<string, object>>> ("list").Where (a => Convert.ToInt32 (a["QuestionNumber"]) == questionNumber && a["ClassName"].Equals (className))
                    .First () ["Answer"].ToString ();
                ret = !string.IsNullOrEmpty (answer);
            } catch (Exception ex) {
                Bot.PushMessage (ex.StackTrace);
                // Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

            return ret;
        }

        public async Task<string> QueryAnswer (string userId, int questionNumber, string className) {
            string answer = null;

            try {
                DocumentSnapshot query = await _db.Collection ("records").Document (userId).GetSnapshotAsync ();
                answer = query.GetValue<List<Dictionary<string, object>>> ("list").Where (a => Convert.ToInt32 (a["QuestionNumber"]) == questionNumber && a["ClassName"].Equals (className))
                    .First () ["Answer"].ToString ();

            } catch (Exception ex) {
                Bot.PushMessage (ex.StackTrace);
                //Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

            return answer;
        }

        public async Task<string> QueryClassName (string userId) {
            string className = null;

            try {
                DocumentSnapshot query = await _db.Collection ("records").Document (userId).GetSnapshotAsync ();
                className = query.GetValue<List<Dictionary<string, object>>> ("list").First () ["ClassName"].ToString ();
            } catch (Exception ex) {
                Bot.PushMessage (ex.StackTrace);
                // Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

            return className;
        }
    }
}