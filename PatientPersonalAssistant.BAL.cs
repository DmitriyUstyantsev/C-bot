// 5)    Уровень бизнес-логики. PatientPersonalAssistant.BAL.

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
            var callback = e.CallbackQuery;
            string userAnswer = callback.Data;
            string[] userAnswerSplit = userAnswer.Split("_");
            Console.WriteLine($"{callback.From.Id} {callback.From.FirstName}");
            Console.WriteLine($"{callback.Data}");

            if (users.FirstOrDefault(u => u.TelegramId == Convert.ToInt32(userAnswerSplit[2])) == null)
            {
                await Bot.AnswerCallbackQueryAsync(callback.Id);
                await Bot.SendTextMessageAsync(callback.From.Id, "С прошлого опроса прошло слишком много времени. Пожалуйста, начните новый опрос с помощью команды /entry.");
            }
            else
            {
                switch (userAnswerSplit[1])
                {
                    case "firstQuestion":
                        users.FirstOrDefault(u => u.TelegramId == callback.From.Id).UserState = "Go";
                        await ResponseToFirstQuestionAsync(callback, userAnswerSplit[0]);
                        break;

                    case "survey":
                        await ResponseToUserAsync(callback, userAnswerSplit[0]);
                        break;

                    case "lastQuestion":
                        if (userAnswerSplit[0] == "-1")
                            await GetRecomendationAsync(callback);
                        else
                        {
                            users.Remove(users.FirstOrDefault(u => u.TelegramId == callback.From.Id));
                            await Bot.AnswerCallbackQueryAsync(callback.Id, "До новых встреч!");
                            await Bot.DeleteMessageAsync(callback.Message.Chat.Id, callback.Message.MessageId);
                        }
                        break;
                }
            }
        }
        private async Task AddUserSurveyDataToDbAsync(CallbackQuery callback, string answerId)

        {
            UserSurveyDataBAL userSurveyData = new UserSurveyDataBAL
            {
                TelegramId = callback.From.Id,
                CodeOfEntry = users.FirstOrDefault(u =>
                u.TelegramId == callback.From.Id)?.CodeOfEntry,
                Date = DateTime.Today.ToString("d"),
                AnswerToTheQuestionId = Convert.ToInt32(answerId)
            };
            await telegramBotRepos.AddToUserSurveyDataAsync(userSurveyData).ConfigureAwait(false);
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
            var dataFromKnowlegeBase = await telegramBotRepos.GetDataFromKnowledgeBaseByBranchIdAsync(branchId);
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
                            Recommendation = value.Recommendation,
                            Persent = machAnswers.Sum(s =>
                            s.WeightOfAnswer) * 100 / totalWeight
                        });
            }
            return processedDataList.Where(p => p.Persent > 50).OrderBy(p => p.Persent).Take(1);
        }
        private async Task MakeEntryAsync(Message message)

        {
            TelegramUser user = new TelegramUser
            {
                TelegaramId = message.From.Id,
                UserState = "Wait",
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
    }
}