using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the overall game state, including player connections and character assignments.
/// This should be set up as an Autoload singleton in your project settings.
/// </summary>
public partial class GameManager : Node
{
    public const string Path = "/root/GameManager";

    /// <summary>
    /// The name chosen by the local player. This is set from the UI before joining.
    /// </summary>
    public string LocalPlayerName { get; set; }

    // This list holds the current party of player characters.
    // It can be modified by story events to add/remove temporary members.
    public List<Node> PlayerParty { get; } = new();

    // Temporary storage for the upcoming battle's data.
    public BattleConfig PendingBattleConfig { get; private set; }

    public int PartyMoney { get; private set; } = 0;
    public int PartyExperience { get; private set; } = 0;

    private NetworkPlayerManager _networkPlayerManager;
    private NodeCollectionSnapshot _partySnapshot;
    private Godot.Collections.Dictionary _returnSceneState;
    private bool _applyReturnStateOnNextSceneChange;

    public override void _Ready()
    {
        // We will now call OnServerCreated directly from HostGame.

        // When a client connects, it needs to tell the server its name.
        this.Subscribe(
            () => Multiplayer.ConnectedToServer += OnConnectedToServer,
            () => Multiplayer.ConnectedToServer -= OnConnectedToServer
        );

        // Get the NetworkPlayerManager node, which is a child of this node in the scene.
        _networkPlayerManager = GetNode<NetworkPlayerManager>("NetworkPlayerManager");

        // Load saved input mappings from disk
        InputPersistenceManager.Load();

        if (_prePauseTimeScale != 1.0)
        {
            SetTimeScale(_prePauseTimeScale);
        }

        GetTree().SceneChanged += OnSceneChanged;
    }

    private double _prePauseTimeScale = 1.0;

    /// <summary>
    /// Sets the global time scale (speed) of the game.
    /// </summary>
    public void SetTimeScale(double scale)
    {
        Engine.TimeScale = System.Math.Max(0.0, scale);
    }

    /// <summary>
    /// Toggles the game between the current time scale and 0 (paused).
    /// </summary>
    public void TogglePause()
    {
        if (Engine.TimeScale == 0.0)
        {
            Engine.TimeScale = _prePauseTimeScale;
        }
        else
        {
            _prePauseTimeScale = Engine.TimeScale;
            if (_prePauseTimeScale == 0.0) _prePauseTimeScale = 1.0;
            Engine.TimeScale = 0.0;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            TogglePause();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// This method is called when the server is successfully created.
    /// It sets up all the server-only nodes and logic.
    /// </summary>
    /// <summary>
    /// Creates a server and initializes server-side logic.
    /// This should be called from your UI (e.g., a "Host" button).
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <param name="maxPlayers">The maximum number of players.</param>
    public void HostGame(int port, int maxPlayers)
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(port, maxPlayers);
        Multiplayer.MultiplayerPeer = peer;
    }

    /// <summary>
    /// Called by the main game scene once it's ready. This is where we can
    /// safely access scene nodes and complete the server setup.
    /// </summary>
    public void FinalizeServerSetup()
    {
        if (!Multiplayer.IsServer()) return; 

        // For now, we'll populate the player party from the scene.
        // In a full game, this would be loaded from a save file.
        // Filter to ensure we only get actual characters (CharacterBody3D), ignoring components that might be in the group.
        var playerCharacters = GetTree().GetNodesInGroup(GameGroups.PlayerCharacters)
            .OfType<CharacterBody3D>().Cast<Node>().ToList();
        foreach (var pc in playerCharacters)
        {
            PlayerParty.Add(pc);
        }
        _networkPlayerManager.Initialize(new Godot.Collections.Array<Node>(playerCharacters));
    }

    public void AddPlayerCharacter(Node character)
    {
        PlayerParty.Add(character);
    }

    /// <summary>
    /// The host assigns a character to a specific player. This would be called from a host-only UI.
    /// </summary>
    /// <param name="targetPeerId">The peer ID of the player to assign the character to.</param>
    /// <param name="characterIndex">The index of the character in the _playerCharacters array.</param>
    public void AssignCharacterToPlayer(long targetPeerId, int characterIndex)
    {
        // The NetworkPlayerManager will handle the server-side check.
        // This keeps the GameManager clean of role-specific logic.
        _networkPlayerManager?.Server_AssignCharacter(targetPeerId, characterIndex);
    }

    /// <summary>
    /// The host unassigns a character, returning it to the pool.
    /// </summary>
    /// <param name="characterIndex">The index of the character to unassign.</param>
    public void UnassignCharacter(int characterIndex)
    {
        // The NetworkPlayerManager will handle the server-side check.
        _networkPlayerManager?.Server_UnassignCharacter(characterIndex);
    }

    /// <summary>
    /// Creates a client and connects to the specified server address.
    /// This should be called from your UI (e.g., a "Join" button).
    /// </summary>
    /// <param name="address">The IP address of the server.</param>
    /// <param name="port">The port to connect to.</param>
    public void JoinGame(string address, int port)
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateClient(address, port);
        Multiplayer.MultiplayerPeer = peer;
    }

    private void OnConnectedToServer()
    {
        // We've established a connection. Now, send an RPC to the server
        // to register ourselves with our chosen name.
        // The RPC is sent to the NetworkPlayerManager on the server (peer ID 1).
        _networkPlayerManager.RpcId(1, nameof(NetworkPlayerManager.Server_RegisterPlayer), LocalPlayerName);
    }

    /// <summary>
    /// Retrieves the name of a player by their peer ID.
    /// </summary>
    public string GetPlayerName(long peerId) => _networkPlayerManager.GetPlayerName(peerId);

    /// <summary>
    /// Prepares the data for an upcoming battle and transitions to the battle scene.
    /// This should be called from the overworld or wherever a battle is initiated.
    /// </summary>
    /// <param name="enemyParties">A list of enemy parties. More than one implies a pincer attack.</param>
    /// <param name="allyParty">An optional party of allies fighting with the player.</param>
    /// <param name="battleScenePath">The path to the MainBattleScene.</param>
    /// <param name="formation">The formation of the battle (e.g., surprise attack).</param>
    /// <param name="seed">An optional seed for the battle.</param>
    /// <param name="music">The music track to play during the battle.</param>
    public void InitiateBattle(List<Godot.Collections.Array<PackedScene>> enemyParties, Godot.Collections.Array<PackedScene> allyParty, string battleScenePath, BattleFormation formation, BattleEnvironmentProfile envProfile = null, ulong? seed = null, BattleMusicData music = null, BattleMusicData postBattleMusic = null, bool allowRetry = true, bool isScriptedLoss = false)
    {
        CapturePartySnapshot();
        CaptureReturnSceneState();

        // Store the data that the BattleController will need.
        var enemyPartiesArray = new Godot.Collections.Array<Godot.Collections.Array<PackedScene>>();
        if (enemyParties != null)
        {
            foreach (var party in enemyParties)
            {
                enemyPartiesArray.Add(party);
            }
        }

        PendingBattleConfig = new BattleConfig
        {
            EnemyParties = enemyPartiesArray,
            AllyParty = allyParty,
            Formation = formation,
            EnvironmentProfile = envProfile,
            BattleMusic = music,
            PostBattleMusic = postBattleMusic,
            BattleScenePath = battleScenePath,
            ReturnScenePath = GetTree().CurrentScene?.SceneFilePath,
            AllowRetry = allowRetry,
            IsScriptedLoss = isScriptedLoss,
            HasSeed = seed.HasValue,
            Seed = seed ?? 0
        };

        foreach (var player in PlayerParty)
        {
            player.GetParent()?.RemoveChild(player);
            AddChild(player);
        }


        // Change to the battle scene.
        GetTree().ChangeSceneToFile(battleScenePath);
    }

    public BattleConfig GetOrCreatePendingBattleConfig()
    {
        if (PendingBattleConfig == null)
        {
            PendingBattleConfig = new BattleConfig();
        }
        return PendingBattleConfig;
    }

    public void AddPartyMoney(int amount)
    {
        if (amount <= 0) return;
        PartyMoney += amount;
    }

    public void AddPartyExperience(int amount)
    {
        if (amount <= 0) return;
        PartyExperience += amount;
    }

    public void ReturnFromBattle(BattleController.BattleState result, BattleRewards rewards, bool wasScriptedLoss)
    {
        // Persist party by moving them under the GameManager before leaving the battle scene.
        foreach (var player in PlayerParty)
        {
            player.GetParent()?.RemoveChild(player);
            AddChild(player);
        }

        var returnPath = PendingBattleConfig?.ReturnScenePath;
        if (!string.IsNullOrEmpty(returnPath))
        {
            _applyReturnStateOnNextSceneChange = true;
            GetTree().ChangeSceneToFile(returnPath);
        }
    }

    public void RestartBattleFromSnapshot()
    {
        if (_partySnapshot == null)
        {
            GD.PrintErr("Cannot restart battle: no party snapshot available.");
            return;
        }
        if (PendingBattleConfig == null || string.IsNullOrEmpty(PendingBattleConfig.BattleScenePath))
        {
            GD.PrintErr("Cannot restart battle: missing pending battle config or battle scene path.");
            return;
        }

        RestorePartyFromSnapshot();
        GetTree().ChangeSceneToFile(PendingBattleConfig.BattleScenePath);
    }

    private void CapturePartySnapshot()
    {
        _partySnapshot = new NodeCollectionSnapshot(PlayerParty);
    }

    private void RestorePartyFromSnapshot()
    {
        foreach (var player in PlayerParty)
        {
            player.QueueFree();
        }
        PlayerParty.Clear();

        var restored = _partySnapshot.InstantiateAll();
        foreach (var player in restored)
        {
            AddChild(player);
            PlayerParty.Add(player);
        }
    }

    private void CaptureReturnSceneState()
    {
        var provider = GetTree().CurrentScene as IBattleReturnStateProvider;
        _returnSceneState = provider?.CaptureBattleReturnState();
    }

    private void OnSceneChanged()
    {
        if (!_applyReturnStateOnNextSceneChange) return;
        _applyReturnStateOnNextSceneChange = false;

        if (_returnSceneState == null) return;
        var newScene = GetTree().CurrentScene;
        if (newScene is IBattleReturnStateProvider provider)
        {
            provider.RestoreBattleReturnState(_returnSceneState);
        }
        _returnSceneState = null;
    }
}
