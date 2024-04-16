// 3)	Уровень доступа к данным. PatientPersonalAssistant.DAL.MSSQLServer.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PatientPersonalAssistant.BAL.Core.Models;
using PatientPersonalAssistant.DAL.Models;

namespace PatientPersonalAssistant.DAL.MSSQLServer
{
    public class TelegramBotRepository : ITelegramBotRepository
    {
        private readonly string connectionString;

        public TelegramBotRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<QuestionWithAnswers> GetQuestionWithAnswersByQuestionIdAsync(decimal Id)
        {
            using (PatientPersonalAssistantDbContext db = new PatientPersonalAssistantDbContext(connectionString))
            {
                QuestionWithAnswers questionWithAnswers = new QuestionWithAnswers();
                var questionWithAnswersDb = await db.Question.Include(answers => answers.AnswerToTheQuestion).FirstOrDefaultAsync(q => q.Id == Id).ConfigureAwait(false);
                questionWithAnswers.Id = questionWithAnswersDb.Id;
                questionWithAnswers.QuestionText = questionWithAnswersDb.QuestionText;
                questionWithAnswers.BranchId = questionWithAnswersDb.BranchId;
                foreach (var answer in questionWithAnswersDb.AnswerToTheQuestion)
                {
                    questionWithAnswers.Answers.Add(new Answer { Id = answer.Id, TextOfAnswer = answer.TextOfAnswer });
                }
                return questionWithAnswers;
            }
        }

        public async Task<IEnumerable<QuestionWithAnswers>> GetQuestionsWithAnswersByBranchIdAsync(decimal branchId)
        {
            using (PatientPersonalAssistantDbContext db = new PatientPersonalAssistantDbContext(connectionString))
            {
                var questionsWithAnswersList = new List<QuestionWithAnswers>();
                var questionsWithAnswers = db.Question.Include(answers => answers.Answers).Where(q => q.BranchId == branchId);
                foreach (var questionWithAnswersDb in questionsWithAnswers)
                {
                    var questionWithAnswers = new QuestionWithAnswers
                    {
                        Id = questionWithAnswersDb.Id,
                        QuestionText = questionWithAnswersDb.QuestionText,
                        BranchId = questionWithAnswersDb.BranchId
                    };
                    foreach (var answer in questionWithAnswersDb.Answers)
                    {
                        questionWithAnswers.Answers.Add(new Answer { Id = answer.Id, TextOfAnswer = answer.TextOfAnswer });
                    }
                    questionsWithAnswersList.Add(questionWithAnswers);
                }
                return questionsWithAnswersList;
            }
        }

        public async Task AddToUserSurveyDataAsync(UserSurveyDataBAL userSurvey)
        {
            using (PatientPersonalAssistantDbContext db = new PatientPersonalAssistantDbContext(connectionString))
            {
                UserSurveyData userSurveyData = new UserSurveyData
                {
                    CodeOfEntry = userSurvey.CodeOfEntry,
                    Date = userSurvey.Date,
                    TelegramId = userSurvey.TelegramId,
                    AnswerToTheQuestionId = userSurvey.AnswerToTheQuestionId
                };
                await db.UserSurveyData.AddAsync(userSurveyData).ConfigureAwait(false);
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<decimal>> GetUserSurveyDataByEntryCodeAsync(string entryCode)
        {
            using (PatientPersonalAssistantDbContext db = new PatientPersonalAssistantDbContext(connectionString))
            {
                var dataList = new List(); var data = db.Diagnosis.Where(d => d.BranchId == branchId).Include(a => a.DiagnosisAnswerToTheQuestion); foreach (var value in data)
                {
                    Console.WriteLine(value.Name); DataFromKnowlegeBase dataFromKnowlegeBase = new DataFromKnowlegeBase { Id = value.Id, Name = value.Name, Recommendation = value.Recommendation };

                    foreach (var corr in value.DiagnosisAnswerToTheQuestion)
                    {
                        dataFromKnowlegeBase.Correlation.Add(new CorrelationOfAnswerAndDiagnosis
                        {
                            Id = corr.Id,
                            AnswerToTheQuestionId = corr.AnswerToTheQuestionId,
                            WeightOfAnswer = corr.WeightOfAnswer
                        });
                    }
                    dataList.Add(dataFromKnowlegeBase);
                }
                return dataList;
            }
        }

        public async Task<decimal> GetBranchWeightByBranchIdAsync(decimal branchId)
        {
            using (PatientPersonalAssistantDbContext db = new PatientPersonalAssistantDbContext(connectionString))
            {
                var branch = await db.Branch.FirstOrDefaultAsync(q => q.Id == branchId).ConfigureAwait(false);
                return branch.TotalWeightOfBranchOfQuestion;
            }
        }

        public async Task<string> GetTextOfAnswerByIdAsync(string textOfAnswerId)
        {
            using (PatientPersonalAssistantDbContext db = new PatientPersonalAssistantDbContext(connectionString))
            {
                var res = await db.AnswerToTheQuestion.FirstOrDefaultAsync(q => q.Id == Convert.ToInt32(textOfAnswerId)).ConfigureAwait(false);
                return res.TextOfAnswer;
            }
        }

        public async Task<string> GetBranchNameByIdAsync(string branchId)
        {
            using (PatientPersonalAssistantDbContext db = new PatientPersonalAssistantDbContext(connectionString))
            {
                var res = await db.Branch.FirstOrDefaultAsync(q => q.Id == Convert.ToInt32(branchId)).ConfigureAwait(false);
                return res.Name;
            }
        }
    }
}

