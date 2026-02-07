/// <summary>
/// An implementation of IMessageSink that broadcasts messages to the in-game
/// chat overlay via the ChatManager. This should only be used on the server,
/// as it relies on server-only functionality.
/// </summary>
public class ChatLogSink : IMessageSink
{
    private readonly ChatManager _chatManager;

    public ChatLogSink(ChatManager chatManager)
    {
        _chatManager = chatManager;
    }

    public void LogInfo(string message)
    {
        _chatManager.BroadcastSystemMessage(message);
    }

    public void LogWarning(string message)
    {
        _chatManager.BroadcastSystemMessage($"[WARN] {message}");
    }

    public void LogError(string message)
    {
        _chatManager.BroadcastSystemMessage($"[ERROR] {message}");
    }
}