using Telegram.Bot.Types.ReplyMarkups;
using TGSaveUtilityBillsBot.Constants;
using TGSaveUtilityBillsBot.Interfaces;
using TGSaveUtilityBillsBot.Models;

namespace TGSaveUtilityBillsBot.Services;

public class KeyboardFactory : IKeyboardFactory
{
    public InlineKeyboardMarkup CreateYearKeyboard()
    {
        var currentYear = DateTime.Now.Year;
        var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    $"📅 {currentYear - 1} (предыдущий)",
                    $"{CallbackDataPrefixes.Year}{currentYear - 1}"
                )
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    $"📅 {currentYear} (текущий)",
                    $"{CallbackDataPrefixes.Year}{currentYear}"
                )
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    $"📅 {currentYear + 1} (следующий)",
                    $"{CallbackDataPrefixes.Year}{currentYear + 1}"
                )
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    "✍️ Ввести вручную",
                    CallbackDataPrefixes.ManualYear
                )
            }
        };

        return new InlineKeyboardMarkup(buttons);
    }

    public InlineKeyboardMarkup CreateMonthKeyboard()
    {
        var buttons = new List<List<InlineKeyboardButton>>();
        var months = Enum.GetValues<Month>();

        // Группируем по 3 кнопки в ряд
        for (int i = 0; i < months.Length; i += 3)
        {
            var row = new List<InlineKeyboardButton>();
            for (int j = i; j < Math.Min(i + 3, months.Length); j++)
            {
                var month = months[j];
                row.Add(InlineKeyboardButton.WithCallbackData(
                    month.ToString(),
                    $"{CallbackDataPrefixes.Month}{(int)month}"
                ));
            }
            buttons.Add(row);
        }

        return new InlineKeyboardMarkup(buttons);
    }

    public InlineKeyboardMarkup CreateCompanyKeyboard()
    {
        var buttons = Enum.GetValues<Company>()
            .Select(company => new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(
                    company.ToString().Replace("_", " "),
                    $"{CallbackDataPrefixes.Company}{company}"
                )
            })
            .ToList();

        return new InlineKeyboardMarkup(buttons);
    }

    public InlineKeyboardMarkup CreateDocumentTypeKeyboard()
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        foreach (var docType in Enum.GetValues<DocumentType>())
        {
            var emoji = docType == DocumentType.Квитанция ? "🧾" : "🧾";
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{emoji} {docType}",
                    $"{CallbackDataPrefixes.DocumentType}{docType}"
                )
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }

    public InlineKeyboardMarkup CreateCancelKeyboard()
    {
        var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("❌ Отменить загрузку", CallbackDataPrefixes.Cancel + "upload")
            }
        };

        return new InlineKeyboardMarkup(buttons);
    }

    public InlineKeyboardMarkup CreateOverwriteConfirmationKeyboard()
    {
        var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("🔄 Перезаписать", CallbackDataPrefixes.Overwrite + "yes")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("❌ Отмена", CallbackDataPrefixes.Cancel + "overwrite")
            }
        };

        return new InlineKeyboardMarkup(buttons);
    }
}

