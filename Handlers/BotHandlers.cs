using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TGSaveUtilityBillsBot.Models;
using TGSaveUtilityBillsBot.Services;

namespace TGSaveUtilityBillsBot.Handlers;

public class BotHandlers
{
    private readonly Dictionary<long, UserState> _userStates = new();
    private readonly YandexDiskService _yandexDiskService;

    public BotHandlers(YandexDiskService yandexDiskService)
    {
        _yandexDiskService = yandexDiskService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Type == UpdateType.Message && update.Message != null)
            {
                await HandleMessageAsync(botClient, update.Message, cancellationToken);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await HandleCallbackQueryAsync(botClient, update.CallbackQuery, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке обновления: {ex.Message}");
        }
    }

    private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        if (message.Type == MessageType.Text && message.Text != null)
        {
            if (message.Text.StartsWith("/"))
            {
                await HandleCommandAsync(botClient, message, cancellationToken);
            }
            else
            {
                await HandleTextMessageAsync(botClient, message, cancellationToken);
            }
        }
        else if (message.Type == MessageType.Document && message.Document != null)
        {
            await HandleDocumentAsync(botClient, message, cancellationToken);
        }
    }

    private async Task HandleCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var command = message.Text!.ToLower();

        switch (command)
        {
            case "/start":
                await botClient.SendTextMessageAsync(
                    chatId,
                    "👋 Добро пожаловать в бот для сохранения квитанций!\n\n" +
                    "Я помогу вам организовать все ваши коммунальные квитанции на Яндекс.Диске.\n\n" +
                    "Используйте команду /upload для загрузки квитанции или чека.",
                    cancellationToken: cancellationToken
                );
                break;

            case "/upload":
                _userStates[chatId] = new UserState
                {
                    State = UserStateEnum.WaitingForYear,
                    CurrentBill = new BillMetadata()
                };

                await botClient.SendTextMessageAsync(
                    chatId,
                    "📅 Укажите год квитанции (например, 2024):",
                    cancellationToken: cancellationToken
                );
                break;

            case "/cancel":
                if (_userStates.ContainsKey(chatId))
                {
                    _userStates.Remove(chatId);
                }

                await botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Операция отменена.",
                    cancellationToken: cancellationToken
                );
                break;

            case "/help":
                await botClient.SendTextMessageAsync(
                    chatId,
                    "📖 Доступные команды:\n\n" +
                    "/start - Начать работу с ботом\n" +
                    "/upload - Загрузить новую квитанцию\n" +
                    "/cancel - Отменить текущую операцию\n" +
                    "/help - Показать эту справку",
                    cancellationToken: cancellationToken
                );
                break;

            default:
                await botClient.SendTextMessageAsync(
                    chatId,
                    "❓ Неизвестная команда. Используйте /help для просмотра доступных команд.",
                    cancellationToken: cancellationToken
                );
                break;
        }
    }

    private async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        if (!_userStates.TryGetValue(chatId, out var userState))
        {
            await botClient.SendTextMessageAsync(
                chatId,
                "Используйте команду /upload для загрузки квитанции.",
                cancellationToken: cancellationToken
            );
            return;
        }

        switch (userState.State)
        {
            case UserStateEnum.WaitingForYear:
                if (int.TryParse(message.Text, out var year) && year >= 2000 && year <= 2100)
                {
                    userState.CurrentBill!.Year = year;
                    userState.State = UserStateEnum.WaitingForMonth;

                    var monthKeyboard = CreateMonthKeyboard();
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "📆 Выберите месяц:",
                        replyMarkup: monthKeyboard,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Некорректный год. Пожалуйста, введите год (например, 2024):",
                        cancellationToken: cancellationToken
                    );
                }
                break;
        }
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data!;

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

        if (!_userStates.TryGetValue(chatId, out var userState))
        {
            return;
        }

        if (data.StartsWith("month_"))
        {
            var monthValue = int.Parse(data.Replace("month_", ""));
            userState.CurrentBill!.Month = (Month)monthValue;
            userState.State = UserStateEnum.WaitingForCompany;

            var companyKeyboard = CreateCompanyKeyboard();
            await botClient.EditMessageTextAsync(
                chatId,
                callbackQuery.Message.MessageId,
                $"✅ Выбран месяц: {userState.CurrentBill.Month}\n\n🏢 Выберите компанию:",
                replyMarkup: companyKeyboard,
                cancellationToken: cancellationToken
            );
        }
        else if (data.StartsWith("company_"))
        {
            var companyName = data.Replace("company_", "");
            userState.CurrentBill!.Company = Enum.Parse<Company>(companyName);
            userState.State = UserStateEnum.WaitingForFile;

            await botClient.EditMessageTextAsync(
                chatId,
                callbackQuery.Message.MessageId,
                $"✅ Год: {userState.CurrentBill.Year}\n" +
                $"✅ Месяц: {userState.CurrentBill.Month}\n" +
                $"✅ Компания: {userState.CurrentBill.Company}\n\n" +
                $"📎 Теперь отправьте квитанцию или чек в формате PDF.",
                cancellationToken: cancellationToken
            );
        }
    }

    private async Task HandleDocumentAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        if (!_userStates.TryGetValue(chatId, out var userState) || 
            userState.State != UserStateEnum.WaitingForFile)
        {
            await botClient.SendTextMessageAsync(
                chatId,
                "Сначала используйте команду /upload для начала процесса загрузки.",
                cancellationToken: cancellationToken
            );
            return;
        }

        var document = message.Document!;
        
        // Проверяем, что это PDF файл
        if (!document.FileName!.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendTextMessageAsync(
                chatId,
                "❌ Пожалуйста, отправьте файл в формате PDF.",
                cancellationToken: cancellationToken
            );
            return;
        }

        try
        {
            var processingMessage = await botClient.SendTextMessageAsync(
                chatId,
                "⏳ Загружаю файл на Яндекс.Диск...",
                cancellationToken: cancellationToken
            );

            // Скачиваем файл от Telegram
            var fileInfo = await botClient.GetFileAsync(document.FileId, cancellationToken);
            
            using var memoryStream = new MemoryStream();
            await botClient.DownloadFileAsync(fileInfo.FilePath!, memoryStream, cancellationToken);
            memoryStream.Position = 0;

            // Загружаем на Яндекс.Диск
            var success = await _yandexDiskService.UploadFileAsync(
                userState.CurrentBill!,
                memoryStream,
                document.FileName
            );

            if (success)
            {
                await botClient.EditMessageTextAsync(
                    chatId,
                    processingMessage.MessageId,
                    $"✅ Квитанция успешно сохранена!\n\n" +
                    $"📁 Путь: Квитанции/{userState.CurrentBill.Year}/{userState.CurrentBill.Month}/{userState.CurrentBill.Company}/{document.FileName}\n\n" +
                    $"Используйте /upload для загрузки следующей квитанции.",
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await botClient.EditMessageTextAsync(
                    chatId,
                    processingMessage.MessageId,
                    "❌ Ошибка при загрузке файла на Яндекс.Диск. Проверьте токен доступа и попробуйте снова.",
                    cancellationToken: cancellationToken
                );
            }

            // Очищаем состояние пользователя
            _userStates.Remove(chatId);
        }
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(
                chatId,
                $"❌ Произошла ошибка: {ex.Message}",
                cancellationToken: cancellationToken
            );
            _userStates.Remove(chatId);
        }
    }

    private InlineKeyboardMarkup CreateMonthKeyboard()
    {
        var buttons = new List<List<InlineKeyboardButton>>();
        var months = Enum.GetValues<Month>();

        for (int i = 0; i < months.Length; i += 3)
        {
            var row = new List<InlineKeyboardButton>();
            for (int j = i; j < Math.Min(i + 3, months.Length); j++)
            {
                var month = months[j];
                row.Add(InlineKeyboardButton.WithCallbackData(
                    month.ToString(),
                    $"month_{(int)month}"
                ));
            }
            buttons.Add(row);
        }

        return new InlineKeyboardMarkup(buttons);
    }

    private InlineKeyboardMarkup CreateCompanyKeyboard()
    {
        var buttons = new List<List<InlineKeyboardButton>>();
        var companies = Enum.GetValues<Company>();

        foreach (var company in companies)
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(
                    company.ToString().Replace("_", " "),
                    $"company_{company}"
                )
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка Telegram API: {exception.Message}");
        return Task.CompletedTask;
    }
}

