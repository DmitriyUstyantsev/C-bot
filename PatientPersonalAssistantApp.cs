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