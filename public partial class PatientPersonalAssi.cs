// 1)	Стартовое приложение PatientPersonalAssistantApp.

using System.IO;
using Microsoft.Extensions.Configuration;
using PatientPersonalAssistant.BAL;
using PatientPersonalAssistant.DAL.MSSQLServer;

namespace PatientPersonalAssistantApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = SetConfiguration();
            var token = config["Token"];
            var connectionString = config.GetConnectionString("DefaultConnection");

            TelegramBotManager telegramBotManager = new TelegramBotManager(token, new TelegramBotRepository(connectionString));
            telegramBotManager.StartTheBot();
        }

        private static IConfiguration SetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            return builder.Build();
        }
    }
}

//2)	Уровень доступа к данным. PatientPersonalAssistant.DAL.

using Microsoft.EntityFrameworkCore;
using PatientPersonalAssistant.DAL.Models;

namespace PatientPersonalAssistant.DAL
{
    public partial class PatientPersonalAssistantDbContext : DbContext
    {
        private readonly string connectionString;

        public PatientPersonalAssistantDbContext() { }

        public PatientPersonalAssistantDbContext(string connectionString) => this.connectionString = connectionString;

        public PatientPersonalAssistantDbContext(DbContextOptions<PatientPersonalAssistantDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AnswerToTheQuestion> AnswerToTheQuestion { get; set; }
        public virtual DbSet<Branch> Branch { get; set; }
        public virtual DbSet<Diagnosis> Diagnosis { get; set; }
        public virtual DbSet<DiagnosisAnswerToTheQuestion> DiagnosisAnswerToTheQuestion { get; set; }
        public virtual DbSet<Question> Question { get; set; }
        public virtual DbSet<UserSurveyData> UserSurveyData { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(this.connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnswerToTheQuestion>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.QuestionId).HasColumnType("numeric(10, 0)");

                entity.Property(e => e.TextOfAnswer)
                    .IsRequired()
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.HasOne(d => d.Question)
                    .WithMany(p => p.AnswerToTheQuestion)
                    .HasForeignKey(d => d.QuestionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Branch>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Diagnosis>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.BranchId).HasColumnType("numeric(10, 0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Recommendation)
                    .IsRequired()
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.HasOne(d => d.Branch)
                    .WithMany(p => p.Diagnosis)
                    .HasForeignKey(d => d.BranchId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<DiagnosisAnswerToTheQuestion>(entity =>
            {
                entity.ToTable("Diagnosis_AnswerToTheQuestion");

                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.AnswerToTheQuestionId).HasColumnType("numeric(10, 0)");

                entity.Property(e => e.DiagnosisId).HasColumnType("numeric(10, 0)");

                entity.Property(e => e.WeightOfAnswer).HasColumnType("numeric(10, 0)");

                entity.HasOne(d => d.AnswerToTheQuestion)
                    .WithMany(p => p.DiagnosisAnswerToTheQuestion)
                    .HasForeignKey(d => d.AnswerToTheQuestionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Diagnosis)
                    .WithMany(p => p.DiagnosisAnswerToTheQuestion)
                    .HasForeignKey(d => d.DiagnosisId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.BranchId).HasColumnType("numeric(10, 0)");

                entity.Property(e => e.QuestionText)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.HasOne(d => d.Branch)
                    .WithMany(p => p.Question)
                    .HasForeignKey(d => d.BranchId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<UserSurveyData>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.AnswerToTheQuestionId).HasColumnType("numeric(10, 0)");

                entity.Property(e => e.CodeOfEntry)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Date)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.HasOne(d => d.AnswerToTheQuestion)
                    .WithMany(p => p.UserSurveyData)
                    .HasForeignKey(d => d.AnswerToTheQuestionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

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

//4)	Уровень бизнес-логики. PatientPersonalAssistant.BAL.Core.

namespace PatientPersonalAssistant.BAL.Core.Models
{
public class Answer
{
public decimal Id { get; set; }
public string TextOfAnswer { get; set; }
}
}

namespace PatientPersonalAssistant.BAL.Core.Models
{
public class BranchBAL
{
public decimal Id { get; set; } public string Name { get; set; }
}
}

namespace PatientPersonalAssistant.BAL.Core.Models
{
public class CorrelationOfAnswerAndDiagnosis
{
public decimal Id { get; set; }
public decimal WeightOfAnswer { get; set; }  public decimal AnswerToTheQuestionId { get; set; }
}
}

using System.Collections.Generic;


namespace PatientPersonalAssistant.BAL.Core.Models
{
public class DataFromKnowlegeBase
{
public decimal Id { get; set; } public string Name { get; set; }
public string Recommendation { get; set; }


public List<CorrelationOfAnswerAndDiagnosis> Correlation { get; set; }

public DataFromKnowlegeBase()
{
Correlation = new List<CorrelationOfAnswerAndDiagnosis>();
}
}
}

namespace PatientPersonalAssistant.BAL.Core.Models
{
public class ProcessedData
{
public string Name { get; set; }
public string Recommendation { get; set; } public decimal Persent { get; set; }
}
}

using System.Collections.Generic;


namespace PatientPersonalAssistant.BAL.Core.Models
{
public class QuestionWithAnswers
{
public decimal Id { get; set; }
public string QuestionText { get; set; } public decimal BranchId { get; set; } public List<Answer> Answers { get; set; }

public QuestionWithAnswers()
{
Answers = new List<Answer>();
}
}
}

namespace PatientPersonalAssistant.BAL.Core.Models
{
public class TelegramUser
{
public int TelegaramId { get; set; } public string UserState { get; set; } public decimal BranchId { get; set; }
public string CodeOfEntry { get; set; }
}
}

namespace PatientPersonalAssistant.BAL.Core.Models
{
public class UserSurveyDataBAL
{
public string CodeOfEntry { get; set; } public int TelegramId { get; set; } public string Date { get; set; }
public decimal AnswerToTheQuestionId { get; set; }
}
}

// 5)	Уровень бизнес-логики. PatientPersonalAssistant.BAL.
 
using PatientPersonalAssistant.BAL.Core.Models;
using PatientPersonalAssistant.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PatientPersonalAssistant.BAL
{
    public class TelegramBotManager
    {
        private readonly string token;
        private readonly ITelegramBotRepository telegramBotRepos;
        private static TelegramBotClient Bot;
        private List<TelegramUser> users = new List<TelegramUser>();

        public TelegramBotManager(string token, ITelegramBotRepository telegramBotRepos)
        {
            this.token = token;
            this.telegramBotRepos = telegramBotRepos;
        }

        public void StartTheBot()
        {
            Bot = new TelegramBotClient(token);
            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnCallbackQuery += BotCallbackQueryReceived;
            Bot.StartReceiving();
            Console.WriteLine("Press any key to stop the bot...");
            Console.ReadKey();
            Bot.StopReceiving();
        }

        private async void BotOnMessageReceived(object sender, MessageEventArgs e)
        {
            var message = e.Message;
            if (message == null || message.Type != MessageType.Text)
                return;

            string userName = $"{message.From.Id} {message.From.FirstName} {message.From.LastName}";
            Console.WriteLine($"Сообщение от {userName}: {message.Text}");

            if (message.Text == "/start")
            {
                string mes = "Введите команду /entry, чтобы начать опрос.";
                await Bot.SendTextMessageAsync(message.Chat.Id, mes);
            }
            else if (message.Text == "/entry")
            {
                if (users.Any(u => u.TelegramId == message.From.Id))
                {
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Пожалуйста, окончите начатый опрос.");
                }
                else
                {
                    await MakeEntryAsync(message);
                }
            }
        }
private async void BotCallbackQueryReceived(object sender, CallbackQueryEventArgs e)
{
var callback = e.CallbackQuery; string userAnswer = callback.Data;
string[] userAnswerSplit = userAnswer.Split("_"); Console.WriteLine($"{callback.From.Id}
{callback.From.FirstName}");
Console.WriteLine($"{callback.Data}");
if (users.FirstOrDefault(u => u.TelegaramId == Convert.ToInt32(userAnswerSplit[2])) == null)
{
"");
 
await Bot.AnswerCallbackQueryAsync(callback.Id,
await Bot.SendTextMessageAsync(callback.From.Id,
 
"С прошлого опроса прошло слишком много времени. Пожалуйста, начните новый опрос с помощью команды /entry.");
}
else
{
switch (userAnswerSplit[1]) 
{
case "firstQuestion": users.FirstOrDefault(u => u.TelegaramId
== callback.From.Id).BranchId = Convert.ToDecimal(userAnswerSplit[0]);
await ResponseToFirstQuestionAsync(callback, userAnswerSplit[0]).ConfigureAwait(false);
break; case "survey":
await ResponseToUserAsync(callback, userAnswerSplit[0]).ConfigureAwait(false);
break;
case "lastQuestion":
if (userAnswerSplit[0] == "-1") await
GetRecomendationAsync(callback).ConfigureAwait(false);
else
{
users.Remove(users.FirstOrDefault(u
=> u.TelegaramId == callback.From.Id));
await Bot.AnswerCallbackQueryAsync(callback.Id, "До новых встреч!");
await Bot.DeleteMessageAsync(callback.Message.Chat.Id, callback.Message.MessageId);
}
break;
}
}
}
private async Task AddUserSurveyDataToDbAsync(CallbackQuery callback, string answerId)
 
{
UserSurveyDataBAL userSurveyData = new
UserSurveyDataBAL
{
TelegramId = callback.From.Id, CodeOfEntry = users.FirstOrDefault(u =>
u.TelegaramId == callback.From.Id).CodeOfEntry,
Date = DateTime.Today.ToString("d"), AnswerToTheQuestionId =
Convert.ToInt32(answerId)
};
await telegramBotRepos.AddToUserSurveyDataAsync(userSurveyData).Config ureAwait(false);
}
private async Task ResponseToFirstQuestionAsync(CallbackQuery callback, string answerId)
{
var answerText = await telegramBotRepos.GetBranchNameById(answerId);
try
{
await Bot.DeleteMessageAsync(callback.Message.Chat.Id, callback.Message.MessageId);
await Bot.SendTextMessageAsync(callback.From.Id,
$"{callback.Message.Text} Ваш ответ: {answerText}");
await Bot.AnswerCallbackQueryAsync(callback.Id, "Ответ записан.");
Console.WriteLine($"{answerId} {answerText}"); users.FirstOrDefault(u => u.TelegaramId ==
callback.From.Id).UserState = "Go";
}
 
catch
{
Console.WriteLine("Error");
}
}
private async Task ResponseToUserAsync(CallbackQuery callback, string answerId)
{
var answerText = await telegramBotRepos.GetTextOfAnswerById(answerId);
try
{
await Bot.DeleteMessageAsync(callback.Message.Chat.Id, callback.Message.MessageId);
await AddUserSurveyDataToDbAsync(callback, answerId).ConfigureAwait(false);
await Bot.SendTextMessageAsync(callback.From.Id,
$"{callback.Message.Text} Ваш ответ: {answerText}");
await Bot.AnswerCallbackQueryAsync(callback.Id, "Ответ записан.");
Console.WriteLine($"{answerId} {answerText}"); users.FirstOrDefault(u => u.TelegaramId ==
callback.From.Id).UserState = "Go";
}
catch
{
Console.WriteLine("Error");
}
}
private async Task GetRecomendationAsync(CallbackQuery callback)
{
var entryCode = users.FirstOrDefault(u => u.TelegaramId == callback.From.Id).CodeOfEntry;
var branchId = users.FirstOrDefault(u => u.TelegaramId == callback.From.Id).BranchId;
var userData = await telegramBotRepos.GetUserSurveyDataByEntryCodeAsync(entryCode);
var dataFromKnowlegeBase = await telegramBotRepos.GetDataFromKnowledgeBaseByBranchIdAsync(branchI d);
var possibleResults = await AnalyzeDataAsync(userData, dataFromKnowlegeBase, callback.From.Id);
if (possibleResults.Any())
{
await Bot.DeleteMessageAsync(callback.Message.Chat.Id, callback.Message.MessageId);
var res = possibleResults.First();
await Bot.SendTextMessageAsync(callback.From.Id,
$"С вероятностью {res.Persent}% у Вас {res.Name}. " +
$"{res.Recommendation}"); users.Remove(users.FirstOrDefault(u =>
u.TelegaramId == callback.From.Id));
}
else
{
await Bot.DeleteMessageAsync(callback.Message.Chat.Id, callback.Message.MessageId);
await Bot.SendTextMessageAsync(callback.From.Id, "Извините, подходящая рекомендация не найдена.");
users.Remove(users.FirstOrDefault(u => u.TelegaramId == callback.From.Id));
}
}
private async Task<IEnumerable<ProcessedData>> AnalyzeDataAsync(IEnumerable<decimal> userData,
IEnumerable<DataFromKnowlegeBase> dataFromKnowlegeBase, int telegramId)
{
var processedDataList = new List<ProcessedData>(); var totalWeight = await
telegramBotRepos.GetBranchWeightByBranchIdAsync(
users.FirstOrDefault(u => u.TelegaramId == telegramId).BranchId);
foreach (var value in dataFromKnowlegeBase)
{
var answers = value.Correlation.Select(a => a.AnswerToTheQuestionId).ToList();
var result = answers.Intersect(userData); List<CorrelationOfAnswerAndDiagnosis>
machAnswers = new List<CorrelationOfAnswerAndDiagnosis>(); foreach (var val in value.Correlation)
if (result.Contains(val.AnswerToTheQuestionId))
machAnswers.Add(val); processedDataList.Add(new ProcessedData
{
Name = value.Name,
Recommendation = value.Recommendation, Persent = machAnswers.Sum(s =>
s.WeightOfAnswer) * 100 / totalWeight
});
}
return processedDataList.Where(p => p.Persent > 50).OrderBy(p => p.Persent).Take(1);
}
private async Task MakeEntryAsync(Message message)
 
{
TelegramUser user = new TelegramUser
{
TelegaramId = message.From.Id, UserState = "Wait",
CodeOfEntry = Guid.NewGuid().ToString()
};
users.Add(user);
await SendFirstQuestionAsync(message); if (Wait(message.From.Id) == false)
return;
var questionsWithAnswers = await telegramBotRepos.GetQuestionsWithAnswersByBranchIdAsync(
users.FirstOrDefault(u => u.TelegaramId == message.From.Id).BranchId).ConfigureAwait(false);
foreach (QuestionWithAnswers questionWithAnswers in questionsWithAnswers)
{
await SendInlineKeyboardAsync(questionWithAnswers, message, "survey").ConfigureAwait(false);
users.FirstOrDefault(u => u.TelegaramId == message.From.Id).UserState = "Wait";
if (Wait(message.From.Id) == false) return;
}
await Bot.SendTextMessageAsync(message.From.Id, "Спасибо, Ваша запись добавлена в журнал.");
await SendLastQuestionAsync(message).ConfigureAwait(false);
}


private async Task SendLastQuestionAsync(Message
message)
{
 
QuestionWithAnswers questionWithAnswers = new QuestionWithAnswers
{
QuestionText = "Хотите просмотреть рекомендации?"
};
questionWithAnswers.Answers.AddRange(new
List<Answer>
{
new Answer {Id = -1, TextOfAnswer = "Да" }, new Answer {Id = -2, TextOfAnswer = "Нет"}
});
await SendInlineKeyboardAsync(questionWithAnswers, message, "lastQuestion").ConfigureAwait(false);
}
private bool Wait(int telegramId)
{
    bool flag = false;
    var start = DateTime.Now;
    var end = start.AddHours(1);
    while (!flag && DateTime.Now < end)
    {
        if (users.FirstOrDefault(u => u.TelegramId == telegramId)?.UserState == "Go")
        {
            flag = true;
        }
        Task.Delay(100).Wait();
    }
    if (!flag)
    {
        users.RemoveAll(u => u.TelegramId == telegramId);
    }
    return flag;
}

private async Task SendFirstQuestionAsync(Message message)
{
    var result = await telegramBotRepos.GetAllBranchesAsync();
    QuestionWithAnswers currentQuestionWithAnswers = new QuestionWithAnswers { QuestionText = "Что Вас беспокоит?" };
    foreach (var branch in result)
    {
        currentQuestionWithAnswers.Answers.Add(new Answer { Id = branch.Id, TextOfAnswer = branch.Name });
    }
    await SendInlineKeyboardAsync(currentQuestionWithAnswers, message, "firstQuestion");
}

private async Task SendInlineKeyboardAsync(QuestionWithAnswers questionWithAnswers, Message message, string code)
{
    var inlineKeyboard = new InlineKeyboardMarkup(
        questionWithAnswers.Answers.Select(answer => new[]
        {
            InlineKeyboardButton.WithCallbackData(answer.TextOfAnswer, $"{answer.Id}_{code}_{message.From.Id}")
        })
    );

    await Bot.SendTextMessageAsync(message.Chat.Id, questionWithAnswers.QuestionText, replyMarkup: inlineKeyboard);
}
// 6) Модели данных. PatientPersonalAssistant.DAL.Models.

using System.Collections.Generic;

namespace PatientPersonalAssistant.DAL.Models { public partial class AnswerToTheQuestion { public AnswerToTheQuestion() { DiagnosisAnswerToTheQuestion = new HashSet(); UserSurveyData = new HashSet(); }

    public decimal Id { get; set; }
    public decimal QuestionId { get; set; }
    public string TextOfAnswer { get; set; }

    public virtual Question Question { get; set; }
    public virtual ICollection<DiagnosisAnswerToTheQuestion> DiagnosisAnswerToTheQuestion { get; set; }
    public virtual ICollection<UserSurveyData> UserSurveyData { get; set; }
}

}

// 8) Настройки приложения. appsettings.json.

// { "ConnectionStrings": { "DefaultConnection": "Server=(localdb)\mssqllocaldb;Database=PatientPersonalAssistantDb;Trusted_Connection=True;" }, "Token": "Your_Telegram_Bot_Token_Here" }