using OpenLineBot.Repository;
using Google.Cloud.Firestore;

namespace OpenLineBot.Service
{
    public class DatabaseService
    {
        ConversationRepository Repos;

        public DatabaseService(BotService bot, FirestoreDb db)
        {
            Repos = new ConversationRepository(bot, db);
        }

        public void AddRecord(string userId, int questionNumber, string className)
        {
            Repos.AddRecord(userId, questionNumber, className);
        }

        public void Remove(string userId, string className)
        {
            Repos.Remove(userId, className);
        }

        public void Update(string userId, int questionNumber, string answer, string className)
        {
            Repos.Update(userId, questionNumber, answer, className);
        }

        public bool IsAny(string userId)
        {
            return Repos.IsAny(userId).Result;
        }

        public int LastQuestionNumber(string userId, string className)
        {
            return Repos.LastQuestionNumber(userId, className).Result;
        }

        public bool HasAnswer(string userId, int questionNumber, string className)
        {
            return Repos.HasAnswer(userId, questionNumber, className).Result;
        }

        public string QueryAnswer(string userId, int questionNumber, string className) 
        {
            return Repos.QueryAnswer(userId, questionNumber, className).Result;
        }

        public string QueryClassName(string userId)
        {
            return Repos.QueryClassName(userId).Result;;
        }
    }
}