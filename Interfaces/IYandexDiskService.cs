using TGSaveUtilityBillsBot.Models;

namespace TGSaveUtilityBillsBot.Interfaces;

public interface IYandexDiskService
{
    Task<bool> UploadFileAsync(BillMetadata metadata, Stream fileStream, string fileName, bool overwrite = false);
    Task<bool> FileExistsAsync(BillMetadata metadata, string fileName);
}

