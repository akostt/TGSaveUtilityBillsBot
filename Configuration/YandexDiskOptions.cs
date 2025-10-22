using System.ComponentModel.DataAnnotations;

namespace TGSaveUtilityBillsBot.Configuration;

public class YandexDiskOptions
{
    public const string SectionName = "YandexDisk";

    [Required(ErrorMessage = "Токен Яндекс.Диска обязателен")]
    [MinLength(10, ErrorMessage = "Токен слишком короткий")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Корневая папка обязательна")]
    public string RootFolder { get; set; } = "Квитанции";
}



