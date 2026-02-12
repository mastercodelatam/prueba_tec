using System.Collections.Concurrent;
using BotApi.Models;

namespace BotApi.Services;

public class ConversationStateService
{
    private readonly ConcurrentDictionary<string, ConversationState> _states = new();

    public ConversationState GetOrCreate(string conversationId)
    {
        return _states.GetOrAdd(conversationId, id => new ConversationState
        {
            ConversationId = id
        });
    }

    public void Update(ConversationState state)
    {
        _states[state.ConversationId] = state;
    }

    public void Reset(string conversationId)
    {
        if (_states.TryGetValue(conversationId, out var state))
        {
            state.Reset();
        }
    }
}
