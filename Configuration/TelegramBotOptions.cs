using System.ComponentModel.DataAnnotations;

namespace TGSaveUtilityBillsBot.Configuration;

public class TelegramBotOptions
{
    public const string SectionName = "TelegramBot";

    [Required(ErrorMessage = "Токен Telegram бота обязателен")]
    [MinLength(10, ErrorMessage = "Токен слишком короткий")]
    public string Token { get; set; } = string.Empty;

    public string AllowedUserIds { get; set; } = string.Empty;

    public HashSet<long> GetAllowedUserIds()
    {
        var userIds = new HashSet<long>();
        
        if (string.IsNullOrWhiteSpace(AllowedUserIds))
        {
            return userIds;
        }

        var userIdStrings = AllowedUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var userIdStr in userIdStrings)
        {
            if (long.TryParse(userIdStr.Trim(), out var userId))
            {
                userIds.Add(userId);
            }
        }

        return userIds;
    }
}



