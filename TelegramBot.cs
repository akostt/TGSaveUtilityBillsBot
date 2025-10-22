using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TGSaveUtilityBillsBot.Handlers;
using TGSaveUtilityBillsBot.Services;

namespace TGSaveUtilityBillsBot;

public class TelegramBot
{
    private readonly ITelegramBotClient _botClient;
    private readonly BotHandlers _handlers;
    private readonly CancellationTokenSource _cts;

    public TelegramBot(string botToken, YandexDiskService yandexDiskService)
    {
        _botClient = new TelegramBotClient(botToken);
        _handlers = new BotHandlers(yandexDiskService);
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync()
    {
        var me = await _botClient.GetMeAsync();
        Console.WriteLine($"🤖 Бот @{me.Username} запущен!");
        Console.WriteLine("Нажмите Ctrl+C для остановки...\n");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
        };

        _botClient.StartReceiving(
            updateHandler: _handlers.HandleUpdateAsync,
            pollingErrorHandler: _handlers.HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: _cts.Token
        );

        await Task.Delay(Timeout.Infinite, _cts.Token);
    }

    public void Stop()
    {
        _cts.Cancel();
        Console.WriteLine("Бот остановлен.");
    }
}

