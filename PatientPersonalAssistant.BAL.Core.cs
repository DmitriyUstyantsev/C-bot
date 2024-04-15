//4)	Уровень бизнес-логики. PatientPersonalAssistant.BAL.Core.

namespace PatientPersonalAssistant.BAL.Core.Models
{
    public class Answer
    {
        public decimal Id { get; set; }
        public string TextOfAnswer { get; set; }
    }

    public class BranchBAL
    {
        public decimal Id { get; set; }
        public string Name { get; set; }
    }

    public class CorrelationOfAnswerAndDiagnosis
    {
        public decimal Id { get; set; }
        public decimal WeightOfAnswer { get; set; }
        public decimal AnswerToTheQuestionId { get; set; }
    }

    public class DataFromKnowlegeBase
    {
        public decimal Id { get; set; }
        public string Name { get; set; }
        public string Recommendation { get; set; }
        public List<CorrelationOfAnswerAndDiagnosis> Correlation { get; set; }

        public DataFromKnowlegeBase()
        {
            Correlation = new List<CorrelationOfAnswerAndDiagnosis>();
        }
    }

    public class ProcessedData
    {
        public string Name { get; set; }
        public string Recommendation { get; set; }
        public decimal Persent { get; set; }
    }

    public class QuestionWithAnswers
    {
        public decimal Id { get; set; }
        public string QuestionText { get; set; }
        public decimal BranchId { get; set; }
        public List<Answer> Answers { get; set; }

        public QuestionWithAnswers()
        {
            Answers = new List<Answer>();
        }
    }

    public class TelegramUser
    {
        public int TelegramId { get; set; }
        public string UserState { get; set; }
        public decimal BranchId { get; set; }
        public string CodeOfEntry { get; set; }
    }

    public class UserSurveyDataBAL
    {
        public string CodeOfEntry { get; set; }
        public int TelegramId { get; set; }
        public string Date { get; set; }
        public decimal AnswerToTheQuestionId { get; set; }
    }
}