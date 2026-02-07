using Godot;

/// <summary>
/// A simple component to listen for AI bark events and print them to the in-game chat log.
/// You could expand this to use a fancier UI with temporary popups on the speaking character.
/// </summary>
public partial class BattleChatController : Node
{
    public override void _Ready()
    {
        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        this.Subscribe(
            () => eventBus.AIShouted += OnAIShouted,
            () => eventBus.AIShouted -= OnAIShouted
        );
    }

    private void OnAIShouted(string message)
    {
        // Get the ChatManager (Autoload)
        var chatManager = GetNode<ChatManager>("/root/ChatOverlay");

        // If not in multiplayer, output to Debug Console (as well as Chat).
        // If in multiplayer, the ChatManager will handle broadcasting the message to all clients.
        if (chatManager != null)
        {
            chatManager.BroadcastSystemMessage($"AI: {message}");
        }
        else GD.Print($"AI: {message}");
    }
}