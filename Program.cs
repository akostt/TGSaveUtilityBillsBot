using Microsoft.Extensions.Configuration;
using TGSaveUtilityBillsBot;
using TGSaveUtilityBillsBot.Services;

// Загружаем конфигурацию
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Получаем настройки
var botToken = configuration["TelegramBot:Token"];
var yandexToken = configuration["YandexDisk:Token"];
var rootFolder = configuration["YandexDisk:RootFolder"];

if (string.IsNullOrEmpty(botToken) || botToken == "YOUR_TELEGRAM_BOT_TOKEN_HERE")
{
    Console.WriteLine("❌ Ошибка: Токен Telegram бота не настроен!");
    Console.WriteLine("Отредактируйте appsettings.json и укажите ваш токен бота.");
    return;
}

if (string.IsNullOrEmpty(yandexToken) || yandexToken == "YOUR_YANDEX_DISK_TOKEN_HERE")
{
    Console.WriteLine("❌ Ошибка: Токен Яндекс.Диска не настроен!");
    Console.WriteLine("Отредактируйте appsettings.json и укажите ваш OAuth токен Яндекс.Диска.");
    return;
}

try
{
    // Создаем сервисы
    var yandexDiskService = new YandexDiskService(yandexToken!, rootFolder!);
    var bot = new TelegramBot(botToken!, yandexDiskService);

    // Обработка Ctrl+C
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        bot.Stop();
    };

    // Запускаем бота
    await bot.StartAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Критическая ошибка: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

