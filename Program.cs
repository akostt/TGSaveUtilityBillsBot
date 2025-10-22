using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TGSaveUtilityBillsBot.Configuration;
using TGSaveUtilityBillsBot.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Настройка конфигурации
builder.Configuration.AddTelegramBotConfiguration();

// Настройка логирования
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

// Регистрация всех сервисов
builder.Services.AddTelegramBot(builder.Configuration);

var host = builder.Build();

// Получаем и выводим информацию о конфигурации
var botOptions = host.Services.GetRequiredService<IOptions<TelegramBotOptions>>().Value;
var allowedUserIds = botOptions.GetAllowedUserIds();

if (allowedUserIds.Count > 0)
{
    Console.WriteLine($"✅ Белый список активен: {allowedUserIds.Count} пользователей");
}
else
{
    Console.WriteLine("⚠️  Белый список пуст - бот доступен всем пользователям");
}

try
{
    await host.RunAsync();
}
catch (OptionsValidationException ex)
{
    Console.WriteLine("❌ Ошибка конфигурации:");
    foreach (var failure in ex.Failures)
    {
        Console.WriteLine($"  - {failure}");
    }
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Критическая ошибка при запуске бота");
    Console.WriteLine($"❌ Критическая ошибка: {ex.Message}");
}
