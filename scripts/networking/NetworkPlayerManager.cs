using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles the logic for assigning existing characters to connecting players.
/// This node should be owned by a central manager (like GameManager) and only run on the server.
/// </summary>
public partial class NetworkPlayerManager : Node
{
    private Godot.Collections.Array<Node> _playerCharacters;
    // Maps Character Node -> Owner Peer ID. Allows one player to own multiple characters.
    private readonly Dictionary<Node, long> _assignedCharacters = new();
    private readonly Dictionary<long, string> _playerNames = new();
    private readonly HashSet<string> _usedNames = new(System.StringComparer.OrdinalIgnoreCase);
    private ChatManager _chatManager;
    private IMessageSink _logger;

    [Signal]
    public delegate void PlayerListChangedEventHandler();

    [Signal]
    public delegate void CharacterAssignmentChangedEventHandler();

    /// <summary>
    /// Emitted after the manager has been initialized with scene-specific data.
    /// </summary>
    [Signal]
    public delegate void InitializedEventHandler();

    /// <summary>
    /// Initializes the manager with the list of controllable characters and hooks into multiplayer events.
    /// Must be called by the owner on the server.
    /// </summary>
    public void Initialize(Godot.Collections.Array<Node> characters)
    {
        _playerCharacters = characters;

        // The server (host) is also a player. Now that the scene is loaded and
        // we are initialized, we can safely register them.
        if (Multiplayer.IsServer())
        {
            var gameManager = GetNode<GameManager>(GameManager.Path);
            Server_RegisterPlayer(gameManager.LocalPlayerName, 1, false); // Don't emit signal yet

            // Default all characters to the server host (Peer 1)
            foreach (var character in _playerCharacters)
            {
                _assignedCharacters[character] = 1;
                character.SetMultiplayerAuthority(1);
                EnsurePlayerController(character);
            }
        }
        
        // Let the UI know that initialization is complete.
        EmitSignal(SignalName.Initialized);
    }

    public override void _Ready()
    {
        // This node is part of the GameManager scene, so it exists from the start.
        // We can safely hook up signals and get references here.
        _chatManager = GetNode<ChatManager>("/root/ChatOverlay");

        // Initialize the logger immediately. This prevents null reference errors
        // in methods called before the main game scene is loaded.
        // We default to a ChatLogSink on the server, and a safe DebugLogSink otherwise.
        _logger = Multiplayer.IsServer() ? 
            new ChatLogSink(_chatManager) : new DebugLogSink();

        this.Subscribe(
            () => Multiplayer.PeerConnected += OnPlayerConnected,
            () => Multiplayer.PeerConnected -= OnPlayerConnected
        );
        this.Subscribe(
            () => Multiplayer.PeerDisconnected += OnPlayerDisconnected,
            () => Multiplayer.PeerDisconnected -= OnPlayerDisconnected
        );
        this.Subscribe(
            () => GetTree().NodeAdded += OnNodeAdded,
            () => GetTree().NodeAdded -= OnNodeAdded
        );
    }

    private void OnPlayerConnected(long peerId)
    {
        // This is a good place for logic that happens as soon as a peer connects,
        // before they have registered with a name.
        if (Multiplayer.IsServer() && _playerCharacters != null)
        {
            // Sync existing assignments to the new player
            foreach (var kvp in _assignedCharacters)
            {
                int index = _playerCharacters.IndexOf(kvp.Key);
                if (index != -1)
                {
                    RpcId(peerId, nameof(Sync_AssignCharacter), kvp.Value, index);
                }
                else
                {
                    RpcId(peerId, nameof(Sync_AssignControlToActor), kvp.Key.GetPath(), kvp.Value);
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Server_RegisterPlayer(string name)
    {
        long peerId = Multiplayer.GetRemoteSenderId();
        _logger.LogInfo($"[NetworkPlayerManager] Received registration request from Peer {peerId} with name '{name}'.");
        Server_RegisterPlayer(name, peerId);
    }

    /// <summary>
    /// Internal method to register a player. Can be told not to emit a signal for initial setup.
    /// </summary>
    private void Server_RegisterPlayer(string name, long peerId, bool emitSignal = true)
    {
        if (!Multiplayer.IsServer()) return;

        // Sanitize empty names to a default.
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Player";
        }

        // If the name is already in use, make it unique by appending a number.
        // This logic now applies to all players, including the host.
        if (_usedNames.Contains(name))
        {
            _logger.LogWarning($"Name '{name}' is already in use. Making it unique.");
            int suffix = 2;
            string originalName = name;
            while (_usedNames.Contains(name))
            {
                name = $"{originalName}_{suffix++}";
            }
        }

        _playerNames[peerId] = name;
        _usedNames.Add(name);
        _chatManager.BroadcastSystemMessage($"{name} has joined the game.");
        _logger.LogInfo($"Player '{name}' (Peer {peerId}) is now registered.");
        if (emitSignal)
        {
            EmitSignal(SignalName.PlayerListChanged);
        }
    }

    /// <summary>
    /// Assigns a character to a player. This can only be called by the host (server).
    /// </summary>
    /// <param name="targetPeerId">The ID of the player to receive the character.</param>
    /// <param name="characterIndex">The index of the character in the pre-defined list.</param>
    public void Server_AssignCharacter(long targetPeerId, int characterIndex)
    {
        // This method should only ever run on the server.
        if (!Multiplayer.IsServer()) return;

        if (characterIndex < 0 || characterIndex >= _playerCharacters.Count)
        {
            _logger.LogError($"Invalid character index: {characterIndex}");
            return;
        }

        var characterToAssign = _playerCharacters[characterIndex];

        // Check if the character is already assigned to someone.
        if (_assignedCharacters.TryGetValue(characterToAssign, out long currentOwner) && currentOwner != 1)
        {
            // Only allow reassignment if the current owner is the host (1)
            _logger.LogWarning($"Character '{characterToAssign.Name}' is already assigned to Peer {currentOwner}.");
            return;
        }

        // Broadcast the assignment to all clients (including self via CallLocal=true).
        Rpc(nameof(Sync_AssignCharacter), targetPeerId, characterIndex);
    }

    /// <summary>
    /// RPC called by a client to request control of a character.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Server_RequestControl(int characterIndex)
    {
        if (!Multiplayer.IsServer()) return;

        long requesterId = Multiplayer.GetRemoteSenderId();

        if (characterIndex < 0 || characterIndex >= _playerCharacters.Count)
        {
            _logger.LogWarning($"Peer {requesterId} requested invalid character index {characterIndex}.");
            return;
        }

        var character = _playerCharacters[characterIndex];

        // Check if the character is available (owned by host/server or unassigned)
        if (_assignedCharacters.TryGetValue(character, out long currentOwner) && currentOwner != 1)
        {
            _logger.LogInfo($"Peer {requesterId} requested '{character.Name}' but it is owned by {currentOwner}. Request denied.");
            return;
        }

        // If we get here, the character is available (owned by host).
        // We can grant the request.
        _logger.LogInfo($"Granting control of '{character.Name}' to Peer {requesterId}.");
        Server_AssignCharacter(requesterId, characterIndex);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Sync_AssignCharacter(long targetPeerId, int characterIndex)
    {
        // Ensure characters are found if not already initialized (e.g. on clients).
        if (_playerCharacters == null || _playerCharacters.Count == 0)
        {
            // Filter to ensure we only get actual characters, ignoring components like StatsComponent that might be in the group.
            _playerCharacters = new Godot.Collections.Array<Node>(GetTree().GetNodesInGroup(GameGroups.PlayerCharacters).OfType<CharacterBody3D>().Cast<Node>());
        }

        if (characterIndex < 0 || characterIndex >= _playerCharacters.Count) return;

        var character = _playerCharacters[characterIndex];
        character.SetMultiplayerAuthority((int)targetPeerId);
        _assignedCharacters[character] = targetPeerId;
        
        // Ensure the controller exists and update its input provider
        EnsurePlayerController(character);
        character.GetNode<PlayerController>(PlayerController.DefaultName).OnAuthorityChanged();

        string playerName = GetPlayerName(targetPeerId);
        _logger?.LogInfo($"[NetworkPlayerManager] Assigned character '{character.Name}' to player '{playerName}' (Peer ID {targetPeerId}).");
        EmitSignal(SignalName.CharacterAssignmentChanged);
    }

    /// <summary>
    /// Unassigns a character, returning control to the host.
    /// This can only be called by the host (server).
    /// </summary>
    /// <param name="characterIndex">The index of the character to unassign.</param>
    public void Server_UnassignCharacter(int characterIndex)
    {
        if (!Multiplayer.IsServer()) return;

        if (characterIndex < 0 || characterIndex >= _playerCharacters.Count)
        {
            _logger.LogError($"Invalid character index for unassignment: {characterIndex}");
            return;
        }

        var characterToUnassign = _playerCharacters[characterIndex];
        if (_assignedCharacters.ContainsKey(characterToUnassign) && _assignedCharacters[characterToUnassign] != 1)
        {
            Rpc(nameof(Sync_AssignCharacter), 1, characterIndex);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Sync_UnassignCharacter(int characterIndex)
    {
        if (_playerCharacters == null || _playerCharacters.Count == 0)
        {
            _playerCharacters = new Godot.Collections.Array<Node>(GetTree().GetNodesInGroup(GameGroups.PlayerCharacters).OfType<CharacterBody3D>().Cast<Node>());
        }

        if (characterIndex < 0 || characterIndex >= _playerCharacters.Count) return;

        var character = _playerCharacters[characterIndex];
        if (_assignedCharacters.ContainsKey(character))
        {
            _assignedCharacters.Remove(character);
        }

        character.SetMultiplayerAuthority(1); // Return authority to server.
        
        // Reset the controller to server authority
        character.GetNode<PlayerController>(PlayerController.DefaultName)?.OnAuthorityChanged();
        EmitSignal(SignalName.CharacterAssignmentChanged);
    }

    /// <summary>
    /// Assigns authority of any network-synced node (like an enemy) to a player.
    /// This supports "Mind Control" mechanics where the target is not in the predefined player list.
    /// </summary>
    public void Server_AssignControlToActor(Node actor, long peerId)
    {
        if (!Multiplayer.IsServer()) return;
        Rpc(nameof(Sync_AssignControlToActor), actor.GetPath(), peerId);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Sync_AssignControlToActor(string actorPath, long peerId)
    {
        var actor = GetNodeOrNull(actorPath);
        if (actor == null)
        {
            _logger?.LogWarning($"[NetworkPlayerManager] Could not find actor at path: {actorPath}");
            return;
        }

        actor.SetMultiplayerAuthority((int)peerId);
        _assignedCharacters[actor] = peerId; // Track assignment so we can clean up on disconnect
        
        // Dynamically attach the controller if it's missing (e.g. on an Enemy)
        EnsurePlayerController(actor);
        actor.GetNode<PlayerController>(PlayerController.DefaultName)?.OnAuthorityChanged();
    }

    private void OnPlayerDisconnected(long peerId)
    {
        GD.Print($"[NetworkPlayerManager] Player disconnected: {peerId}");
        
        // Find all characters owned by this player
        var ownedCharacters = _assignedCharacters.Where(kvp => kvp.Value == peerId).Select(kvp => kvp.Key).ToList();

        foreach (var character in ownedCharacters)
        {
            // Reclaim to Host (1)
            int index = _playerCharacters.IndexOf(character);
            if (index != -1)
            {
                Rpc(nameof(Sync_AssignCharacter), 1, index);
            }
            else
            {
                Rpc(nameof(Sync_AssignControlToActor), character.GetPath(), 1);
            }
            _logger.LogInfo($"[NetworkPlayerManager] Reclaimed character '{character.Name}' from Peer ID {peerId}.");
        }

        if (_playerNames.TryGetValue(peerId, out string name))
        {
            _playerNames.Remove(peerId);
            _usedNames.Remove(name);
            _chatManager.BroadcastSystemMessage($"{name} has left the game.");
            _logger.LogInfo($"Freed up name '{name}'.");
            // Emit signal once after processing all characters
            EmitSignal(SignalName.PlayerListChanged);
        }
    }

    /// <summary>
    /// Retrieves the name of a player by their peer ID.
    /// </summary>
    public string GetPlayerName(long peerId) => _playerNames.GetValueOrDefault(peerId, "Unknown");

    // Public methods for the UI to get data
    public IReadOnlyDictionary<long, string> GetPlayerList() => _playerNames;
    public Godot.Collections.Array<Node> GetCharacters() => _playerCharacters;
    public IReadOnlyDictionary<Node, long> GetAssignedCharacters() => _assignedCharacters;

    private void OnNodeAdded(Node node)
    {
        // Ensure we only attach to valid character bodies, avoiding components like StatsComponent that might be in the group.
        if (node is CharacterBody3D && node.IsInGroup(GameGroups.PlayerCharacters))
        {
            EnsurePlayerController(node);
        }
    }

    private void EnsurePlayerController(Node character)
    {
        var controller = character.GetNodeOrNull<PlayerController>(PlayerController.DefaultName);
        if (controller == null)
        {
            controller = new PlayerController { Name = PlayerController.DefaultName };
            character.AddChild(controller);
        }

        // Ensure the controller's authority matches the character's authority.
        // This handles cases where the character might have been assigned before the controller was added.
        int parentAuth = character.GetMultiplayerAuthority();
        if (controller.GetMultiplayerAuthority() != parentAuth)
        {
            controller.SetMultiplayerAuthority(parentAuth);
            controller.OnAuthorityChanged();
        }
    }
}