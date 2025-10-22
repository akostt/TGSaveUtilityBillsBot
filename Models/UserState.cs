namespace TGSaveUtilityBillsBot.Models;

public enum UserStateEnum
{
    None,
    WaitingForYear,
    WaitingForMonth,
    WaitingForCompany,
    WaitingForFile
}

public class UserState
{
    public UserStateEnum State { get; set; } = UserStateEnum.None;
    public BillMetadata? CurrentBill { get; set; }
}

