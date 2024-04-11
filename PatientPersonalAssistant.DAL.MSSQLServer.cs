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
        private readonly PatientPersonalAssistantDbContext _context;

        public TelegramBotRepository(string connectionString)
        {
            var options = new DbContextOptionsBuilder<PatientPersonalAssistantDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            _context = new PatientPersonalAssistantDbContext(options);
        }

        public async Task<QuestionWithAnswers> GetQuestionWithAnswersByQuestionIdAsync(decimal id)
        {
            var questionWithAnswersDb = await _context.Question
                .Include(answers => answers.AnswerToTheQuestion)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (questionWithAnswersDb == null)
                return null;

            var questionWithAnswers = new QuestionWithAnswers
            {
                Id = questionWithAnswersDb.Id,
                QuestionText = questionWithAnswersDb.QuestionText,
                BranchId = questionWithAnswersDb.BranchId,
                Answers = questionWithAnswersDb.AnswerToTheQuestion
                    .Select(a => new Answer { Id = a.Id, TextOfAnswer = a.TextOfAnswer })
                    .ToList()
            };

            return questionWithAnswers;
        }

        public async Task<IEnumerable<QuestionWithAnswers>> GetQuestionsWithAnswersByBranchIdAsync(decimal branchId)
        {
            var questionsWithAnswers = await _context.Question
                .Include(answers => answers.AnswerToTheQuestion)
                .Where(q => q.BranchId == branchId)
                .Select(q => new QuestionWithAnswers
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    BranchId = q.BranchId,
                    Answers = q.AnswerToTheQuestion
                        .Select(a => new Answer { Id = a.Id, TextOfAnswer = a.TextOfAnswer })
                        .ToList()
                })
                .ToListAsync();

            return questionsWithAnswers;
        }

        public async Task AddToUserSurveyDataAsync(UserSurveyDataBAL userSurvey)
        {
            var userSurveyData = new UserSurveyData
            {
                CodeOfEntry = userSurvey.CodeOfEntry,
                Date = userSurvey.Date,
                TelegramId = userSurvey.TelegramId,
                AnswerToTheQuestionId = userSurvey.AnswerToTheQuestionId
            };

            await _context.UserSurveyData.AddAsync(userSurveyData);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<decimal>> GetUserSurveyDataByEntryCodeAsync(string entryCode)
        {
            var userAnswers = await _context.UserSurveyData
                .Where(d => d.CodeOfEntry == entryCode)
                .Select(ua => ua.AnswerToTheQuestionId)
                .ToListAsync();

            return userAnswers;
        }

        public async Task<IEnumerable<BranchBAL>> GetAllBranchesAsync()
        {
            var branches = await _context.Branch
                .Select(b => new BranchBAL
                {
                    Id = b.Id,
                    Name = b.Name
                })
                .ToListAsync();

            return branches;
        }

        public async Task<IEnumerable<DataFromKnowlegeBase>> GetDataFromKnowledgeBaseByBranchIdAsync(decimal branchId)
        {
            var data = await _context.Diagnosis
                .Where(d => d.BranchId == branchId)
                .Include(a => a.DiagnosisAnswerToTheQuestion)
                .Select(value => new DataFromKnowlegeBase
                {
                    Id = value.Id,
                    Name = value.Name,
                    Recommendation = value.Recommendation,
                    Correlation = value.DiagnosisAnswerToTheQuestion
                        .Select(corr => new CorrelationOfAnswerAndDiagnosis
                        {
                            Id = corr.Id,
                            AnswerToTheQuestionId = corr.AnswerToTheQuestionId,
                            WeightOfAnswer = corr.WeightOfAnswer
                        })
                        .ToList()
                })
                .ToListAsync();

            return data;
        }

        public async Task<decimal> GetBranchWeightByBranchIdAsync(decimal branchId)
        {
            var branch = await _context.Branch
                .FirstOrDefaultAsync(q => q.Id == branchId);

            return branch?.TotalWeightOfBranchOfQuestion ?? 0;
        }

        public async Task<string> GetTextOfAnswerById(string textOfAnswerId)
        {
            var res = await _context.AnswerToTheQuestion
                .FirstOrDefaultAsync(q => q.Id == Convert.ToDecimal(textOfAnswerId));

            return res?.TextOfAnswer;
        }

        public async Task<string> GetBranchNameById(string branchId)
        {
            var res = await _context.Branch
                .FirstOrDefaultAsync(q => q.Id == Convert.ToDecimal(branchId));

            return res?.Name;
        }
    }
}