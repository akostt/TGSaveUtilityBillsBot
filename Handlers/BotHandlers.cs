using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TGSaveUtilityBillsBot.Configuration;
using TGSaveUtilityBillsBot.Constants;
using TGSaveUtilityBillsBot.Interfaces;
using TGSaveUtilityBillsBot.Models;

namespace TGSaveUtilityBillsBot.Handlers;

public class BotHandlers
{
    private readonly IYandexDiskService _yandexDiskService;
    private readonly IUserStateManager _stateManager;
    private readonly IKeyboardFactory _keyboardFactory;
    private readonly ILogger<BotHandlers> _logger;
    private readonly HashSet<long> _allowedUserIds;

    public BotHandlers(
        IYandexDiskService yandexDiskService,
        IUserStateManager stateManager,
        IKeyboardFactory keyboardFactory,
        ILogger<BotHandlers> logger,
        IOptions<TelegramBotOptions> botOptions)
    {
        _yandexDiskService = yandexDiskService;
        _stateManager = stateManager;
        _keyboardFactory = keyboardFactory;
        _logger = logger;
        _allowedUserIds = botOptions.Value.GetAllowedUserIds();
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            // Проверяем доступ пользователя
            long userId = update.Type switch
            {
                UpdateType.Message when update.Message != null => update.Message.From!.Id,
                UpdateType.CallbackQuery when update.CallbackQuery != null => update.CallbackQuery.From.Id,
                _ => 0
            };

            // Если белый список не пустой и пользователь не в списке - отказываем в доступе
            if (_allowedUserIds.Count > 0 && !_allowedUserIds.Contains(userId))
            {
                if (update.Message != null)
                {
                    await botClient.SendTextMessageAsync(
                        update.Message.Chat.Id,
                        BotMessages.AccessDenied,
                        cancellationToken: cancellationToken
                    );
                }
                return;
            }

            var handler = update.Type switch
            {
                UpdateType.Message when update.Message != null => HandleMessageAsync(botClient, update.Message, cancellationToken),
                UpdateType.CallbackQuery when update.CallbackQuery != null => HandleCallbackQueryAsync(botClient, update.CallbackQuery, cancellationToken),
                _ => Task.CompletedTask
            };

            await handler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке обновления");
        }
    }

    private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var handler = message.Type switch
        {
            MessageType.Text when message.Text?.StartsWith("/") == true => 
                HandleCommandAsync(botClient, message, cancellationToken),
            MessageType.Text => 
                HandleTextMessageAsync(botClient, message, cancellationToken),
            MessageType.Document when message.Document != null => 
                HandleDocumentAsync(botClient, message, cancellationToken),
            _ => Task.CompletedTask
        };

        await handler;
    }

    private async Task HandleCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var command = message.Text!.ToLower();

        var handler = command switch
        {
            BotCommands.Start => SendWelcomeMessageAsync(botClient, chatId, cancellationToken),
            BotCommands.Upload => StartUploadProcessAsync(botClient, chatId, cancellationToken),
            BotCommands.Cancel => CancelOperationAsync(botClient, chatId, cancellationToken),
            BotCommands.Help => SendHelpMessageAsync(botClient, chatId, cancellationToken),
            _ => SendUnknownCommandMessageAsync(botClient, chatId, cancellationToken)
        };

        await handler;
    }

    private Task SendWelcomeMessageAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Пользователь {ChatId} запустил бота", chatId);
        return botClient.SendTextMessageAsync(chatId, BotMessages.Welcome, cancellationToken: cancellationToken);
    }

    private Task SendHelpMessageAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        return botClient.SendTextMessageAsync(chatId, BotMessages.Help, cancellationToken: cancellationToken);
    }

    private Task SendUnknownCommandMessageAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        return botClient.SendTextMessageAsync(chatId, BotMessages.InvalidCommand, cancellationToken: cancellationToken);
    }

    private async Task StartUploadProcessAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var userState = new UserState
        {
            State = UserStateEnum.WaitingForYear,
            CurrentBill = new BillMetadata()
        };

        _stateManager.SetUserState(chatId, userState);
        _logger.LogInformation("Пользователь {ChatId} начал процесс загрузки", chatId);

        var keyboard = _keyboardFactory.CreateYearKeyboard();
        await botClient.SendTextMessageAsync(
            chatId, 
            BotMessages.AskForYear, 
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task CancelOperationAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        _stateManager.RemoveUserState(chatId);
        _logger.LogInformation("Пользователь {ChatId} отменил операцию", chatId);

        await botClient.SendTextMessageAsync(chatId, BotMessages.OperationCancelled, cancellationToken: cancellationToken);
    }

    private async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        if (!_stateManager.TryGetUserState(chatId, out var userState) || userState == null)
        {
            await botClient.SendTextMessageAsync(chatId, BotMessages.UseUploadCommand, cancellationToken: cancellationToken);
            return;
        }

        if (userState.State == UserStateEnum.WaitingForManualYear)
        {
            await HandleYearInputAsync(botClient, message, userState, cancellationToken);
        }
    }

    private async Task HandleYearInputAsync(ITelegramBotClient botClient, Message message, UserState userState, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        if (!int.TryParse(message.Text, out var year) || 
            year < ValidationRules.MinYear || 
            year > ValidationRules.MaxYear)
        {
            await botClient.SendTextMessageAsync(chatId, BotMessages.InvalidYear, cancellationToken: cancellationToken);
            return;
        }

        userState.CurrentBill!.Year = year;
        userState.State = UserStateEnum.WaitingForMonth;

        var keyboard = _keyboardFactory.CreateMonthKeyboard();
        await botClient.SendTextMessageAsync(
            chatId,
            BotMessages.AskForMonth,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data!;

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

        if (!_stateManager.TryGetUserState(chatId, out var userState) || userState == null)
        {
            return;
        }

        if (data.StartsWith(CallbackDataPrefixes.Year))
        {
            await HandleYearSelectionAsync(botClient, callbackQuery, userState, data, cancellationToken);
        }
        else if (data == CallbackDataPrefixes.ManualYear)
        {
            await HandleManualYearRequestAsync(botClient, callbackQuery, userState, cancellationToken);
        }
        else if (data.StartsWith(CallbackDataPrefixes.Month))
        {
            await HandleMonthSelectionAsync(botClient, callbackQuery, userState, data, cancellationToken);
        }
        else if (data.StartsWith(CallbackDataPrefixes.Company))
        {
            await HandleCompanySelectionAsync(botClient, callbackQuery, userState, data, cancellationToken);
        }
        else if (data.StartsWith(CallbackDataPrefixes.DocumentType))
        {
            await HandleDocumentTypeSelectionAsync(botClient, callbackQuery, userState, data, cancellationToken);
        }
        else if (data.StartsWith(CallbackDataPrefixes.Overwrite))
        {
            await HandleOverwriteAsync(botClient, callbackQuery, userState, cancellationToken);
        }
        else if (data.StartsWith(CallbackDataPrefixes.Cancel))
        {
            await HandleCancelAsync(botClient, callbackQuery, userState, cancellationToken);
        }
    }

    private async Task HandleYearSelectionAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        UserState userState,
        string callbackData,
        CancellationToken cancellationToken)
    {
        var year = int.Parse(callbackData.Replace(CallbackDataPrefixes.Year, ""));
        userState.CurrentBill!.Year = year;
        userState.State = UserStateEnum.WaitingForMonth;

        var keyboard = _keyboardFactory.CreateMonthKeyboard();
        await botClient.EditMessageTextAsync(
            callbackQuery.Message!.Chat.Id,
            callbackQuery.Message.MessageId,
            BotMessages.AskForMonth,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleManualYearRequestAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        UserState userState,
        CancellationToken cancellationToken)
    {
        userState.State = UserStateEnum.WaitingForManualYear;

        await botClient.EditMessageTextAsync(
            callbackQuery.Message!.Chat.Id,
            callbackQuery.Message.MessageId,
            BotMessages.AskForManualYear,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleMonthSelectionAsync(
        ITelegramBotClient botClient, 
        CallbackQuery callbackQuery, 
        UserState userState, 
        string callbackData, 
        CancellationToken cancellationToken)
    {
        var monthValue = int.Parse(callbackData.Replace(CallbackDataPrefixes.Month, ""));
        userState.CurrentBill!.Month = (Month)monthValue;
        userState.State = UserStateEnum.WaitingForCompany;

        var keyboard = _keyboardFactory.CreateCompanyKeyboard();
        var message = BotMessages.MonthSelected(userState.CurrentBill.Month.ToString());

        await botClient.EditMessageTextAsync(
            callbackQuery.Message!.Chat.Id,
            callbackQuery.Message.MessageId,
            message,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleCompanySelectionAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        UserState userState,
        string callbackData,
        CancellationToken cancellationToken)
    {
        var companyName = callbackData.Replace(CallbackDataPrefixes.Company, "");
        userState.CurrentBill!.Company = Enum.Parse<Company>(companyName);
        userState.State = UserStateEnum.WaitingForDocumentType;

        var keyboard = _keyboardFactory.CreateDocumentTypeKeyboard();
        var message = BotMessages.CompanySelected(
            userState.CurrentBill.Company.ToString().Replace("_", " ")
        );

        await botClient.EditMessageTextAsync(
            callbackQuery.Message!.Chat.Id,
            callbackQuery.Message.MessageId,
            message,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleDocumentTypeSelectionAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        UserState userState,
        string callbackData,
        CancellationToken cancellationToken)
    {
        var documentTypeName = callbackData.Replace(CallbackDataPrefixes.DocumentType, "");
        userState.CurrentBill!.DocumentType = Enum.Parse<DocumentType>(documentTypeName);
        userState.State = UserStateEnum.WaitingForFile;

        var message = BotMessages.DocumentTypeSelected(
            userState.CurrentBill.Year,
            userState.CurrentBill.Month.ToString(),
            userState.CurrentBill.Company.ToString().Replace("_", " "),
            userState.CurrentBill.DocumentType.ToString()
        );

        var keyboard = _keyboardFactory.CreateCancelKeyboard();

        await botClient.EditMessageTextAsync(
            callbackQuery.Message!.Chat.Id,
            callbackQuery.Message.MessageId,
            message,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleDocumentAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        if (!_stateManager.TryGetUserState(chatId, out var userState) || 
            userState == null ||
            userState.State != UserStateEnum.WaitingForFile)
        {
            await botClient.SendTextMessageAsync(chatId, BotMessages.UseUploadCommandFirst, cancellationToken: cancellationToken);
            return;
        }

        var document = message.Document!;

        if (!document.FileName!.EndsWith(ValidationRules.PdfExtension, StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendTextMessageAsync(chatId, BotMessages.NotPdfFile, cancellationToken: cancellationToken);
            return;
        }

        await ProcessFileUploadAsync(botClient, chatId, document, userState, cancellationToken);
    }

    private async Task ProcessFileUploadAsync(
        ITelegramBotClient botClient,
        long chatId,
        Document document,
        UserState userState,
        CancellationToken cancellationToken)
    {
        Message processingMessage = null!;

        try
        {
            processingMessage = await botClient.SendTextMessageAsync(chatId, BotMessages.Uploading, cancellationToken: cancellationToken);

            _logger.LogInformation("Загрузка файла {FileName} для пользователя {ChatId}", document.FileName, chatId);

            var fileInfo = await botClient.GetFileAsync(document.FileId, cancellationToken);

            using var memoryStream = new MemoryStream();
            await botClient.DownloadFileAsync(fileInfo.FilePath!, memoryStream, cancellationToken);
            memoryStream.Position = 0;

            // Генерируем имя файла на основе типа документа
            var fileName = userState.CurrentBill!.DocumentType == DocumentType.Квитанция 
                ? "Квитанция.pdf" 
                : "Чек.pdf";

            // Проверяем существование файла
            var fileExists = await _yandexDiskService.FileExistsAsync(userState.CurrentBill!, fileName);

            if (fileExists)
            {
                // Сохраняем данные файла для последующей загрузки
                userState.PendingFileData = memoryStream.ToArray();
                userState.PendingFilePath = fileName;
                userState.State = UserStateEnum.WaitingForOverwriteConfirmation;

                var keyboard = _keyboardFactory.CreateOverwriteConfirmationKeyboard();
                await botClient.EditMessageTextAsync(
                    chatId,
                    processingMessage.MessageId,
                    BotMessages.FileExists(fileName),
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
                return;
            }

            var success = await _yandexDiskService.UploadFileAsync(
                userState.CurrentBill!,
                memoryStream,
                fileName,
                overwrite: false
            );

            var responseMessage = success
                ? BotMessages.UploadSuccess(
                    userState.CurrentBill.Year,
                    userState.CurrentBill.Month.ToString(),
                    userState.CurrentBill.Company.ToString().Replace("_", " "),
                    fileName)
                : BotMessages.UploadError;

            await botClient.EditMessageTextAsync(chatId, processingMessage.MessageId, responseMessage, cancellationToken: cancellationToken);

            if (success)
            {
                _logger.LogInformation("Файл {FileName} успешно загружен для пользователя {ChatId}", document.FileName, chatId);
            }
            else
            {
                _logger.LogWarning("Не удалось загрузить файл {FileName} для пользователя {ChatId}", document.FileName, chatId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке файла для пользователя {ChatId}", chatId);

            if (processingMessage != null)
            {
                await botClient.EditMessageTextAsync(chatId, processingMessage.MessageId, BotMessages.Error(ex.Message), cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, BotMessages.Error(ex.Message), cancellationToken: cancellationToken);
            }
            
            // При ошибке удаляем состояние
            _stateManager.RemoveUserState(chatId);
        }
        finally
        {
            // Удаляем состояние только если это не ожидание подтверждения перезаписи
            if (!_stateManager.TryGetUserState(chatId, out var state) || 
                state == null || 
                state.State != UserStateEnum.WaitingForOverwriteConfirmation)
            {
                _stateManager.RemoveUserState(chatId);
            }
        }
    }

    private async Task HandleOverwriteAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        UserState userState,
        CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message!.Chat.Id;

        if (userState.PendingFileData == null || userState.PendingFilePath == null)
        {
            await botClient.EditMessageTextAsync(
                chatId,
                callbackQuery.Message.MessageId,
                "❌ Ошибка: данные файла не найдены. Попробуйте загрузить файл снова.",
                cancellationToken: cancellationToken
            );
            _stateManager.RemoveUserState(chatId);
            return;
        }

        var fileName = userState.PendingFilePath;

        try
        {
            // Показываем, что началась перезапись
            await botClient.EditMessageTextAsync(
                chatId,
                callbackQuery.Message.MessageId,
                BotMessages.Overwriting,
                cancellationToken: cancellationToken
            );

            // Небольшая задержка для лучшего UX
            await Task.Delay(300, cancellationToken);

            // Выполняем перезапись
            using var memoryStream = new MemoryStream(userState.PendingFileData);

            var success = await _yandexDiskService.UploadFileAsync(
                userState.CurrentBill!,
                memoryStream,
                fileName,
                overwrite: true
            );
            if (success)
            {
                var responseMessage = $"✅ Файл успешно перезаписан!\n\n" +
                    $"📁 Путь: Квитанции/{userState.CurrentBill.Year}/{userState.CurrentBill.Month}/{userState.CurrentBill.Company}/{fileName}\n\n" +
                    $"Используйте /upload для загрузки следующего документа.";

                await botClient.EditMessageTextAsync(
                    chatId,
                    callbackQuery.Message.MessageId,
                    responseMessage,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await botClient.EditMessageTextAsync(
                    chatId,
                    callbackQuery.Message.MessageId,
                    BotMessages.UploadError,
                    cancellationToken: cancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при перезаписи файла для пользователя {ChatId}", chatId);
            await botClient.EditMessageTextAsync(
                chatId,
                callbackQuery.Message.MessageId,
                BotMessages.Error(ex.Message),
                cancellationToken: cancellationToken
            );
        }
        finally
        {
            _stateManager.RemoveUserState(chatId);
        }
    }

    private async Task HandleCancelAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        UserState userState,
        CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message!.Chat.Id;

        await botClient.EditMessageTextAsync(
            chatId,
            callbackQuery.Message.MessageId,
            BotMessages.OperationCancelled + "\n\nИспользуйте /upload для новой загрузки.",
            cancellationToken: cancellationToken
        );

        _stateManager.RemoveUserState(chatId);
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Ошибка Telegram API");
        return Task.CompletedTask;
    }
}
