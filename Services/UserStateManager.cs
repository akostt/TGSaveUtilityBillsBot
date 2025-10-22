using System.Collections.Concurrent;
using TGSaveUtilityBillsBot.Interfaces;
using TGSaveUtilityBillsBot.Models;

namespace TGSaveUtilityBillsBot.Services;

public class UserStateManager : IUserStateManager
{
    private readonly ConcurrentDictionary<long, UserState> _userStates = new();

    public UserState? GetUserState(long chatId)
    {
        return _userStates.TryGetValue(chatId, out var state) ? state : null;
    }

    public void SetUserState(long chatId, UserState state)
    {
        _userStates[chatId] = state;
    }

    public void RemoveUserState(long chatId)
    {
        _userStates.TryRemove(chatId, out _);
    }

    public bool TryGetUserState(long chatId, out UserState? state)
    {
        return _userStates.TryGetValue(chatId, out state);
    }
}



