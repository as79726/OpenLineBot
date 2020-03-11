using System;
using System.Collections.Generic;
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

        [ActivatorUtilitiesConstructor]
        public ConversationRepository (FirestoreDb db) {
            _db = db;
        }
        public ConversationRepository (BotService bot) {

            Bot = bot;

        }

        public async void AddRecord (string userId, int questionNumber, string className) {

            try {
                CollectionReference docRef = _db.Collection ("records").Document (userId).Collection ("list");
                Dictionary<string, object> record = new Dictionary<string, object> { { "QuestionNumber", questionNumber },
                    { "Answer", "" },
                    { "ClassName", className }
                };
                await docRef.AddAsync (record);
            } catch (Exception ex) {
                Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

        }

        public async void Remove (string userId, string className) {

            try {
                QuerySnapshot query = await _db.Collection ("records").Document (userId).Collection ("list").WhereEqualTo ("className", className).GetSnapshotAsync ();
                IReadOnlyList<DocumentSnapshot> documents = query.Documents;
                while (documents.Count > 0) {
                    foreach (DocumentSnapshot document in documents) {
                        await document.Reference.DeleteAsync ();
                    }
                }
            } catch (Exception ex) {
                Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

        }

        public async void Update (string userId, int questionNumber, string answer, string className) {
            try {
                QuerySnapshot query = await _db.Collection ("records").Document (userId).Collection ("list").WhereEqualTo ("className", className).WhereEqualTo ("QuestionNumber", questionNumber).GetSnapshotAsync ();
                Dictionary<string, object> record = new Dictionary<string, object> { { "Answer", "" }
                };
                await query.Documents.First ().Reference.UpdateAsync (record);
            } catch (Exception ex) {
                Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

        }

        public async Task<bool> IsAny (string userId) {
            bool ret = false;

            try {
                QuerySnapshot query = await _db.Collection ("records").Document (userId).Collection ("list").GetSnapshotAsync ();
                ret = query.Documents.Count () == 0 ? false : true;
            } catch (Exception ex) {
                Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

            return ret;
        }

        public async Task<int> LastQuestionNumber (string userId, string className) {
            int ret = 0;

            try {
                QuerySnapshot query = await _db.Collection ("records").Document (userId).Collection ("list").WhereEqualTo ("className", className).OrderByDescending ("QuestionNumber").GetSnapshotAsync ();
                ret = query.Documents.First ().GetValue<int> ("QuestionNumber");
            } catch (Exception ex) {
                Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

            return ret;
        }

        public async Task<bool> HasAnswer (string userId, int questionNumber, string className) {
            bool ret = false;

            try {
                QuerySnapshot query = await _db.Collection ("records").Document (userId).Collection ("list").WhereEqualTo ("className", className).WhereEqualTo ("QuestionNumber", questionNumber).GetSnapshotAsync ();
                string answer = query.Documents.First ().GetValue<string> ("Answer");
                ret = !string.IsNullOrEmpty (answer);
            } catch (Exception ex) {
                Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

            return ret;
        }

        public async Task<string> QueryAnswer (string userId, int questionNumber, string className) {
            string answer = null;

            try {
                QuerySnapshot query = await _db.Collection ("records").Document (userId).Collection ("list").WhereEqualTo ("className", className).WhereEqualTo ("QuestionNumber", questionNumber).GetSnapshotAsync ();
                answer = query.Documents.First ().GetValue<string> ("Answer");

            } catch (Exception ex) {
                Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

            return answer;
        }

        public async Task<string> QueryClassName (string userId) {
            string className = null;

            try {
                QuerySnapshot query = await _db.Collection ("records").Document (userId).Collection ("list").GetSnapshotAsync ();
                className = query.Documents.First ().GetValue<string> ("ClassName");
            } catch (Exception ex) {
                Bot.Notify (new Exception (new Error (ErrCode.D001, Bot.UserInfo.userId, ex.Message).Message));
            }

            return className;
        }
    }
}