using Godot;

/// <summary>
/// Manages the global chat UI and message handling.
/// This should be set up as an Autoload singleton.
/// </summary>
public partial class ChatManager : CanvasLayer
{
    private RichTextLabel _messageLog;
    private LineEdit _messageInput;

    public override void _Ready()
    {
        _messageLog = GetNode<RichTextLabel>("MessageLog");
        _messageInput = GetNode<LineEdit>("MessageInput");

        // When the user presses Enter in the input field, send the message.
        _messageInput.TextSubmitted += OnTextSubmitted;
    }

    /// <summary>
    /// Called when the user submits a message in the chat input.
    /// </summary>
    private void OnTextSubmitted(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // Clear the input field.
        _messageInput.Clear();

        // Send the message to the server for broadcasting.
        Rpc(nameof(Server_BroadcastMessage), text);
    }

    /// <summary>
    /// An RPC method that runs on the server to broadcast a message to all clients.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void Server_BroadcastMessage(string message)
    {
        long senderId = Multiplayer.GetRemoteSenderId();
        // We need to get the sender's name from the NetworkPlayerManager.
        var playerManager = GetNode<NetworkPlayerManager>("/root/GameManager/NetworkPlayerManager");
        string senderName = playerManager.GetPlayerName(senderId);

        string formattedMessage = $"[color=cyan]{senderName}[/color]: {message}";

        // Now, call the client-side RPC to display the message.
        Rpc(nameof(Client_ReceiveMessage), formattedMessage);
    }

    /// <summary>
    /// An RPC method that runs on all clients to display a received message.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Client_ReceiveMessage(string message)
    {
        _messageLog.AppendText(message + "\n");
    }

    /// <summary>
    /// Displays a system message (e.g., player join/leave) on all clients.
    /// This should only be called on the server.
    /// </summary>
    public void BroadcastSystemMessage(string message)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        // Use yellow for system messages.
        string formattedMessage = $"[color=yellow]{message}[/color]";
        Rpc(nameof(Client_ReceiveMessage), formattedMessage);
    }
}