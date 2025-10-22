using Telegram.Bot.Types.ReplyMarkups;

namespace TGSaveUtilityBillsBot.Interfaces;

public interface IKeyboardFactory
{
    InlineKeyboardMarkup CreateYearKeyboard();
    InlineKeyboardMarkup CreateMonthKeyboard();
    InlineKeyboardMarkup CreateCompanyKeyboard();
    InlineKeyboardMarkup CreateDocumentTypeKeyboard();
    InlineKeyboardMarkup CreateCancelKeyboard();
    InlineKeyboardMarkup CreateOverwriteConfirmationKeyboard();
}

