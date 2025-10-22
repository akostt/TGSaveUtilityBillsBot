using TGSaveUtilityBillsBot.Models;

namespace TGSaveUtilityBillsBot.Interfaces;

public interface IUserStateManager
{
    UserState? GetUserState(long chatId);
    void SetUserState(long chatId, UserState state);
    void RemoveUserState(long chatId);
    bool TryGetUserState(long chatId, out UserState? state);
}



