using Microsoft.Extensions.Hosting;
using TGSaveUtilityBillsBot.Handlers;

namespace TGSaveUtilityBillsBot.Services;

public class TelegramBotHostedService : IHostedService
{
    private readonly TelegramBot _bot;

    public TelegramBotHostedService(TelegramBot bot)
    {
        _bot = bot;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Запускаем бота в фоновом режиме
        _ = _bot.StartAsync();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _bot.Stop();
        return Task.CompletedTask;
    }
}



