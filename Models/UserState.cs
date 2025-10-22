namespace TGSaveUtilityBillsBot.Models;

public enum UserStateEnum
{
    None,
    WaitingForYear,
    WaitingForManualYear,
    WaitingForMonth,
    WaitingForCompany,
    WaitingForDocumentType,
    WaitingForFile,
    WaitingForOverwriteConfirmation
}

public class UserState
{
    public UserStateEnum State { get; set; } = UserStateEnum.None;
    public BillMetadata? CurrentBill { get; set; }
    public string? PendingFilePath { get; set; }
    public byte[]? PendingFileData { get; set; }
}

