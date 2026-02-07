using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// This component is responsible for sending local input to the server.
/// It should only be active on the client that owns the associated player character.
/// It reads from a LocalInputProvider and sends the data to the server via RPC.
/// </summary>
public partial class InputSender : Node
{
    [Export]
    private LocalInputProvider _localInputProvider;

    [Export]
    private NetworkInputProvider _networkInputProvider;

    private PlayerController _playerController;

    public override void _Ready()
    {
        // Attempt to find the sibling PlayerController
        _playerController = GetParent().GetNodeOrNull<PlayerController>(PlayerController.DefaultName);
    }

    public override void _PhysicsProcess(double delta)
    {
        // This component only acts for the player who has authority over the parent character.
        if (!GetParent<Node>().IsMultiplayerAuthority()) return;

        // If movement is disabled (e.g. targeting or menu open), send a neutral state
        // so the server stops moving the character.
        if (_playerController != null && !_playerController.IsMovementEnabled)
        {
            Rpc(nameof(Server_ReceiveInput), Vector2.Zero, new Godot.Collections.Dictionary(), new Godot.Collections.Dictionary());
            return;
        }

        var pressed = new Godot.Collections.Dictionary();
        var justPressed = new Godot.Collections.Dictionary();

        // Populate the dictionaries with the current input state.
        foreach (GameInputAction action in Enum.GetValues(typeof(GameInputAction)))
        {
            if (_localInputProvider.IsActionPressed(action))
            {
                // Cast the enum to long, as Godot.Dictionary keys must be Variants.
                pressed[(long)action] = true;
            }
            if (_localInputProvider.IsActionJustPressed(action))
            {
                // Cast the enum to long, as Godot.Dictionary keys must be Variants.
                justPressed[(long)action] = true;
            }
        }

        Vector2 moveVector = _localInputProvider.GetMoveVector();

        // Send the collected input to the server via an RPC.
        Rpc(nameof(Server_ReceiveInput), moveVector, pressed, justPressed);
    }

    /// <summary>
    /// This RPC method is called by a client and executes on the server's instance of this InputSender.
    /// It then forwards the data to the NetworkInputProvider on the server.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void Server_ReceiveInput(Vector2 moveVector, Godot.Collections.Dictionary pressed, Godot.Collections.Dictionary justPressed)
    {
        // This guard ensures the logic only runs on the server, preventing client-side errors.
        if (!Multiplayer.IsServer()) return;

        // We can safely call the method on the server's NetworkInputProvider.
        _networkInputProvider.UpdateNetworkState(moveVector, pressed, justPressed);
    }
}