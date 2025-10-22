using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TGSaveUtilityBillsBot.Configuration;
using TGSaveUtilityBillsBot.Handlers;

namespace TGSaveUtilityBillsBot;

public class TelegramBot
{
    private readonly ITelegramBotClient _botClient;
    private readonly BotHandlers _handlers;
    private readonly ILogger<TelegramBot> _logger;
    private readonly CancellationTokenSource _cts;

    public TelegramBot(
        IOptions<TelegramBotOptions> options,
        BotHandlers handlers,
        ILogger<TelegramBot> logger)
    {
        ArgumentNullException.ThrowIfNull(handlers, nameof(handlers));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        var botToken = options.Value.Token;
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken, nameof(botToken));

        _botClient = new TelegramBotClient(botToken);
        _handlers = handlers;
        _logger = logger;
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync()
    {
        var me = await _botClient.GetMeAsync(_cts.Token);
        _logger.LogInformation("Бот @{Username} запущен!", me.Username);
        
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
            ThrowPendingUpdates = true
        };

        _botClient.StartReceiving(
            updateHandler: _handlers.HandleUpdateAsync,
            pollingErrorHandler: _handlers.HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: _cts.Token
        );

        try
        {
            await Task.Delay(Timeout.Infinite, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Ожидаемое исключение при остановке
        }
    }

    public void Stop()
    {
        _logger.LogInformation("Остановка бота");
        _cts.Cancel();
    }
}
