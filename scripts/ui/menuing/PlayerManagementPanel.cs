using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A host-only UI panel for viewing connected players and assigning them to characters.
/// </summary>
public partial class PlayerManagementPanel : PanelContainer
{
    [Export]
    private ItemList _playerList;
    [Export]
    private ItemList _characterList;
    [Export]
    private Button _assignButton;
    [Export]
    private Button _unassignButton;

    private NetworkPlayerManager _networkPlayerManager;
    private GameManager _gameManager;

    public override void _Ready()
    {
        // Use the exported nodes if they are set, otherwise fall back to GetNode.
        _playerList ??= GetNode<ItemList>("HBoxContainer/VBoxContainer/PlayerList");
        _characterList ??= GetNode<ItemList>("HBoxContainer/VBoxContainer2/CharacterList");
        _assignButton ??= GetNode<Button>("AssignButton");
        _unassignButton ??= GetNode<Button>("UnassignButton");

        // A final check to ensure the nodes are available before proceeding.
        if (_playerList == null || _characterList == null || _assignButton == null || _unassignButton == null)
        {
            GD.PrintErr("PlayerManagementPanel is not configured correctly. Please assign UI nodes in the editor or check scene paths.");
            QueueFree();
            return;
        }

        // As an autoload, it should start hidden.
        Hide();

        // This panel is only for the host.
        if (!Multiplayer.IsServer())
        {
            QueueFree(); // Remove the panel if not the server.
            return;
        }

        _gameManager = GetNode<GameManager>(GameManager.Path);
        _networkPlayerManager = GetNode<NetworkPlayerManager>("/root/GameManager/NetworkPlayerManager");

        // The panel needs to wait for the NetworkPlayerManager to be initialized
        // before it can safely access the character list.
        this.Subscribe(
            () => _networkPlayerManager.Initialized += OnNetworkManagerInitialized,
            () => _networkPlayerManager.Initialized -= OnNetworkManagerInitialized
        );
    }

    public override void _Input(InputEvent @event)
    {
        // Allow the host to toggle the panel's visibility with the Tab key.
        if (Multiplayer.IsServer() && @event.IsActionPressed("toggle_player_panel"))
        {
            Visible = !Visible;

            // When the panel is made visible, force a refresh of the lists.
            if (Visible)
            {
                RefreshPlayerList();
                RefreshCharacterList();
            }
        }
    }

    private void OnNetworkManagerInitialized()
    {
        // Now that the NetworkPlayerManager is ready, we can connect to its signals
        // and do the initial population of the lists.
        this.Subscribe(
            () => _networkPlayerManager.PlayerListChanged += RefreshPlayerList,
            () => _networkPlayerManager.PlayerListChanged -= RefreshPlayerList
        );
        this.Subscribe(
            () => _networkPlayerManager.CharacterAssignmentChanged += RefreshCharacterList,
            () => _networkPlayerManager.CharacterAssignmentChanged -= RefreshCharacterList
        );

        _assignButton.Pressed += OnAssignButtonPressed;
        _unassignButton.Pressed += OnUnassignButtonPressed;

        // Pull the initial state now that we know the manager is ready.
        // This ensures the host is visible immediately.
        RefreshPlayerList();
        RefreshCharacterList();
    }

    private void RefreshPlayerList()
    {
        _playerList.Clear();
        var players = _networkPlayerManager.GetPlayerList();
        foreach (var (peerId, name) in players)
        {
            // Store the peer ID in the item's metadata.
            _playerList.AddItem($"{name} (ID: {peerId})");
            _playerList.SetItemMetadata(_playerList.ItemCount - 1, peerId);
        }
    }

    private void RefreshCharacterList()
    {
        _characterList.Clear();
        var characters = _networkPlayerManager.GetCharacters();
        var assignments = _networkPlayerManager.GetAssignedCharacters();

        for (int i = 0; i < characters.Count; i++)
        {
            var character = characters[i];
            string text = character.Name;

            // Check if this character is assigned.
            if (assignments.ContainsKey(character))
            {
                long assignedPeerId = assignments[character];
                string playerName = _networkPlayerManager.GetPlayerName(assignedPeerId);
                text += $" (Assigned to: {playerName})";
            }

            _characterList.AddItem(text);
            // Store the character's index in the metadata.
            _characterList.SetItemMetadata(_characterList.ItemCount - 1, i);
        }
    }

    private void OnAssignButtonPressed()
    {
        var selectedPlayerIndices = _playerList.GetSelectedItems();
        var selectedCharIndices = _characterList.GetSelectedItems();

        if (selectedPlayerIndices.Length == 0 || selectedCharIndices.Length == 0)
        {
            GD.Print("Please select one player and one character to assign.");
            return;
        }

        // Get the peer ID and character index from the metadata we stored.
        long targetPeerId = _playerList.GetItemMetadata(selectedPlayerIndices[0]).AsInt64();
        int characterIndex = _characterList.GetItemMetadata(selectedCharIndices[0]).AsInt32();

        // Call the GameManager method to perform the assignment.
        _gameManager.AssignCharacterToPlayer(targetPeerId, characterIndex);
    }

    private void OnUnassignButtonPressed()
    {
        var selectedCharIndices = _characterList.GetSelectedItems();

        if (selectedCharIndices.Length == 0)
        {
            GD.Print("Please select a character to unassign.");
            return;
        }

        int characterIndex = _characterList.GetItemMetadata(selectedCharIndices[0]).AsInt32();

        // Call the GameManager method to perform the unassignment.
        _gameManager.UnassignCharacter(characterIndex);
    }
}