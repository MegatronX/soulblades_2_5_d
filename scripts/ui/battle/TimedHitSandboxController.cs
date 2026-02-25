using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Authoring sandbox for timed-hit tuning on real action execution flow.
/// Supports per-run action/character/window timing offsets and VFX timing overrides.
/// </summary>
public partial class TimedHitSandboxController : Control
{
    [ExportGroup("Characters")]
    [Export]
    public Godot.Collections.Array<PackedScene> CharacterSceneLibrary { get; set; } = new();

    [Export]
    public PackedScene DefaultActorScene { get; set; }

    [Export]
    public PackedScene DefaultTargetScene { get; set; }

    [Export]
    public BaseStats FallbackBaseStats { get; set; }

    private Node3D _playerAnchor;
    private Node3D _enemyAnchor;

    private OptionButton _actorSceneSelect;
    private OptionButton _targetSceneSelect;
    private Button _respawnActorButton;
    private Button _respawnTargetButton;
    private Button _respawnBothButton;

    private OptionButton _actionSelect;
    private OptionButton _windowSelect;
    private Button _refreshActionsButton;
    private Button _runActionButton;

    private SpinBox _actionOffsetSpin;
    private SpinBox _characterOffsetSpin;
    private SpinBox _windowOffsetSpin;
    private SpinBox _travelDelayOffsetSpin;
    private SpinBox _travelDurationOffsetSpin;
    private SpinBox _impactPrewarmOffsetSpin;
    private Button _commitWindowOffsetButton;
    private Button _commitVisualTimingButton;
    private Button _resetOffsetsButton;

    private Label _summaryLabel;
    private Label _timingLabel;
    private RichTextLabel _logLabel;

    private ActionDirector _actionDirector;
    private TimedHitManager _timedHitManager;
    private BattleMechanics _battleMechanics;

    private Node _actor;
    private Node _target;
    private ActionContext _activeContext;
    private bool _runInProgress;
    private ulong _runCounter;

    private readonly List<PackedScene> _characterOptions = new();
    private readonly List<ActionData> _actionOptions = new();

    private const string PlayerSceneFallback = "res://assets/resources/characters/ceira/ceira.tscn";
    private const string EnemySceneFallback = "res://assets/resources/characters/goblin_lv5/goblin_lv5_animated.tscn";
    private const string TemplateSceneFallback = "res://assets/resources/characters/CharacterTemplate.tscn";

    public override void _Ready()
    {
        CacheNodes();
        EnsureActionSystems();
        EnsureCharacterLibrary();
        WireUi();

        PopulateCharacterSelectors();
        RespawnBoth();
        RefreshActionList();
        RefreshSummary();

        Log("Timed Hit Sandbox ready.");
    }

    public override void _ExitTree()
    {
        if (_timedHitManager != null)
        {
            _timedHitManager.TimedHitResolvedDetailed -= OnTimedHitResolvedDetailed;
        }
    }

    private void CacheNodes()
    {
        _playerAnchor = GetNodeOrNull<Node3D>("SandboxWorld/PlayerAnchor");
        _enemyAnchor = GetNodeOrNull<Node3D>("SandboxWorld/EnemyAnchor");

        _actorSceneSelect = GetNodeOrNull<OptionButton>("UI/LeftPanel/CharacterPanel/Body/ActorRow/ActorSceneSelect");
        _targetSceneSelect = GetNodeOrNull<OptionButton>("UI/LeftPanel/CharacterPanel/Body/TargetRow/TargetSceneSelect");
        _respawnActorButton = GetNodeOrNull<Button>("UI/LeftPanel/CharacterPanel/Body/ActorRow/RespawnActorButton");
        _respawnTargetButton = GetNodeOrNull<Button>("UI/LeftPanel/CharacterPanel/Body/TargetRow/RespawnTargetButton");
        _respawnBothButton = GetNodeOrNull<Button>("UI/LeftPanel/CharacterPanel/Body/RespawnBothButton");

        _actionSelect = GetNodeOrNull<OptionButton>("UI/LeftPanel/ActionPanel/Body/ActionRow/ActionSelect");
        _windowSelect = GetNodeOrNull<OptionButton>("UI/LeftPanel/ActionPanel/Body/ActionRow/WindowSelect");
        _refreshActionsButton = GetNodeOrNull<Button>("UI/LeftPanel/ActionPanel/Body/ActionButtonsRow/RefreshActionsButton");
        _runActionButton = GetNodeOrNull<Button>("UI/LeftPanel/ActionPanel/Body/ActionButtonsRow/RunActionButton");

        _actionOffsetSpin = GetNodeOrNull<SpinBox>("UI/LeftPanel/TuningPanel/Body/TimingOffsetRow/ActionOffsetSpin");
        _characterOffsetSpin = GetNodeOrNull<SpinBox>("UI/LeftPanel/TuningPanel/Body/TimingOffsetRow/CharacterOffsetSpin");
        _windowOffsetSpin = GetNodeOrNull<SpinBox>("UI/LeftPanel/TuningPanel/Body/TimingOffsetRow/WindowOffsetSpin");
        _travelDelayOffsetSpin = GetNodeOrNull<SpinBox>("UI/LeftPanel/TuningPanel/Body/VfxOffsetRow/TravelDelayOffsetSpin");
        _travelDurationOffsetSpin = GetNodeOrNull<SpinBox>("UI/LeftPanel/TuningPanel/Body/VfxOffsetRow/TravelDurationOffsetSpin");
        _impactPrewarmOffsetSpin = GetNodeOrNull<SpinBox>("UI/LeftPanel/TuningPanel/Body/VfxOffsetRow/ImpactPrewarmOffsetSpin");
        _commitWindowOffsetButton = GetNodeOrNull<Button>("UI/LeftPanel/TuningPanel/Body/CommitRow/CommitWindowOffsetButton");
        _commitVisualTimingButton = GetNodeOrNull<Button>("UI/LeftPanel/TuningPanel/Body/CommitRow/CommitVisualTimingButton");
        _resetOffsetsButton = GetNodeOrNull<Button>("UI/LeftPanel/TuningPanel/Body/CommitRow/ResetOffsetsButton");

        _summaryLabel = GetNodeOrNull<Label>("UI/RightPanel/StatePanel/Body/SummaryLabel");
        _timingLabel = GetNodeOrNull<Label>("UI/RightPanel/StatePanel/Body/TimingLabel");
        _logLabel = GetNodeOrNull<RichTextLabel>("UI/RightPanel/LogPanel/Body/LogLabel");

        _actionDirector = GetNodeOrNull<ActionDirector>("ActionDirector");
        _timedHitManager = GetNodeOrNull<TimedHitManager>("ActionDirector/TimedHitManager");
        _battleMechanics = GetNodeOrNull<BattleMechanics>("ActionDirector/BattleMechanics");
    }

    private void EnsureActionSystems()
    {
        if (_battleMechanics != null)
        {
            var strategy = new CalculationStrategy();
            strategy.Set("HitLogic", new StandardHitStrategy());
            strategy.Set("CritLogic", new StandardCritStrategy());
            strategy.Set("DamageLogic", new StandardDamageStrategy());
            _battleMechanics.Set("_defaultStrategy", strategy);
        }

        if (_timedHitManager != null)
        {
            _timedHitManager.TimedHitResolvedDetailed += OnTimedHitResolvedDetailed;
        }
    }

    private void EnsureCharacterLibrary()
    {
        _characterOptions.Clear();
        if (CharacterSceneLibrary != null)
        {
            foreach (var scene in CharacterSceneLibrary)
            {
                if (scene != null) _characterOptions.Add(scene);
            }
        }

        if (_characterOptions.Count == 0)
        {
            var ceira = GD.Load<PackedScene>(PlayerSceneFallback);
            var goblin = GD.Load<PackedScene>(EnemySceneFallback);
            var template = GD.Load<PackedScene>(TemplateSceneFallback);

            if (ceira != null) _characterOptions.Add(ceira);
            if (goblin != null) _characterOptions.Add(goblin);
            if (template != null) _characterOptions.Add(template);
        }

        if (DefaultActorScene == null)
        {
            DefaultActorScene = _characterOptions.FirstOrDefault();
        }

        if (DefaultTargetScene == null)
        {
            DefaultTargetScene = _characterOptions.Count > 1 ? _characterOptions[1] : _characterOptions.FirstOrDefault();
        }
    }

    private void WireUi()
    {
        _respawnActorButton?.Connect(Button.SignalName.Pressed, Callable.From(RespawnActor));
        _respawnTargetButton?.Connect(Button.SignalName.Pressed, Callable.From(RespawnTarget));
        _respawnBothButton?.Connect(Button.SignalName.Pressed, Callable.From(RespawnBoth));

        _refreshActionsButton?.Connect(Button.SignalName.Pressed, Callable.From(RefreshActionList));
        _runActionButton?.Connect(Button.SignalName.Pressed, Callable.From(RunSelectedActionAsync));
        _commitWindowOffsetButton?.Connect(Button.SignalName.Pressed, Callable.From(CommitWindowOffsetToAction));
        _commitVisualTimingButton?.Connect(Button.SignalName.Pressed, Callable.From(CommitVisualTimingToAction));
        _resetOffsetsButton?.Connect(Button.SignalName.Pressed, Callable.From(ResetOffsets));

        if (_actionSelect != null)
        {
            _actionSelect.ItemSelected += _ => RefreshWindowSelector();
        }
    }

    private void PopulateCharacterSelectors()
    {
        PopulateCharacterSelector(_actorSceneSelect, DefaultActorScene);
        PopulateCharacterSelector(_targetSceneSelect, DefaultTargetScene);
    }

    private void PopulateCharacterSelector(OptionButton option, PackedScene preferred)
    {
        if (option == null) return;
        option.Clear();
        int selected = 0;

        for (int i = 0; i < _characterOptions.Count; i++)
        {
            var scene = _characterOptions[i];
            option.AddItem(GetSceneDisplayName(scene), i);
            if (scene == preferred) selected = i;
        }

        if (_characterOptions.Count > 0)
        {
            option.Selected = Mathf.Clamp(selected, 0, _characterOptions.Count - 1);
        }
    }

    private static string GetSceneDisplayName(PackedScene packed)
    {
        if (packed == null) return "(none)";
        if (string.IsNullOrEmpty(packed.ResourcePath)) return packed.ResourceName;
        return packed.ResourcePath.GetFile().GetBaseName();
    }

    private void RespawnBoth()
    {
        RespawnActor();
        RespawnTarget();
    }

    private void RespawnActor()
    {
        var scene = ResolveSelectedScene(_actorSceneSelect) ?? DefaultActorScene;
        _actor = SpawnCombatant(_actor, scene, _playerAnchor, true, "Actor");
        RefreshActionList();
        RefreshSummary();
    }

    private void RespawnTarget()
    {
        var scene = ResolveSelectedScene(_targetSceneSelect) ?? DefaultTargetScene;
        _target = SpawnCombatant(_target, scene, _enemyAnchor, false, "Target");
        RefreshSummary();
    }

    private PackedScene ResolveSelectedScene(OptionButton selector)
    {
        if (selector == null || _characterOptions.Count == 0) return null;
        int index = Mathf.Clamp(selector.Selected, 0, _characterOptions.Count - 1);
        return _characterOptions[index];
    }

    private Node SpawnCombatant(Node existing, PackedScene scene, Node3D anchor, bool isPlayer, string fallbackName)
    {
        if (existing != null && GodotObject.IsInstanceValid(existing))
        {
            existing.QueueFree();
        }

        if (anchor == null) return null;

        Node spawned = scene?.Instantiate();
        if (spawned == null)
        {
            spawned = CreateFallbackCombatant(fallbackName);
        }

        spawned.Name = fallbackName;
        anchor.AddChild(spawned);

        if (isPlayer) spawned.AddToGroup(GameGroups.PlayerCharacters);
        else spawned.RemoveFromGroup(GameGroups.PlayerCharacters);

        EnsureCombatantComponents(spawned);
        PositionCombatant(spawned, isPlayer);
        return spawned;
    }

    private Node CreateFallbackCombatant(string fallbackName)
    {
        var character = new BaseCharacter { Name = fallbackName };
        var stats = new StatsComponent { Name = StatsComponent.NodeName };
        stats.SetBaseStatsResource(ResolveFallbackStats());
        character.AddChild(stats);
        character.AddChild(new StatusEffectManager { Name = StatusEffectManager.NodeName });
        character.AddChild(new AbilityManager { Name = AbilityManager.NodeName });
        character.AddChild(new ActionManager { Name = ActionManager.DefaultName });
        return character;
    }

    private void EnsureCombatantComponents(Node combatant)
    {
        if (combatant == null) return;

        var stats = combatant.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null)
        {
            stats = new StatsComponent { Name = StatsComponent.NodeName };
            combatant.AddChild(stats);
        }

        if (stats.GetStatValue(StatType.HP) <= 0 || stats.GetStatValue(StatType.MP) <= 0)
        {
            stats.SetBaseStatsResource(ResolveFallbackStats());
        }

        if (combatant.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName) == null)
        {
            combatant.AddChild(new StatusEffectManager { Name = StatusEffectManager.NodeName });
        }

        if (combatant.GetNodeOrNull<AbilityManager>(AbilityManager.NodeName) == null)
        {
            combatant.AddChild(new AbilityManager { Name = AbilityManager.NodeName });
        }

        if (combatant.GetNodeOrNull<ActionManager>(ActionManager.DefaultName) == null)
        {
            combatant.AddChild(CreateDefaultActionManager());
        }
    }

    private ActionManager CreateDefaultActionManager()
    {
        var manager = new ActionManager { Name = ActionManager.DefaultName };
        var attack = GD.Load<ActionData>("res://assets/resources/actions/Attack.tres");
        var fire = GD.Load<ActionData>("res://assets/resources/actions/Magic/Fire.tres");
        var haste = GD.Load<ActionData>("res://assets/resources/actions/Magic/haste.tres");

        var page = new BattleCategory { CommandName = "Main" };
        page.SubCommands = new Godot.Collections.Array<BattleCommand>();
        if (attack != null) page.SubCommands.Add(attack);
        if (fire != null) page.SubCommands.Add(fire);
        if (haste != null) page.SubCommands.Add(haste);
        manager.RootPages = new Godot.Collections.Array<BattleCategory> { page };
        return manager;
    }

    private BaseStats ResolveFallbackStats()
    {
        return FallbackBaseStats ?? new BaseStats
        {
            HP = 350,
            MP = 150,
            Strength = 24,
            Defense = 20,
            Magic = 22,
            MagicDefense = 19,
            Speed = 16,
            Evasion = 6,
            MgEvasion = 6,
            Accuracy = 12,
            MgAccuracy = 12,
            Luck = 8,
            AP = 30
        };
    }

    private static void PositionCombatant(Node combatant, bool isPlayer)
    {
        if (combatant is not Node3D node3D) return;
        node3D.Position = isPlayer ? new Vector3(-2.5f, 0f, 0f) : new Vector3(2.5f, 0f, 0f);
        node3D.Rotation = isPlayer ? Vector3.Zero : new Vector3(0f, Mathf.Pi, 0f);
    }

    private void RefreshActionList()
    {
        _actionOptions.Clear();
        _actionSelect?.Clear();

        var actor = _actor;
        var manager = actor?.GetNodeOrNull<ActionManager>(ActionManager.DefaultName);
        if (manager == null || _actionSelect == null)
        {
            RefreshWindowSelector();
            return;
        }

        var unique = new HashSet<ActionData>();
        foreach (var action in EnumerateActionCommands(manager))
        {
            if (action == null || !unique.Add(action)) continue;
            _actionOptions.Add(action);
            _actionSelect.AddItem(action.CommandName);
        }

        if (_actionOptions.Count > 0)
        {
            _actionSelect.Selected = 0;
        }

        RefreshWindowSelector();
    }

    private IEnumerable<ActionData> EnumerateActionCommands(ActionManager manager)
    {
        if (manager == null) yield break;

        if (manager.RootPages != null)
        {
            foreach (var page in manager.RootPages)
            {
                if (page == null) continue;
                foreach (var action in FlattenCommands(page.SubCommands))
                {
                    yield return action;
                }
            }
        }

        if (manager.LearnedActions == null) yield break;
        foreach (var action in manager.LearnedActions.OfType<ActionData>())
        {
            yield return action;
        }
    }

    private static IEnumerable<ActionData> FlattenCommands(IEnumerable<BattleCommand> commands)
    {
        if (commands == null) yield break;
        foreach (var command in commands)
        {
            if (command == null) continue;
            if (command is ActionData action)
            {
                yield return action;
                continue;
            }

            if (command is not BattleCategory category) continue;
            foreach (var nested in FlattenCommands(category.SubCommands))
            {
                yield return nested;
            }
        }
    }

    private ActionData GetSelectedAction()
    {
        if (_actionOptions.Count == 0 || _actionSelect == null) return null;
        int index = Mathf.Clamp(_actionSelect.Selected, 0, _actionOptions.Count - 1);
        return _actionOptions[index];
    }

    private int GetSelectedWindowIndex()
    {
        if (_windowSelect == null || _windowSelect.ItemCount == 0) return -1;
        int selected = _windowSelect.Selected;
        if (selected < 0) return -1;
        return _windowSelect.GetItemId(selected);
    }

    private void RefreshWindowSelector()
    {
        if (_windowSelect == null) return;

        _windowSelect.Clear();
        var action = GetSelectedAction();
        if (action?.TimedHitSettings == null || action.TimedHitSettings.Count == 0)
        {
            _windowSelect.AddItem("(No Timed Hits)", -1);
            _windowSelect.Selected = 0;
            return;
        }

        for (int i = 0; i < action.TimedHitSettings.Count; i++)
        {
            var settings = action.TimedHitSettings[i];
            string label = BuildWindowLabel(settings, i);
            _windowSelect.AddItem(label, i);
        }

        _windowSelect.Selected = 0;
    }

    private static string BuildWindowLabel(TimedHitSettings settings, int index)
    {
        string name = settings != null && !string.IsNullOrEmpty(settings.WindowLabel)
            ? settings.WindowLabel
            : $"Window {index + 1}";
        string mode = settings?.OffsetMode == TimedHitOffsetMode.NormalizedExecutionAnimation ? "norm" : "sec";
        return $"{name} [{mode}]";
    }

    private async void RunSelectedActionAsync()
    {
        if (_runInProgress)
        {
            Log("Action already running.");
            return;
        }

        var actor = _actor;
        var target = _target;
        var action = GetSelectedAction();
        if (actor == null || target == null || action == null || _actionDirector == null)
        {
            Log("Run failed: missing actor/target/action/system.");
            return;
        }

        var runtimeAction = action.Duplicate(true) as ActionData;
        if (runtimeAction == null)
        {
            Log("Run failed: could not clone selected action.");
            return;
        }

        ApplyVisualTimingOverrides(runtimeAction);

        var context = new ActionContext(runtimeAction, actor, new[] { target });
        context.TimedHitActionOffsetSeconds = (float)(_actionOffsetSpin?.Value ?? 0.0);
        context.TimedHitCharacterOffsetSeconds = (float)(_characterOffsetSpin?.Value ?? 0.0);

        int selectedWindow = GetSelectedWindowIndex();
        if (selectedWindow >= 0)
        {
            context.TimedHitWindowOffsetSeconds[selectedWindow] = (float)(_windowOffsetSpin?.Value ?? 0.0);
        }

        _actionDirector.Initialize(new[] { actor, target });

        _runInProgress = true;
        _runCounter++;
        _activeContext = context;
        RefreshSummary();
        Log($"Run {_runCounter}: {action.CommandName} started. Press confirm for timed hit.");

        try
        {
            await _actionDirector.ProcessAction(context);
            Log($"Run {_runCounter}: action resolved.");
        }
        catch (Exception ex)
        {
            Log($"Run {_runCounter}: action error: {ex.Message}");
        }
        finally
        {
            _runInProgress = false;
            _activeContext = null;
            RefreshSummary();
        }
    }

    private void ApplyVisualTimingOverrides(ActionData runtimeAction)
    {
        if (runtimeAction?.VisualSettings == null) return;

        runtimeAction.VisualSettings.TravelDelay = Mathf.Max(0f,
            runtimeAction.VisualSettings.TravelDelay + (float)(_travelDelayOffsetSpin?.Value ?? 0.0));

        runtimeAction.VisualSettings.TravelDuration = Mathf.Max(0f,
            runtimeAction.VisualSettings.TravelDuration + (float)(_travelDurationOffsetSpin?.Value ?? 0.0));

        runtimeAction.VisualSettings.ImpactVfxPrewarm = Mathf.Max(0f,
            runtimeAction.VisualSettings.ImpactVfxPrewarm + (float)(_impactPrewarmOffsetSpin?.Value ?? 0.0));
    }

    private void OnTimedHitResolvedDetailed(TimedHitRating rating, ActionContext context, TimedHitSettings settings, float signedOffsetSeconds, float absoluteOffsetSeconds, int windowIndex)
    {
        if (context == null || context != _activeContext) return;

        var timingBand = TimedHitFeedback.Classify(rating, signedOffsetSeconds);
        string timing = TimedHitFeedback.GetLabel(timingBand).ToUpperInvariant();
        float signedMs = signedOffsetSeconds * 1000f;
        float absMs = absoluteOffsetSeconds * 1000f;
        string label = BuildWindowLabel(settings, windowIndex >= 0 ? windowIndex : 0);

        if (_timingLabel != null)
        {
            _timingLabel.Text =
                $"Last Timed Hit: {rating} | {timing} {signedMs:0.0} ms | |error| {absMs:0.0} ms | window={windowIndex + 1} ({label})";
        }

        Log($"TimedHit: rating={rating}, window={windowIndex}, signedMs={signedMs:0.0}, absMs={absMs:0.0}");
    }

    private void CommitWindowOffsetToAction()
    {
        var action = GetSelectedAction();
        int windowIndex = GetSelectedWindowIndex();
        if (action == null || windowIndex < 0 || action.TimedHitSettings == null || windowIndex >= action.TimedHitSettings.Count)
        {
            Log("Commit window offset failed: select valid action/window.");
            return;
        }

        var settings = action.TimedHitSettings[windowIndex];
        if (settings == null)
        {
            Log("Commit window offset failed: missing settings.");
            return;
        }

        float delta = (float)(_actionOffsetSpin?.Value ?? 0.0) + (float)(_windowOffsetSpin?.Value ?? 0.0);
        if (Mathf.IsZeroApprox(delta))
        {
            Log("Commit window offset skipped: delta is 0.");
            return;
        }

        if (settings.OffsetMode != TimedHitOffsetMode.Seconds)
        {
            Log("Commit window offset only supports OffsetMode=Seconds currently.");
            return;
        }

        settings.TimingOffset = Mathf.Max(0f, settings.TimingOffset + delta);
        var result = ResourceSaver.Save(action);
        if (result == Error.Ok)
        {
            Log($"Committed window offset delta {delta:0.###}s to '{action.CommandName}' window {windowIndex + 1}.");
            RefreshWindowSelector();
            return;
        }

        Log($"Commit window offset failed: {result}");
    }

    private void CommitVisualTimingToAction()
    {
        var action = GetSelectedAction();
        if (action?.VisualSettings == null)
        {
            Log("Commit visual timing failed: selected action has no VisualSettings.");
            return;
        }

        float delayDelta = (float)(_travelDelayOffsetSpin?.Value ?? 0.0);
        float durationDelta = (float)(_travelDurationOffsetSpin?.Value ?? 0.0);
        float prewarmDelta = (float)(_impactPrewarmOffsetSpin?.Value ?? 0.0);

        if (Mathf.IsZeroApprox(delayDelta) && Mathf.IsZeroApprox(durationDelta) && Mathf.IsZeroApprox(prewarmDelta))
        {
            Log("Commit visual timing skipped: all deltas are 0.");
            return;
        }

        action.VisualSettings.TravelDelay = Mathf.Max(0f, action.VisualSettings.TravelDelay + delayDelta);
        action.VisualSettings.TravelDuration = Mathf.Max(0f, action.VisualSettings.TravelDuration + durationDelta);
        action.VisualSettings.ImpactVfxPrewarm = Mathf.Max(0f, action.VisualSettings.ImpactVfxPrewarm + prewarmDelta);

        var result = ResourceSaver.Save(action);
        if (result == Error.Ok)
        {
            Log($"Committed visual timing to '{action.CommandName}' (travelDelay {delayDelta:0.###}, travelDuration {durationDelta:0.###}, prewarm {prewarmDelta:0.###}).");
            return;
        }

        Log($"Commit visual timing failed: {result}");
    }

    private void ResetOffsets()
    {
        if (_actionOffsetSpin != null) _actionOffsetSpin.Value = 0.0;
        if (_characterOffsetSpin != null) _characterOffsetSpin.Value = 0.0;
        if (_windowOffsetSpin != null) _windowOffsetSpin.Value = 0.0;
        if (_travelDelayOffsetSpin != null) _travelDelayOffsetSpin.Value = 0.0;
        if (_travelDurationOffsetSpin != null) _travelDurationOffsetSpin.Value = 0.0;
        if (_impactPrewarmOffsetSpin != null) _impactPrewarmOffsetSpin.Value = 0.0;
        Log("Reset all runtime tuning offsets to 0.");
    }

    private void RefreshSummary()
    {
        string actorName = _actor?.Name ?? "(none)";
        string targetName = _target?.Name ?? "(none)";
        string actionName = GetSelectedAction()?.CommandName ?? "(none)";

        if (_summaryLabel != null)
        {
            _summaryLabel.Text =
                $"Actor: {actorName} | Target: {targetName}\nAction: {actionName}\nRunning: {(_runInProgress ? "Yes" : "No")} | Run#: {_runCounter}";
        }
    }

    private void Log(string message)
    {
        if (_logLabel == null) return;
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        _logLabel.Text += line;
        _logLabel.ScrollToLine(_logLabel.GetLineCount());
    }
}
