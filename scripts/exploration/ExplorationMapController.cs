using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Root controller for explorable map scenes.
/// Handles party actor attachment/spawning, map state access, and battle return restoration.
/// </summary>
[GlobalClass]
public partial class ExplorationMapController : Node3D, IBattleReturnStateProvider
{
    [Export]
    public string MapId { get; private set; } = string.Empty;

    [Export]
    public NodePath PartyRootPath { get; private set; }

    [Export]
    public string DefaultSpawnId { get; private set; } = "default";

    [Export]
    public PackedScene FallbackPlayerScene { get; private set; }

    [Export]
    public NodePath EncounterManagerPath { get; private set; }

    [Export]
    public bool AutoAttachPlayerController { get; private set; } = true;

    [Export]
    public bool AutoAttachLayerParticipant { get; private set; } = true;

    [Export]
    public bool AutoAttachInteractor { get; private set; } = true;

    [Export]
    public bool InvertPlayerVerticalInput { get; private set; } = true;

    [Export]
    public NodePath MusicControllerPath { get; private set; } = "ExplorationMusicController";

    public EncounterManager EncounterManager { get; private set; }
    public ExplorationMusicController MusicController { get; private set; }
    public CharacterBody3D PlayerActor => _playerActor;

    private CharacterBody3D _playerActor;
    private Node3D _partyRoot;

    public override void _Ready()
    {
        ResolveMapId();
        ResolveReferences();
        EnsurePlayerActor();
        PlacePlayerAtPendingSpawnIfPresent();
    }

    public Godot.Collections.Dictionary CaptureBattleReturnState()
    {
        var state = new Godot.Collections.Dictionary
        {
            ["map_id"] = MapId
        };

        if (_playerActor != null && GodotObject.IsInstanceValid(_playerActor))
        {
            state["player_position"] = _playerActor.GlobalPosition;
            state["player_rotation"] = _playerActor.GlobalRotation;

            var layerParticipant = _playerActor.GetNodeOrNull<MapLayerParticipant>(MapLayerParticipant.DefaultName);
            if (layerParticipant != null)
            {
                state["player_map_layer"] = layerParticipant.CurrentLayer;
            }
        }

        return state;
    }

    public void RestoreBattleReturnState(Godot.Collections.Dictionary state)
    {
        if (state == null || state.Count == 0) return;
        EnsurePlayerActor();

        if (_playerActor == null) return;

        if (state.TryGetValue("player_position", out var posVariant) && posVariant.VariantType == Variant.Type.Vector3)
        {
            _playerActor.GlobalPosition = posVariant.AsVector3();
        }

        if (state.TryGetValue("player_rotation", out var rotVariant) && rotVariant.VariantType == Variant.Type.Vector3)
        {
            _playerActor.GlobalRotation = rotVariant.AsVector3();
        }

        if (state.TryGetValue("player_map_layer", out var layerVariant) && layerVariant.VariantType == Variant.Type.Int)
        {
            var layerParticipant = _playerActor.GetNodeOrNull<MapLayerParticipant>(MapLayerParticipant.DefaultName);
            layerParticipant?.SetLayer(layerVariant.AsInt32());
        }
    }

    public bool GetMapFlag(string key, bool defaultValue = false)
    {
        return MapRuntimeStateStore.GetBool(MapId, key, defaultValue);
    }

    public void SetMapFlag(string key, bool value)
    {
        MapRuntimeStateStore.SetBool(MapId, key, value);
    }

    public bool IsPartyMemberPresent(string name, bool allowPartialMatch = true)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        var entries = new List<string>
        {
            _playerActor?.Name ?? string.Empty
        };

        if (_playerActor is BaseCharacter baseCharacter)
        {
            entries.Add(baseCharacter.PresentationData?.DisplayName ?? string.Empty);
            entries.Add(GetSceneStem(baseCharacter.SceneFilePath));
        }

        var gameManager = GetNodeOrNull<GameManager>(GameManager.Path);
        if (gameManager != null)
        {
            foreach (var member in gameManager.ActiveParty.OfType<Node>())
            {
                entries.Add(member.Name);
                entries.Add(GetSceneStem(member.SceneFilePath));

                if (member is BaseCharacter bc)
                {
                    entries.Add(bc.PresentationData?.DisplayName ?? string.Empty);
                }
            }
        }

        if (allowPartialMatch)
        {
            return entries.Any(e => !string.IsNullOrWhiteSpace(e) && e.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        return entries.Any(e => string.Equals(e, name, StringComparison.OrdinalIgnoreCase));
    }

    private void ResolveMapId()
    {
        if (!string.IsNullOrWhiteSpace(MapId)) return;
        MapId = !string.IsNullOrWhiteSpace(SceneFilePath) ? SceneFilePath : Name;
    }

    private void ResolveReferences()
    {
        _partyRoot = GetNodeOrNull<Node3D>(PartyRootPath) ?? this;
        EncounterManager = GetNodeOrNull<EncounterManager>(EncounterManagerPath)
            ?? GetNodeOrNull<EncounterManager>("EncounterManager");
        MusicController = GetNodeOrNull<ExplorationMusicController>(MusicControllerPath)
            ?? GetNodeOrNull<ExplorationMusicController>("ExplorationMusicController");
    }

    private void EnsurePlayerActor()
    {
        if (_playerActor != null && GodotObject.IsInstanceValid(_playerActor))
        {
            ConfigurePlayerActor(_playerActor);
            return;
        }

        _playerActor = GetTree()
            .GetNodesInGroup(GameGroups.PlayerCharacters)
            .OfType<CharacterBody3D>()
            .FirstOrDefault(node => node.GetTree() == GetTree() && IsNodeInCurrentScene(node));

        if (_playerActor == null)
        {
            _playerActor = PullOrSpawnPartyLeader();
        }

        ConfigurePlayerActor(_playerActor);
    }

    private CharacterBody3D PullOrSpawnPartyLeader()
    {
        var gameManager = GetNodeOrNull<GameManager>(GameManager.Path);
        if (gameManager != null)
        {
            var existing = gameManager.ActiveParty.OfType<CharacterBody3D>().FirstOrDefault();
            if (existing != null)
            {
                existing.GetParent()?.RemoveChild(existing);
                _partyRoot.AddChild(existing);
                return existing;
            }
        }

        if (FallbackPlayerScene == null)
        {
            GD.PrintErr("[ExplorationMapController] No fallback player scene configured.");
            return null;
        }

        var instance = FallbackPlayerScene.Instantiate<CharacterBody3D>();
        if (instance == null)
        {
            GD.PrintErr("[ExplorationMapController] FallbackPlayerScene does not instantiate a CharacterBody3D.");
            return null;
        }

        _partyRoot.AddChild(instance);
        return instance;
    }

    private void ConfigurePlayerActor(CharacterBody3D actor)
    {
        if (actor == null) return;

        if (!actor.IsInGroup(GameGroups.PlayerCharacters))
        {
            actor.AddToGroup(GameGroups.PlayerCharacters);
        }

        PlayerController playerController = actor.GetNodeOrNull<PlayerController>(PlayerController.DefaultName);
        if (AutoAttachPlayerController && playerController == null)
        {
            playerController = new PlayerController { Name = PlayerController.DefaultName };
            actor.AddChild(playerController);
        }

        if (playerController != null)
        {
            playerController.InvertVerticalInput = InvertPlayerVerticalInput;
        }

        if (AutoAttachLayerParticipant && actor.GetNodeOrNull<MapLayerParticipant>(MapLayerParticipant.DefaultName) == null)
        {
            actor.AddChild(new MapLayerParticipant { Name = MapLayerParticipant.DefaultName });
        }

        if (AutoAttachInteractor && actor.GetNodeOrNull<ExplorationInteractor>(ExplorationInteractor.DefaultName) == null)
        {
            actor.AddChild(new ExplorationInteractor { Name = ExplorationInteractor.DefaultName });
        }

        var gameManager = GetNodeOrNull<GameManager>(GameManager.Path);
        if (gameManager != null && !gameManager.HasPlayerCharacter(actor))
        {
            gameManager.AddPlayerCharacter(actor);
        }
    }

    private void PlacePlayerAtPendingSpawnIfPresent()
    {
        if (_playerActor == null) return;

        string requestedSpawn = ExplorationTransitionState.ConsumePendingSpawnId();
        if (string.IsNullOrWhiteSpace(requestedSpawn))
        {
            requestedSpawn = DefaultSpawnId;
        }

        if (string.IsNullOrWhiteSpace(requestedSpawn)) return;

        var spawn = FindSpawnPoint(requestedSpawn);
        if (spawn == null) return;

        _playerActor.GlobalPosition = spawn.GlobalPosition;
        _playerActor.GlobalRotation = spawn.GlobalRotation;
    }

    private MapSpawnPoint FindSpawnPoint(string spawnId)
    {
        foreach (var node in GetTree().GetNodesInGroup(ExplorationGroups.MapSpawnPoints))
        {
            if (node is not MapSpawnPoint spawn) continue;
            if (spawn.GetTree() != GetTree()) continue;
            if (!IsNodeInCurrentScene(spawn)) continue;
            if (string.Equals(spawn.SpawnId, spawnId, StringComparison.OrdinalIgnoreCase))
            {
                return spawn;
            }
        }

        return null;
    }

    private static string GetSceneStem(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath)) return string.Empty;
        return Path.GetFileNameWithoutExtension(scenePath);
    }

    private bool IsNodeInCurrentScene(Node node)
    {
        var currentScene = GetTree()?.CurrentScene;
        if (currentScene == null || node == null) return false;

        Node cursor = node;
        while (cursor != null)
        {
            if (cursor == currentScene) return true;
            cursor = cursor.GetParent();
        }

        return false;
    }
}
