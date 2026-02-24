using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Sandbox scene controller for rapidly testing statuses, abilities, and combat interactions
/// without launching a full battle flow.
/// </summary>
public partial class StatusSandboxController : Control
{
    [ExportGroup("Resource Roots")]
    [Export]
    public string StatusResourceRoot { get; set; } = "res://assets/resources/status_effects";

    [Export]
    public string AbilityResourceRoot { get; set; } = "res://assets/resources/abilities";

    [ExportGroup("Characters")]
    [Export]
    public Godot.Collections.Array<PackedScene> CharacterSceneLibrary { get; set; } = new();

    [Export]
    public PackedScene DefaultActorScene { get; set; }

    [Export]
    public PackedScene DefaultTargetScene { get; set; }

    [ExportGroup("Fallback")]
    [Export]
    public BaseStats FallbackBaseStats { get; set; }

    [ExportGroup("UI Scaling")]
    [Export]
    public bool AutoScaleWindowContent { get; set; } = true;

    [Export(PropertyHint.Range, "0.8,2.0,0.01")]
    public float MinUiScale { get; set; } = 0.95f;

    [Export(PropertyHint.Range, "0.8,2.5,0.01")]
    public float MaxUiScale { get; set; } = 1.55f;

    [Export]
    public int BaseFontSize { get; set; } = 16;

    private Node3D _playerAnchor;
    private Node3D _enemyAnchor;
    private Node3D _allyAnchor;

    private HSplitContainer _mainSplit;
    private ScrollContainer _leftScroll;
    private VBoxContainer _leftPanel;

    private OptionButton _actorSceneSelect;
    private OptionButton _targetSceneSelect;
    private Button _respawnActorButton;
    private Button _respawnTargetButton;
    private Button _respawnBothButton;

    private OptionButton _statusSubjectSelect;
    private OptionButton _statusSelect;
    private Button _applyStatusButton;
    private Button _removeStatusButton;
    private Button _clearStatusesButton;
    private ItemList _actorStatusList;
    private ItemList _targetStatusList;

    private OptionButton _turnSubjectSelect;
    private Button _turnStartButton;
    private Button _turnEndButton;
    private Button _fullTurnButton;
    private Button _commitOverflowTurnButton;

    private OptionButton _statSubjectSelect;
    private SpinBox _setHpSpin;
    private SpinBox _setMpSpin;
    private Button _setHpButton;
    private Button _setMpButton;
    private SpinBox _statAmountSpin;
    private Button _damageButton;
    private Button _healButton;
    private Button _gainMpButton;
    private Button _spendMpButton;

    private OptionButton _actionAttackerSelect;
    private OptionButton _actionDefenderSelect;
    private SpinBox _actionPowerSpin;
    private SpinBox _actionCritSpin;
    private Button _simulatePhysicalButton;
    private Button _simulateMagicButton;
    private Button _simulateHealButton;
    private Button _refreshMenuButton;
    private Button _validateMenuActionButton;
    private Button _simulateMenuActionButton;
    private ItemList _menuActionList;
    private Label _menuValidationLabel;

    private OptionButton _abilitySubjectSelect;
    private OptionButton _abilitySelect;
    private Button _equipAbilityButton;
    private Button _unequipAbilityButton;
    private OptionButton _abilityTriggerSelect;
    private Button _triggerAbilityButton;

    private OptionButton _resourceSubjectSelect;
    private SpinBox _chargeAmountSpin;
    private Button _addChargeButton;
    private Button _spendChargeButton;
    private Button _setChargeButton;

    private OptionButton _overflowSideSelect;
    private SpinBox _overflowAmountSpin;
    private Button _addOverflowButton;
    private Button _spendOverflowButton;
    private Button _setOverflowButton;
    private Button _overflowPerfectHitButton;
    private Button _overflowPerfectGuardButton;
    private Button _overflowOverhealButton;
    private Button _overflowMpRestoreButton;

    private Label _actorStateLabel;
    private Label _targetStateLabel;
    private Label _resourceStateLabel;
    private Label _summaryLabel;
    private RichTextLabel _logLabel;

    private Node _actor;
    private Node _target;

    private readonly List<PackedScene> _characterOptions = new();
    private readonly List<StatusEffect> _statusOptions = new();
    private readonly List<Ability> _abilityOptions = new();
    private readonly List<AbilityTrigger> _abilityTriggers = new();
    private readonly List<ActionData> _menuActions = new();

    private ChargeSystem _chargeSystem;
    private OverflowSystem _overflowSystem;
    private CalculationStrategy _defaultCalculation;
    private readonly IRandomNumberGenerator _rng = new GodotRandomNumberGenerator();
    private Theme _responsiveTheme;
    private float _lastAppliedUiScale = -1f;
    private bool _windowScaleModeStored = false;
    private Window.ContentScaleModeEnum _previousContentScaleMode;
    private Window.ContentScaleAspectEnum _previousContentScaleAspect;

    // We do not add this node to tree. It is only used as a method host for status rules that expect BattleController.
    private BattleController _ruleBattleControllerProxy;

    private const string PlayerSceneFallback = "res://assets/resources/characters/ceira/ceira.tscn";
    private const string EnemySceneFallback = "res://assets/resources/characters/goblin_lv5/goblin_lv5_animated.tscn";
    private const string TemplateSceneFallback = "res://assets/resources/characters/CharacterTemplate.tscn";

    public override void _Ready()
    {
        _playerAnchor = GetNodeOrNull<Node3D>("SandboxWorld/PlayerAnchor");
        _enemyAnchor = GetNodeOrNull<Node3D>("SandboxWorld/EnemyAnchor");
        _allyAnchor = GetNodeOrNull<Node3D>("SandboxWorld/AllyAnchor");

        CacheUiNodes();
        EnsureDefaultCalculation();
        EnsureSystems();
        EnsureCharacterLibrary();
        ConfigureWindowContentScaling();
        ApplyResponsiveLayoutAndTheme();

        PopulateSubjectSelectors();
        PopulateSideSelector();
        PopulateTriggerSelector();
        PopulateCharacterSelectors();
        LoadStatusLibrary();
        LoadAbilityLibrary();

        WireUiEvents();

        RespawnBoth();
        RefreshAll();

        Log("Sandbox ready.");
    }

    public override void _ExitTree()
    {
        RestoreWindowContentScaling();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            ApplyResponsiveLayoutAndTheme();
        }
    }

    private void CacheUiNodes()
    {
        _mainSplit = GetNodeOrNull<HSplitContainer>("UI/MainSplit");
        _leftScroll = GetNodeOrNull<ScrollContainer>("UI/MainSplit/LeftScroll");
        _leftPanel = GetNodeOrNull<VBoxContainer>("UI/MainSplit/LeftScroll/LeftPanel");

        _actorSceneSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftScroll/LeftPanel/SetupPanel/Body/ActorRow/ActorSceneSelect");
        _targetSceneSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftScroll/LeftPanel/SetupPanel/Body/TargetRow/TargetSceneSelect");
        _respawnActorButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/SetupPanel/Body/ActorRow/RespawnActorButton");
        _respawnTargetButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/SetupPanel/Body/TargetRow/RespawnTargetButton");
        _respawnBothButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/SetupPanel/Body/RespawnBothButton");

        _statusSubjectSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftScroll/LeftPanel/StatusPanel/Body/StatusControlRow/StatusSubjectSelect");
        _statusSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftScroll/LeftPanel/StatusPanel/Body/StatusControlRow/StatusSelect");
        _applyStatusButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/StatusPanel/Body/StatusButtonRow/ApplyStatusButton");
        _removeStatusButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/StatusPanel/Body/StatusButtonRow/RemoveStatusButton");
        _clearStatusesButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/StatusPanel/Body/StatusButtonRow/ClearStatusesButton");
        _actorStatusList = GetNodeOrNull<ItemList>("UI/MainSplit/LeftScroll/LeftPanel/StatusPanel/Body/StatusListsRow/ActorStatusList");
        _targetStatusList = GetNodeOrNull<ItemList>("UI/MainSplit/LeftScroll/LeftPanel/StatusPanel/Body/StatusListsRow/TargetStatusList");

        _turnSubjectSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftScroll/LeftPanel/TurnPanel/Body/TurnControlRow/TurnSubjectSelect");
        _turnStartButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/TurnPanel/Body/TurnButtonRow/TurnStartButton");
        _turnEndButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/TurnPanel/Body/TurnButtonRow/TurnEndButton");
        _fullTurnButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/TurnPanel/Body/TurnButtonRow/FullTurnButton");
        _commitOverflowTurnButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/TurnPanel/Body/TurnButtonRow/CommitOverflowTurnButton");

        _statSubjectSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftScroll/LeftPanel/StatsPanel/Body/StatSubjectRow/StatSubjectSelect");
        _setHpSpin = GetNodeOrNull<SpinBox>("UI/MainSplit/LeftScroll/LeftPanel/StatsPanel/Body/StatSetRow/SetHpSpin");
        _setMpSpin = GetNodeOrNull<SpinBox>("UI/MainSplit/LeftScroll/LeftPanel/StatsPanel/Body/StatSetRow/SetMpSpin");
        _setHpButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/StatsPanel/Body/StatSetRow/SetHpButton");
        _setMpButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/StatsPanel/Body/StatSetRow/SetMpButton");
        _statAmountSpin = GetNodeOrNull<SpinBox>("UI/MainSplit/LeftScroll/LeftPanel/StatsPanel/Body/StatDeltaRow/StatAmountSpin");
        _damageButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/StatsPanel/Body/StatDeltaRow/DamageButton");
        _healButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/StatsPanel/Body/StatDeltaRow/HealButton");
        _gainMpButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/StatsPanel/Body/StatDeltaRow/GainMpButton");
        _spendMpButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/StatsPanel/Body/StatDeltaRow/SpendMpButton");

        _abilitySubjectSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftScroll/LeftPanel/AbilityPanel/Body/AbilityControlRow/AbilitySubjectSelect");
        _abilitySelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftScroll/LeftPanel/AbilityPanel/Body/AbilityControlRow/AbilitySelect");
        _equipAbilityButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/AbilityPanel/Body/AbilityButtonRow/EquipAbilityButton");
        _unequipAbilityButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/AbilityPanel/Body/AbilityButtonRow/UnequipAbilityButton");
        _abilityTriggerSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftScroll/LeftPanel/AbilityPanel/Body/AbilityTriggerRow/AbilityTriggerSelect");
        _triggerAbilityButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/AbilityPanel/Body/AbilityTriggerRow/TriggerAbilityButton");

        _resourceSubjectSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/ResourceSubjectRow/ResourceSubjectSelect");
        _chargeAmountSpin = GetNodeOrNull<SpinBox>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/ChargeRow/ChargeAmountSpin");
        _addChargeButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/ChargeRow/AddChargeButton");
        _spendChargeButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/ChargeRow/SpendChargeButton");
        _setChargeButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/ChargeRow/SetChargeButton");

        _overflowSideSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/OverflowRow/OverflowSideSelect");
        _overflowAmountSpin = GetNodeOrNull<SpinBox>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/OverflowRow/OverflowAmountSpin");
        _addOverflowButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/OverflowRow/AddOverflowButton");
        _spendOverflowButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/OverflowRow/SpendOverflowButton");
        _setOverflowButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/OverflowRow/SetOverflowButton");

        _overflowPerfectHitButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/OverflowEventRow/OverflowPerfectHitButton");
        _overflowPerfectGuardButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/OverflowEventRow/OverflowPerfectGuardButton");
        _overflowOverhealButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/OverflowEventRow/OverflowOverhealButton");
        _overflowMpRestoreButton = GetNodeOrNull<Button>("UI/MainSplit/LeftScroll/LeftPanel/ResourcesPanel/Body/OverflowEventRow/OverflowMpRestoreButton");

        _actionAttackerSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/RightPanel/ActionPanel/Body/ActionUnitRow/ActionAttackerSelect");
        _actionDefenderSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/RightPanel/ActionPanel/Body/ActionUnitRow/ActionDefenderSelect");
        _actionPowerSpin = GetNodeOrNull<SpinBox>("UI/MainSplit/RightPanel/ActionPanel/Body/ActionValueRow/ActionPowerSpin");
        _actionCritSpin = GetNodeOrNull<SpinBox>("UI/MainSplit/RightPanel/ActionPanel/Body/ActionValueRow/ActionCritSpin");
        _simulatePhysicalButton = GetNodeOrNull<Button>("UI/MainSplit/RightPanel/ActionPanel/Body/ActionButtonRow/SimulatePhysicalButton");
        _simulateMagicButton = GetNodeOrNull<Button>("UI/MainSplit/RightPanel/ActionPanel/Body/ActionButtonRow/SimulateMagicButton");
        _simulateHealButton = GetNodeOrNull<Button>("UI/MainSplit/RightPanel/ActionPanel/Body/ActionButtonRow/SimulateHealButton");
        _refreshMenuButton = GetNodeOrNull<Button>("UI/MainSplit/RightPanel/ActionPanel/Body/MenuButtonRow/RefreshMenuButton");
        _validateMenuActionButton = GetNodeOrNull<Button>("UI/MainSplit/RightPanel/ActionPanel/Body/MenuButtonRow/ValidateMenuActionButton");
        _simulateMenuActionButton = GetNodeOrNull<Button>("UI/MainSplit/RightPanel/ActionPanel/Body/MenuButtonRow/SimulateMenuActionButton");
        _menuActionList = GetNodeOrNull<ItemList>("UI/MainSplit/RightPanel/ActionPanel/Body/MenuActionList");
        _menuValidationLabel = GetNodeOrNull<Label>("UI/MainSplit/RightPanel/ActionPanel/Body/MenuValidationLabel");

        _actorStateLabel = GetNodeOrNull<Label>("UI/MainSplit/RightPanel/StatePanel/Body/ActorStateLabel");
        _targetStateLabel = GetNodeOrNull<Label>("UI/MainSplit/RightPanel/StatePanel/Body/TargetStateLabel");
        _resourceStateLabel = GetNodeOrNull<Label>("UI/MainSplit/RightPanel/StatePanel/Body/ResourceStateLabel");
        _summaryLabel = GetNodeOrNull<Label>("UI/MainSplit/LeftScroll/LeftPanel/SetupPanel/Body/SummaryLabel");
        _logLabel = GetNodeOrNull<RichTextLabel>("UI/MainSplit/RightPanel/LogPanel/Body/LogLabel");
    }

    private void ConfigureWindowContentScaling()
    {
        if (!AutoScaleWindowContent) return;

        var window = GetWindow();
        if (window == null) return;

        _previousContentScaleMode = window.ContentScaleMode;
        _previousContentScaleAspect = window.ContentScaleAspect;
        _windowScaleModeStored = true;

        window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
    }

    private void RestoreWindowContentScaling()
    {
        if (!AutoScaleWindowContent || !_windowScaleModeStored) return;

        var window = GetWindow();
        if (window == null) return;

        window.ContentScaleMode = _previousContentScaleMode;
        window.ContentScaleAspect = _previousContentScaleAspect;
    }

    private void ApplyResponsiveLayoutAndTheme()
    {
        float width = Mathf.Max(1f, Size.X);
        float height = Mathf.Max(1f, Size.Y);

        // Keep left tooling panel readable while allowing right panel to grow on large windows.
        if (_mainSplit != null)
        {
            int desiredLeftWidth = Mathf.RoundToInt(Mathf.Clamp(width * 0.44f, 500f, 880f));
            _mainSplit.SplitOffset = desiredLeftWidth;
        }

        if (_leftScroll != null)
        {
            _leftScroll.CustomMinimumSize = new Vector2(Mathf.Clamp(width * 0.34f, 460f, 760f), 0f);
        }

        if (_leftPanel != null)
        {
            float minHeight = Mathf.Max(920f, height - 40f);
            _leftPanel.CustomMinimumSize = new Vector2(0f, minHeight);
        }

        float scaleByWidth = width / 1600f;
        float scaleByHeight = height / 900f;
        float resolvedScale = Mathf.Clamp(Mathf.Min(scaleByWidth, scaleByHeight), MinUiScale, MaxUiScale);
        ApplyReadableTheme(resolvedScale);
    }

    private void ApplyReadableTheme(float uiScale)
    {
        if (_responsiveTheme == null)
        {
            _responsiveTheme = new Theme();
        }

        if (Mathf.IsEqualApprox(uiScale, _lastAppliedUiScale))
        {
            return;
        }

        _lastAppliedUiScale = uiScale;
        int bodySize = Mathf.Max(12, Mathf.RoundToInt(BaseFontSize * uiScale));
        int compactSize = Mathf.Max(11, Mathf.RoundToInt((BaseFontSize - 1) * uiScale));

        _responsiveTheme.SetFontSize("font_size", "Label", bodySize);
        _responsiveTheme.SetFontSize("font_size", "Button", bodySize);
        _responsiveTheme.SetFontSize("font_size", "OptionButton", bodySize);
        _responsiveTheme.SetFontSize("font_size", "SpinBox", bodySize);
        _responsiveTheme.SetFontSize("font_size", "LineEdit", bodySize);
        _responsiveTheme.SetFontSize("font_size", "ItemList", compactSize);
        _responsiveTheme.SetFontSize("normal_font_size", "RichTextLabel", compactSize);
        _responsiveTheme.DefaultBaseScale = uiScale;

        Theme = _responsiveTheme;
    }

    private void EnsureDefaultCalculation()
    {
        _defaultCalculation = new CalculationStrategy();
        _defaultCalculation.Set("HitLogic", new StandardHitStrategy());
        _defaultCalculation.Set("CritLogic", new StandardCritStrategy());
        _defaultCalculation.Set("DamageLogic", new StandardDamageStrategy());
    }

    private void EnsureSystems()
    {
        _chargeSystem = GetNodeOrNull<ChargeSystem>("ChargeSystem");
        if (_chargeSystem == null)
        {
            _chargeSystem = new ChargeSystem { Name = "ChargeSystem" };
            AddChild(_chargeSystem);
        }

        _overflowSystem = GetNodeOrNull<OverflowSystem>("OverflowSystem");
        if (_overflowSystem == null)
        {
            _overflowSystem = new OverflowSystem { Name = "OverflowSystem" };
            AddChild(_overflowSystem);
        }

        _chargeSystem.Initialize(null);
        _overflowSystem.Initialize(null, null);

        _chargeSystem.ChargesChanged += (_, _, _) => RefreshAll();
        _overflowSystem.OverflowChanged += (_, _, _, _, _) => RefreshAll();
    }

    private void PopulateSubjectSelectors()
    {
        FillSubjectSelector(_statusSubjectSelect);
        FillSubjectSelector(_turnSubjectSelect);
        FillSubjectSelector(_statSubjectSelect);
        FillSubjectSelector(_abilitySubjectSelect);
        FillSubjectSelector(_resourceSubjectSelect);

        FillUnitSelector(_actionAttackerSelect);
        FillUnitSelector(_actionDefenderSelect);
        if (_actionDefenderSelect != null)
        {
            _actionDefenderSelect.Selected = 1;
        }
    }

    private static void FillSubjectSelector(OptionButton option)
    {
        if (option == null) return;
        option.Clear();
        option.AddItem("Actor", 0);
        option.AddItem("Target", 1);
    }

    private static void FillUnitSelector(OptionButton option)
    {
        if (option == null) return;
        option.Clear();
        option.AddItem("Actor", 0);
        option.AddItem("Target", 1);
    }

    private void PopulateSideSelector()
    {
        if (_overflowSideSelect == null) return;
        _overflowSideSelect.Clear();
        _overflowSideSelect.AddItem("Player", 0);
        _overflowSideSelect.AddItem("Enemy", 1);
    }

    private void PopulateTriggerSelector()
    {
        if (_abilityTriggerSelect == null) return;

        _abilityTriggers.Clear();
        _abilityTriggerSelect.Clear();

        foreach (AbilityTrigger trigger in Enum.GetValues(typeof(AbilityTrigger)))
        {
            if (trigger == AbilityTrigger.None) continue;
            _abilityTriggers.Add(trigger);
            _abilityTriggerSelect.AddItem(trigger.ToString());
        }
    }

    private void EnsureCharacterLibrary()
    {
        _characterOptions.Clear();

        if (CharacterSceneLibrary != null)
        {
            foreach (var packed in CharacterSceneLibrary)
            {
                if (packed != null) _characterOptions.Add(packed);
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

        if (DefaultActorScene == null && _characterOptions.Count > 0)
        {
            DefaultActorScene = _characterOptions[0];
        }

        if (DefaultTargetScene == null)
        {
            DefaultTargetScene = _characterOptions.Count > 1 ? _characterOptions[1] : _characterOptions.FirstOrDefault();
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
            var packed = _characterOptions[i];
            string name = GetSceneDisplayName(packed);
            option.AddItem(name, i);
            if (packed == preferred)
            {
                selected = i;
            }
        }

        if (_characterOptions.Count > 0)
        {
            option.Selected = Mathf.Clamp(selected, 0, _characterOptions.Count - 1);
        }
    }

    private static string GetSceneDisplayName(PackedScene packed)
    {
        if (packed == null) return "None";
        string path = packed.ResourcePath;
        if (string.IsNullOrEmpty(path)) return packed.ResourceName;
        return path.GetFile().GetBaseName();
    }

    private void LoadStatusLibrary()
    {
        _statusOptions.Clear();
        LoadResourcesRecursive(StatusResourceRoot, resource =>
        {
            if (resource is not StatusEffect effect) return;
            _statusOptions.Add(effect);
        });

        _statusOptions.Sort((a, b) => string.Compare(a?.EffectName, b?.EffectName, StringComparison.OrdinalIgnoreCase));

        if (_statusSelect != null)
        {
            _statusSelect.Clear();
            foreach (var effect in _statusOptions)
            {
                _statusSelect.AddItem(effect?.EffectName ?? "(unnamed)");
            }
        }
    }

    private void LoadAbilityLibrary()
    {
        _abilityOptions.Clear();
        LoadResourcesRecursive(AbilityResourceRoot, resource =>
        {
            if (resource is not Ability ability) return;
            _abilityOptions.Add(ability);
        });

        _abilityOptions.Sort((a, b) => string.Compare(a?.AbilityName, b?.AbilityName, StringComparison.OrdinalIgnoreCase));

        if (_abilitySelect != null)
        {
            _abilitySelect.Clear();
            foreach (var ability in _abilityOptions)
            {
                _abilitySelect.AddItem(ability?.AbilityName ?? "(unnamed)");
            }
        }
    }

    private static void LoadResourcesRecursive(string root, Action<Resource> visitor)
    {
        if (string.IsNullOrEmpty(root) || visitor == null) return;
        VisitDirectory(root, visitor);
    }

    private static void VisitDirectory(string path, Action<Resource> visitor)
    {
        using var dir = DirAccess.Open(path);
        if (dir == null)
        {
            return;
        }

        dir.ListDirBegin();
        while (true)
        {
            string entry = dir.GetNext();
            if (string.IsNullOrEmpty(entry)) break;
            if (entry == "." || entry == "..") continue;

            string fullPath = path.PathJoin(entry);
            if (dir.CurrentIsDir())
            {
                VisitDirectory(fullPath, visitor);
                continue;
            }

            if (!entry.EndsWith(".tres", StringComparison.OrdinalIgnoreCase) &&
                !entry.EndsWith(".res", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var resource = ResourceLoader.Load(fullPath);
            if (resource != null)
            {
                visitor(resource);
            }
        }

        dir.ListDirEnd();
    }

    private void WireUiEvents()
    {
        _respawnActorButton?.Connect(Button.SignalName.Pressed, Callable.From(RespawnActor));
        _respawnTargetButton?.Connect(Button.SignalName.Pressed, Callable.From(RespawnTarget));
        _respawnBothButton?.Connect(Button.SignalName.Pressed, Callable.From(RespawnBoth));

        _applyStatusButton?.Connect(Button.SignalName.Pressed, Callable.From(ApplySelectedStatus));
        _removeStatusButton?.Connect(Button.SignalName.Pressed, Callable.From(RemoveSelectedStatus));
        _clearStatusesButton?.Connect(Button.SignalName.Pressed, Callable.From(ClearSubjectStatuses));

        _turnStartButton?.Connect(Button.SignalName.Pressed, Callable.From(SimulateTurnStart));
        _turnEndButton?.Connect(Button.SignalName.Pressed, Callable.From(SimulateTurnEnd));
        _fullTurnButton?.Connect(Button.SignalName.Pressed, Callable.From(SimulateFullTurn));
        _commitOverflowTurnButton?.Connect(Button.SignalName.Pressed, Callable.From(CommitOverflowTurn));

        _setHpButton?.Connect(Button.SignalName.Pressed, Callable.From(SetSubjectHp));
        _setMpButton?.Connect(Button.SignalName.Pressed, Callable.From(SetSubjectMp));
        _damageButton?.Connect(Button.SignalName.Pressed, Callable.From(ApplySubjectDamage));
        _healButton?.Connect(Button.SignalName.Pressed, Callable.From(ApplySubjectHealing));
        _gainMpButton?.Connect(Button.SignalName.Pressed, Callable.From(ApplySubjectMpGain));
        _spendMpButton?.Connect(Button.SignalName.Pressed, Callable.From(ApplySubjectMpSpend));

        _simulatePhysicalButton?.Connect(Button.SignalName.Pressed, Callable.From(SimulatePhysicalAction));
        _simulateMagicButton?.Connect(Button.SignalName.Pressed, Callable.From(SimulateMagicAction));
        _simulateHealButton?.Connect(Button.SignalName.Pressed, Callable.From(SimulateHealAction));
        _refreshMenuButton?.Connect(Button.SignalName.Pressed, Callable.From(RefreshMenuActions));
        _validateMenuActionButton?.Connect(Button.SignalName.Pressed, Callable.From(ValidateSelectedMenuAction));
        _simulateMenuActionButton?.Connect(Button.SignalName.Pressed, Callable.From(SimulateSelectedMenuAction));
        if (_actionAttackerSelect != null)
        {
            _actionAttackerSelect.ItemSelected += _ => RefreshMenuActions();
        }

        _equipAbilityButton?.Connect(Button.SignalName.Pressed, Callable.From(EquipSelectedAbility));
        _unequipAbilityButton?.Connect(Button.SignalName.Pressed, Callable.From(UnequipSelectedAbility));
        _triggerAbilityButton?.Connect(Button.SignalName.Pressed, Callable.From(TriggerSelectedAbility));

        _addChargeButton?.Connect(Button.SignalName.Pressed, Callable.From(AddSubjectCharge));
        _spendChargeButton?.Connect(Button.SignalName.Pressed, Callable.From(SpendSubjectCharge));
        _setChargeButton?.Connect(Button.SignalName.Pressed, Callable.From(SetSubjectCharge));

        _addOverflowButton?.Connect(Button.SignalName.Pressed, Callable.From(AddOverflow));
        _spendOverflowButton?.Connect(Button.SignalName.Pressed, Callable.From(SpendOverflow));
        _setOverflowButton?.Connect(Button.SignalName.Pressed, Callable.From(SetOverflow));
        _overflowPerfectHitButton?.Connect(Button.SignalName.Pressed, Callable.From(SendOverflowPerfectHit));
        _overflowPerfectGuardButton?.Connect(Button.SignalName.Pressed, Callable.From(SendOverflowPerfectGuard));
        _overflowOverhealButton?.Connect(Button.SignalName.Pressed, Callable.From(SendOverflowOverheal));
        _overflowMpRestoreButton?.Connect(Button.SignalName.Pressed, Callable.From(SendOverflowMpRestore));
    }

    private void RespawnBoth()
    {
        RespawnActor();
        RespawnTarget();
        ResetBattleSandboxState();
        RefreshAll();
    }

    private void RespawnActor()
    {
        var scene = GetSelectedCharacterScene(_actorSceneSelect) ?? DefaultActorScene;
        _actor = SpawnCombatant(_actor, scene, _playerAnchor, true, "Actor");
        RefreshMenuActions();
    }

    private void RespawnTarget()
    {
        var scene = GetSelectedCharacterScene(_targetSceneSelect) ?? DefaultTargetScene;
        _target = SpawnCombatant(_target, scene, _enemyAnchor, false, "Target");
        RefreshMenuActions();
    }

    private Node SpawnCombatant(Node existing, PackedScene scene, Node3D anchor, bool isPlayerSide, string fallbackName)
    {
        if (existing != null && GodotObject.IsInstanceValid(existing))
        {
            existing.QueueFree();
        }

        if (anchor == null)
        {
            Log("Spawn failed: missing anchor.");
            return null;
        }

        Node combatant = scene?.Instantiate();
        if (combatant == null)
        {
            combatant = CreateFallbackCombatant(fallbackName);
        }

        combatant.Name = fallbackName;
        anchor.AddChild(combatant);

        if (isPlayerSide)
        {
            combatant.AddToGroup(GameGroups.PlayerCharacters);
        }
        else
        {
            combatant.RemoveFromGroup(GameGroups.PlayerCharacters);
        }

        EnsureCombatantComponents(combatant);
        PositionCombatant(combatant, isPlayerSide);

        return combatant;
    }

    private static void PositionCombatant(Node combatant, bool isPlayerSide)
    {
        if (combatant is not Node3D node3D) return;
        node3D.Position = isPlayerSide ? new Vector3(-2.5f, 0f, 0f) : new Vector3(2.5f, 0f, 0f);
        node3D.Rotation = isPlayerSide ? Vector3.Zero : new Vector3(0f, Mathf.Pi, 0f);
    }

    private Node CreateFallbackCombatant(string name)
    {
        var character = new BaseCharacter { Name = name };
        var stats = new StatsComponent { Name = StatsComponent.NodeName };
        stats.SetBaseStatsResource(ResolveFallbackStats());
        character.AddChild(stats);
        character.AddChild(new StatusEffectManager { Name = StatusEffectManager.NodeName });
        character.AddChild(new AbilityManager { Name = AbilityManager.NodeName });
        character.AddChild(CreateDefaultActionManager());
        return character;
    }

    private BaseStats ResolveFallbackStats()
    {
        return FallbackBaseStats ?? new BaseStats
        {
            HP = 350,
            MP = 120,
            Strength = 24,
            Defense = 20,
            Magic = 22,
            MagicDefense = 18,
            Speed = 16,
            Evasion = 6,
            MgEvasion = 6,
            Accuracy = 12,
            MgAccuracy = 12,
            Luck = 8,
            AP = 25
        };
    }

    private ActionManager CreateDefaultActionManager()
    {
        var manager = new ActionManager { Name = ActionManager.DefaultName };

        var page = new BattleCategory
        {
            CommandName = "Main",
            SubCommands = new Godot.Collections.Array<BattleCommand>
            {
                BuildActionTemplate("Attack", ActionCategory.Attack, 1.0f, 20, 95, 5, ActionFlags.None),
                BuildActionTemplate("Arcane Bolt", ActionCategory.Magic, 0.0f, 22, 95, 5, ActionFlags.None),
                BuildActionTemplate("Cure", ActionCategory.Heal, 0.0f, 30, 100, 0, ActionFlags.FixedDamage)
            }
        };

        manager.RootPages = new Godot.Collections.Array<BattleCategory> { page };
        return manager;
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

    private void ResetBattleSandboxState()
    {
        _chargeSystem.Initialize(null);
        _overflowSystem.Initialize(null, null);
        _overflowSystem.ResetForBattle(GetAllCombatants());

        ConfigureRuleBattleControllerProxy();

        var actorStats = GetStats(_actor);
        if (actorStats != null)
        {
            _setHpSpin.Value = actorStats.CurrentHP;
            _setMpSpin.Value = actorStats.CurrentMP;
        }
    }

    private void ConfigureRuleBattleControllerProxy()
    {
        _ruleBattleControllerProxy ??= new BattleController();

        try
        {
            _ruleBattleControllerProxy.Set("_playerTeamContainer", _playerAnchor);
            _ruleBattleControllerProxy.Set("_enemyTeamContainer", _enemyAnchor);
            _ruleBattleControllerProxy.Set("_allyTeamContainer", _allyAnchor);
        }
        catch (Exception ex)
        {
            Log($"Rule proxy wiring warning: {ex.Message}");
        }
    }

    private void RefreshAll()
    {
        RefreshStatusLists();
        RefreshMenuActions();
        RefreshStateLabels();
    }

    private void RefreshStatusLists()
    {
        if (_actorStatusList != null)
        {
            PopulateStatusList(_actorStatusList, GetStatusManager(_actor), "Actor: ");
        }

        if (_targetStatusList != null)
        {
            PopulateStatusList(_targetStatusList, GetStatusManager(_target), "Target: ");
        }
    }

    private static void PopulateStatusList(ItemList list, StatusEffectManager manager, string prefix)
    {
        list.Clear();

        if (manager == null)
        {
            list.AddItem(prefix + "(No StatusEffectManager)");
            return;
        }

        var active = manager.GetActiveEffects();
        if (active == null || active.Count == 0)
        {
            list.AddItem(prefix + "(No active statuses)");
            return;
        }

        foreach (var instance in active)
        {
            if (instance?.EffectData == null) continue;
            string item = $"{instance.EffectData.EffectName} | turns={instance.RemainingTurns} | stacks={instance.Stacks}";
            list.AddItem(item);
        }
    }

    private void RefreshStateLabels()
    {
        if (_actorStateLabel != null)
        {
            _actorStateLabel.Text = BuildCombatantStateText("Actor", _actor);
        }

        if (_targetStateLabel != null)
        {
            _targetStateLabel.Text = BuildCombatantStateText("Target", _target);
        }

        if (_resourceStateLabel != null)
        {
            int actorCharge = _actor != null ? _chargeSystem.GetCharges(_actor) : 0;
            int targetCharge = _target != null ? _chargeSystem.GetCharges(_target) : 0;
            int playerOverflow = _overflowSystem.GetOverflowForSide(OverflowPartySide.Player);
            int playerCap = _overflowSystem.GetOverflowCapForSide(OverflowPartySide.Player);
            int enemyOverflow = _overflowSystem.GetOverflowForSide(OverflowPartySide.Enemy);
            int enemyCap = _overflowSystem.GetOverflowCapForSide(OverflowPartySide.Enemy);

            _resourceStateLabel.Text =
                $"Charges: Actor={actorCharge}, Target={targetCharge}\n" +
                $"Overflow(Player): {playerOverflow}/{playerCap}\n" +
                $"Overflow(Enemy): {enemyOverflow}/{enemyCap}";
        }

        if (_summaryLabel != null)
        {
            _summaryLabel.Text = $"Actor: {SafeName(_actor)} | Target: {SafeName(_target)}";
        }
    }

    private string BuildCombatantStateText(string label, Node combatant)
    {
        if (combatant == null || !GodotObject.IsInstanceValid(combatant)) return $"{label}: (none)";

        var stats = GetStats(combatant);
        var status = GetStatusManager(combatant);
        var ability = GetAbilityManager(combatant);

        int hp = stats?.CurrentHP ?? 0;
        int hpMax = stats?.GetStatValue(StatType.HP) ?? 0;
        int mp = stats?.CurrentMP ?? 0;
        int mpMax = stats?.GetStatValue(StatType.MP) ?? 0;

        string statusText = "None";
        if (status != null)
        {
            var names = status.GetActiveEffects()
                .Where(e => e?.EffectData != null)
                .Select(e => e.EffectData.EffectName)
                .ToArray();
            statusText = names.Length > 0 ? string.Join(", ", names) : "None";
        }

        string abilityText = "None";
        if (ability != null)
        {
            var equipped = ability.GetEquippedAbilities().Where(a => a != null).Select(a => a.AbilityName).ToArray();
            abilityText = equipped.Length > 0 ? string.Join(", ", equipped) : "None";
        }

        return $"{label}: {combatant.Name}\nHP {hp}/{hpMax} | MP {mp}/{mpMax}\nStatuses: {statusText}\nEquipped Abilities: {abilityText}";
    }

    private void ApplySelectedStatus()
    {
        var subject = GetSelectedSubject(_statusSubjectSelect);
        var combatant = ResolveSubject(subject);
        var manager = GetStatusManager(combatant);
        var effect = GetSelectedStatusEffect();

        if (manager == null || effect == null)
        {
            Log("Apply status failed: missing subject manager or status selection.");
            return;
        }

        bool applied = manager.TryApplyEffect(effect, null, 100f, _rng);
        Log(applied
            ? $"Applied status '{effect.EffectName}' to {SafeName(combatant)}."
            : $"Status '{effect.EffectName}' did not apply to {SafeName(combatant)}.");

        RefreshAll();
    }

    private void RemoveSelectedStatus()
    {
        var subject = GetSelectedSubject(_statusSubjectSelect);
        var combatant = ResolveSubject(subject);
        var manager = GetStatusManager(combatant);
        var effect = GetSelectedStatusEffect();

        if (manager == null || effect == null)
        {
            Log("Remove status failed: missing subject manager or status selection.");
            return;
        }

        bool removed = manager.RemoveEffect(effect, null);
        Log(removed
            ? $"Removed status '{effect.EffectName}' from {SafeName(combatant)}."
            : $"Status '{effect.EffectName}' not active on {SafeName(combatant)}.");

        RefreshAll();
    }

    private void ClearSubjectStatuses()
    {
        var subject = GetSelectedSubject(_statusSubjectSelect);
        var combatant = ResolveSubject(subject);
        var manager = GetStatusManager(combatant);

        if (manager == null)
        {
            Log("Clear statuses failed: missing StatusEffectManager.");
            return;
        }

        var snapshot = manager.GetActiveEffects().ToList();
        foreach (var instance in snapshot)
        {
            manager.RemoveEffect(instance, null);
        }

        Log($"Cleared all statuses from {SafeName(combatant)}.");
        RefreshAll();
    }

    private void SimulateTurnStart()
    {
        var subject = ResolveSubject(GetSelectedSubject(_turnSubjectSelect));
        var status = GetStatusManager(subject);
        status?.OnTurnStart(null);

        var ability = GetAbilityManager(subject);
        ability?.ApplyTrigger(AbilityTrigger.TurnStart, new AbilityEffectContext(subject, AbilityTrigger.TurnStart)
        {
            OverflowSystem = _overflowSystem
        });

        Log($"Turn start simulated for {SafeName(subject)}.");
        RefreshAll();
    }

    private void SimulateTurnEnd()
    {
        var subject = ResolveSubject(GetSelectedSubject(_turnSubjectSelect));
        var status = GetStatusManager(subject);
        status?.OnTurnEnd(null);

        var ability = GetAbilityManager(subject);
        ability?.ApplyTrigger(AbilityTrigger.TurnEnd, new AbilityEffectContext(subject, AbilityTrigger.TurnEnd)
        {
            OverflowSystem = _overflowSystem
        });

        Log($"Turn end simulated for {SafeName(subject)}.");
        RefreshAll();
    }

    private void SimulateFullTurn()
    {
        SimulateTurnStart();
        SimulateTurnEnd();
        CommitOverflowTurn();
    }

    private void CommitOverflowTurn()
    {
        var subject = ResolveSubject(GetSelectedSubject(_turnSubjectSelect));
        if (subject == null) return;

        _overflowSystem.NotifyTurnCommitted(subject);
        Log($"Overflow turn commit sent for {SafeName(subject)}.");
        RefreshAll();
    }

    private void SetSubjectHp()
    {
        var subject = ResolveSubject(GetSelectedSubject(_statSubjectSelect));
        var stats = GetStats(subject);
        if (stats == null) return;

        int desired = Mathf.Clamp((int)_setHpSpin.Value, 0, stats.GetStatValue(StatType.HP));
        int delta = desired - stats.CurrentHP;
        stats.ModifyCurrentHP(delta);

        Log($"Set HP for {SafeName(subject)} to {desired}.");
        RefreshAll();
    }

    private void SetSubjectMp()
    {
        var subject = ResolveSubject(GetSelectedSubject(_statSubjectSelect));
        var stats = GetStats(subject);
        if (stats == null) return;

        int desired = Mathf.Clamp((int)_setMpSpin.Value, 0, stats.GetStatValue(StatType.MP));
        int delta = desired - stats.CurrentMP;
        stats.ModifyCurrentMP(delta);

        Log($"Set MP for {SafeName(subject)} to {desired}.");
        RefreshAll();
    }

    private void ApplySubjectDamage()
    {
        var subject = ResolveSubject(GetSelectedSubject(_statSubjectSelect));
        var stats = GetStats(subject);
        if (stats == null) return;

        int amount = Mathf.Max(1, (int)_statAmountSpin.Value);
        stats.ModifyCurrentHP(-amount);
        Log($"Applied direct damage {amount} to {SafeName(subject)}.");
        RefreshAll();
    }

    private void ApplySubjectHealing()
    {
        var subject = ResolveSubject(GetSelectedSubject(_statSubjectSelect));
        var stats = GetStats(subject);
        if (stats == null) return;

        int amount = Mathf.Max(1, (int)_statAmountSpin.Value);
        stats.ModifyCurrentHP(amount);
        Log($"Applied direct healing {amount} to {SafeName(subject)}.");
        RefreshAll();
    }

    private void ApplySubjectMpGain()
    {
        var subject = ResolveSubject(GetSelectedSubject(_statSubjectSelect));
        var stats = GetStats(subject);
        var status = GetStatusManager(subject);
        if (stats == null) return;

        int baseAmount = Mathf.Max(1, (int)_statAmountSpin.Value);
        int adjusted = status?.ModifyIncomingMpRestore(baseAmount) ?? baseAmount;
        stats.ModifyCurrentMP(adjusted);

        if (adjusted > 0)
        {
            _overflowSystem.ReportMpRestored(subject, subject, adjusted);
        }

        Log($"Applied MP gain {baseAmount} (adjusted {adjusted}) to {SafeName(subject)}.");
        RefreshAll();
    }

    private void ApplySubjectMpSpend()
    {
        var subject = ResolveSubject(GetSelectedSubject(_statSubjectSelect));
        var stats = GetStats(subject);
        if (stats == null) return;

        int amount = Mathf.Max(1, (int)_statAmountSpin.Value);
        stats.ModifyCurrentMP(-amount);
        Log($"Spent MP {amount} from {SafeName(subject)}.");
        RefreshAll();
    }

    private void SimulatePhysicalAction()
    {
        var attacker = ResolveSelectedUnit(_actionAttackerSelect);
        var defender = ResolveSelectedUnit(_actionDefenderSelect);

        if (attacker == null || defender == null)
        {
            Log("Simulation failed: missing attacker or defender.");
            return;
        }

        var action = BuildActionTemplate(
            "Sandbox Physical",
            ActionCategory.Attack,
            1.0f,
            Mathf.Max(1, (int)_actionPowerSpin.Value),
            95,
            Mathf.Clamp((int)_actionCritSpin.Value, 0, 100),
            ActionFlags.None);

        SimulateAction(attacker, new List<Node> { defender }, action);
    }

    private void SimulateMagicAction()
    {
        var attacker = ResolveSelectedUnit(_actionAttackerSelect);
        var defender = ResolveSelectedUnit(_actionDefenderSelect);

        if (attacker == null || defender == null)
        {
            Log("Simulation failed: missing attacker or defender.");
            return;
        }

        var action = BuildActionTemplate(
            "Sandbox Magic",
            ActionCategory.Magic,
            0.0f,
            Mathf.Max(1, (int)_actionPowerSpin.Value),
            95,
            Mathf.Clamp((int)_actionCritSpin.Value, 0, 100),
            ActionFlags.None);

        SimulateAction(attacker, new List<Node> { defender }, action);
    }

    private void SimulateHealAction()
    {
        var attacker = ResolveSelectedUnit(_actionAttackerSelect);
        var defender = ResolveSelectedUnit(_actionDefenderSelect);

        if (attacker == null || defender == null)
        {
            Log("Simulation failed: missing caster or heal target.");
            return;
        }

        var action = BuildActionTemplate(
            "Sandbox Heal",
            ActionCategory.Heal,
            0.0f,
            Mathf.Max(1, (int)_actionPowerSpin.Value),
            100,
            0,
            ActionFlags.FixedDamage | ActionFlags.AlwaysHits);

        SimulateAction(attacker, new List<Node> { defender }, action);
    }

    private void RefreshMenuActions()
    {
        _menuActions.Clear();
        _menuActionList?.Clear();

        var attacker = ResolveSelectedUnit(_actionAttackerSelect);
        if (attacker == null || _menuActionList == null) return;

        var actionManager = GetActionManager(attacker);
        if (actionManager == null)
        {
            _menuActionList.AddItem("(No ActionManager)");
            return;
        }

        var unique = new HashSet<ActionData>();
        foreach (var action in GetActionCommands(actionManager))
        {
            if (action == null || !unique.Add(action)) continue;
            _menuActions.Add(action);

            bool allowed = IsActionAllowedForActorLocal(attacker, action, null, out var reason);
            string itemText = allowed ? $"{action.CommandName} [OK]" : $"{action.CommandName} [BLOCKED: {reason}]";
            _menuActionList.AddItem(itemText);
        }

        if (_menuActions.Count == 0)
        {
            _menuActionList.AddItem("(No actions)");
        }
    }

    private IEnumerable<ActionData> GetActionCommands(ActionManager manager)
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

        if (manager.LearnedActions != null)
        {
            foreach (var action in manager.LearnedActions.OfType<ActionData>())
            {
                yield return action;
            }
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
            }
            else if (command is BattleCategory category)
            {
                foreach (var nested in FlattenCommands(category.SubCommands))
                {
                    yield return nested;
                }
            }
        }
    }

    private void ValidateSelectedMenuAction()
    {
        var attacker = ResolveSelectedUnit(_actionAttackerSelect);
        var action = GetSelectedMenuAction();

        if (attacker == null || action == null)
        {
            SetMenuValidationText("Select a valid attacker and action.");
            return;
        }

        bool allowed = IsActionAllowedForActorLocal(attacker, action, null, out var reason);
        string text = allowed
            ? $"{action.CommandName}: allowed"
            : $"{action.CommandName}: blocked ({reason})";

        SetMenuValidationText(text);
    }

    private void SimulateSelectedMenuAction()
    {
        var attacker = ResolveSelectedUnit(_actionAttackerSelect);
        var defender = ResolveSelectedUnit(_actionDefenderSelect);
        var action = GetSelectedMenuAction();

        if (attacker == null || defender == null || action == null)
        {
            Log("Simulate menu action failed: select attacker, defender, and action.");
            return;
        }

        SimulateAction(attacker, new List<Node> { defender }, action);
    }

    private void SimulateAction(Node initiator, List<Node> initialTargets, ActionData action, int depth = 0)
    {
        if (initiator == null || action == null || initialTargets == null || initialTargets.Count == 0)
        {
            Log("Action simulation aborted: invalid initiator/action/targets.");
            return;
        }

        if (depth > 4)
        {
            Log("Action simulation aborted: reaction depth limit reached.");
            return;
        }

        if (!IsActionAllowedForActorLocal(initiator, action, null, out var rejection))
        {
            Log($"Action '{action.CommandName}' blocked for {SafeName(initiator)}: {rejection}");
            SetMenuValidationText(rejection);
            return;
        }

        var targets = RewriteTargetsFromStatusRulesLocal(initiator, action, null, initialTargets);
        if (_overflowSystem != null && !_overflowSystem.TrySpendForAction(initiator, action, out var overflowRejection))
        {
            Log($"Overflow rejected '{action.CommandName}': {overflowRejection}");
            SetMenuValidationText(overflowRejection);
            return;
        }

        var masterContext = new ActionContext(action, initiator, targets);

        ProcessInitiation(masterContext);
        ProcessGlobal(masterContext);

        var finalContexts = ProcessTargeting(masterContext, targets);
        CalculateAndApply(finalContexts);
        ProcessPostExecution(finalContexts);

        foreach (var reaction in masterContext.PendingReactions.ToList())
        {
            var reactionTargets = reaction.InitialTargets?.ToList() ?? new List<Node>();
            SimulateAction(reaction.Initiator, reactionTargets, reaction.SourceAction, depth + 1);
        }

        foreach (var context in finalContexts)
        {
            foreach (var reaction in context.PendingReactions.ToList())
            {
                var reactionTargets = reaction.InitialTargets?.ToList() ?? new List<Node>();
                SimulateAction(reaction.Initiator, reactionTargets, reaction.SourceAction, depth + 1);
            }
        }

        Log($"Simulated action '{action.CommandName}' from {SafeName(initiator)} to {string.Join(", ", targets.Select(SafeName))}.");
        RefreshAll();
    }

    private void ProcessInitiation(ActionContext context)
    {
        foreach (var modifier in GetOrderedModifiersFrom(context.Initiator))
        {
            modifier.OnActionInitiated(context, context.Initiator);
        }

        var allies = GetAlliesOf(context.Initiator);
        foreach (var entry in GetOrderedModifiersFromMany(allies))
        {
            entry.Modifier.OnAllyActionInitiated(context, context.Initiator, entry.Owner);
        }
    }

    private void ProcessGlobal(ActionContext context)
    {
        foreach (var entry in GetOrderedModifiersFromMany(GetAllCombatants()))
        {
            entry.Modifier.OnActionBroadcast(context, entry.Owner);
        }
    }

    private List<ActionContext> ProcessTargeting(ActionContext masterContext, List<Node> targets)
    {
        var finalContexts = new List<ActionContext>();

        foreach (var originalTarget in targets)
        {
            if (originalTarget == null || !GodotObject.IsInstanceValid(originalTarget)) continue;

            var context = new ActionContext(masterContext, originalTarget)
            {
                Stage = ActionStage.Targeting
            };

            var owners = new List<Node> { originalTarget };
            owners.AddRange(GetAlliesOf(originalTarget));

            foreach (var entry in GetOrderedModifiersFromMany(owners))
            {
                entry.Modifier.OnActionTargeted(context, entry.Owner);
            }

            finalContexts.Add(context);
        }

        return finalContexts;
    }

    private void CalculateAndApply(List<ActionContext> contexts)
    {
        foreach (var context in contexts)
        {
            var target = context.CurrentTarget;
            if (target == null || !GodotObject.IsInstanceValid(target)) continue;

            var result = context.GetResult(target);
            var strategy = context.SourceAction.CalculationStrategy ?? _defaultCalculation;

            result.IsHit = strategy?.CalculateHit(context, target, _rng) ?? true;
            if (!result.IsHit) continue;

            if (!context.SourceAction.Flags.HasFlag(ActionFlags.FixedDamage))
            {
                result.IsCritical = StatusRuleUtils.ShouldResolveDamage(context) && (strategy?.CalculateCrit(context, target, _rng) ?? false);
            }

            bool shouldResolveDamage = StatusRuleUtils.ShouldResolveDamage(context);
            result.FinalDamage = shouldResolveDamage ? (strategy?.CalculateDamage(context, target, result, _rng) ?? 0) : 0;
            if (shouldResolveDamage && context.SourceAction.Category == ActionCategory.Heal && result.FinalDamage > 0)
            {
                result.FinalDamage = -result.FinalDamage;
            }

            var targetStats = GetStats(target);
            if (!shouldResolveDamage || targetStats == null)
            {
                ApplyStatusesOnHit(context, result);
                continue;
            }

            int hpBefore = targetStats.CurrentHP;
            int maxHp = Mathf.Max(1, targetStats.GetStatValue(StatType.HP));
            targetStats.ModifyCurrentHP(-result.FinalDamage);

            if (result.IsHeal)
            {
                int requested = -result.FinalDamage;
                int effective = Mathf.Clamp(maxHp - hpBefore, 0, requested);
                int overheal = Mathf.Max(0, requested - effective);
                result.HealingAmount = effective;
                if (overheal > 0)
                {
                    _overflowSystem.ReportOverheal(context.Initiator, target, overheal);
                }
            }

            ApplyStatusesOnHit(context, result);
        }
    }

    private void ApplyStatusesOnHit(ActionContext context, ActionResult result)
    {
        if (context == null || result == null || !result.IsHit) return;

        var target = context.CurrentTarget;
        var statusManager = GetStatusManager(target);
        if (statusManager == null) return;

        var entries = new List<StatusEffectChanceEntry>();
        if (context.SourceAction?.StatusEffectsOnHit != null)
        {
            entries.AddRange(context.SourceAction.StatusEffectsOnHit);
        }

        if (context.ExtraStatusEffectsOnHit != null && context.ExtraStatusEffectsOnHit.Count > 0)
        {
            entries.AddRange(context.ExtraStatusEffectsOnHit);
        }

        foreach (var entry in entries)
        {
            if (entry == null || entry.Effect == null) continue;

            float chance = Mathf.Clamp(entry.ChancePercent, 0f, 100f);
            bool had = statusManager.HasEffect(entry.Effect);
            bool applied = statusManager.TryApplyEffect(entry.Effect, null, chance, _rng);
            if (applied && !had)
            {
                _overflowSystem.ReportUniqueDebuffApplied(context.Initiator, target, entry.Effect);
            }
        }
    }

    private void ProcessPostExecution(List<ActionContext> contexts)
    {
        foreach (var context in contexts)
        {
            var target = context.CurrentTarget;
            if (target == null || !GodotObject.IsInstanceValid(target)) continue;

            var stats = GetStats(target);
            if (stats != null && stats.CurrentHP <= 0) continue;

            var result = context.GetResult(target);
            foreach (var modifier in GetActionModifiersFrom(target))
            {
                modifier.OnActionPostExecution(context, target, result);
            }
        }
    }

    private readonly struct ModifierEntry
    {
        public ModifierEntry(IActionModifier modifier, Node owner)
        {
            Modifier = modifier;
            Owner = owner;
        }

        public IActionModifier Modifier { get; }
        public Node Owner { get; }
    }

    private IEnumerable<IActionModifier> GetActionModifiersFrom(Node owner)
    {
        if (owner == null) return Enumerable.Empty<IActionModifier>();

        var modifiers = new List<IActionModifier>();
        modifiers.AddRange(owner.FindChildren("*", recursive: true).OfType<IActionModifier>());

        var status = GetStatusManager(owner);
        if (status != null)
        {
            modifiers.AddRange(status.GetActionModifiers().OfType<IActionModifier>());
        }

        var ability = GetAbilityManager(owner);
        if (ability != null)
        {
            modifiers.AddRange(ability.GetActionModifiers());
        }

        return modifiers;
    }

    private IEnumerable<IActionModifier> GetOrderedModifiersFrom(Node owner)
    {
        return GetActionModifiersFrom(owner)
            .OrderByDescending(GetModifierPriority)
            .ToList();
    }

    private IEnumerable<ModifierEntry> GetOrderedModifiersFromMany(IEnumerable<Node> owners)
    {
        var entries = new List<ModifierEntry>();
        foreach (var owner in owners)
        {
            if (owner == null) continue;
            foreach (var modifier in GetActionModifiersFrom(owner))
            {
                entries.Add(new ModifierEntry(modifier, owner));
            }
        }

        return entries
            .OrderByDescending(entry => GetModifierPriority(entry.Modifier))
            .ToList();
    }

    private static int GetModifierPriority(IActionModifier modifier)
    {
        return modifier is IPrioritizedModifier prioritized ? prioritized.Priority : 0;
    }

    private IEnumerable<Node> GetAllCombatants()
    {
        if (_actor != null && GodotObject.IsInstanceValid(_actor)) yield return _actor;
        if (_target != null && GodotObject.IsInstanceValid(_target)) yield return _target;
    }

    private IEnumerable<Node> GetAlliesOf(Node owner)
    {
        bool ownerPlayer = owner?.IsInGroup(GameGroups.PlayerCharacters) ?? true;
        foreach (var candidate in GetAllCombatants())
        {
            if (candidate == owner) continue;
            bool candidatePlayer = candidate.IsInGroup(GameGroups.PlayerCharacters);
            if (candidatePlayer == ownerPlayer)
            {
                yield return candidate;
            }
        }
    }

    private bool IsActionAllowedForActorLocal(Node actor, ActionData action, ItemData sourceItem, out string reason)
    {
        reason = string.Empty;

        if (actor == null || action == null)
        {
            reason = "Invalid actor or action.";
            return false;
        }

        var manager = GetStatusManager(actor);
        if (manager != null)
        {
            foreach (var instance in manager.GetActiveEffects())
            {
                if (instance?.EffectData is not IStatusActionRule rule) continue;

                bool allowed = false;
                try
                {
                    allowed = rule.IsActionAllowed(action, sourceItem, actor, _ruleBattleControllerProxy, out reason);
                }
                catch (Exception ex)
                {
                    reason = $"Status rule error: {ex.Message}";
                    return false;
                }

                if (!allowed)
                {
                    if (string.IsNullOrEmpty(reason)) reason = "Blocked by status.";
                    return false;
                }
            }
        }

        if (_overflowSystem != null && !_overflowSystem.CanAffordAction(actor, action, out var overflowReason))
        {
            reason = overflowReason;
            return false;
        }

        return true;
    }

    private List<Node> RewriteTargetsFromStatusRulesLocal(Node actor, ActionData action, ItemData sourceItem, List<Node> currentTargets)
    {
        if (actor == null || action == null || currentTargets == null || currentTargets.Count == 0)
        {
            return currentTargets;
        }

        var manager = GetStatusManager(actor);
        if (manager == null) return currentTargets;

        foreach (var instance in manager.GetActiveEffects())
        {
            if (instance?.EffectData is not IStatusActionRule rule) continue;

            try
            {
                if (rule.TryRewriteTargets(action, sourceItem, actor, currentTargets, _ruleBattleControllerProxy, _rng, out var rewritten)
                    && rewritten != null
                    && rewritten.Count > 0)
                {
                    return rewritten;
                }
            }
            catch (Exception)
            {
                // Fallback for rules that fail due missing battle-controller context.
                if (instance.EffectData is ConfuseStatusEffect)
                {
                    float roll = _rng.RandRangeFloat(0f, 1f);
                    if (roll <= 0.5f)
                    {
                        var living = GetAllCombatants().Where(c => c != null && GodotObject.IsInstanceValid(c)).ToList();
                        if (living.Count > 0)
                        {
                            int index = _rng.RandRangeInt(0, living.Count - 1);
                            return new List<Node> { living[index] };
                        }
                    }
                }
            }
        }

        return currentTargets;
    }

    private void EquipSelectedAbility()
    {
        var combatant = ResolveSubject(GetSelectedSubject(_abilitySubjectSelect));
        var manager = GetAbilityManager(combatant);
        var ability = GetSelectedAbility();

        if (manager == null || ability == null)
        {
            Log("Equip ability failed: missing manager or selection.");
            return;
        }

        manager.LearnAbility(ability);
        bool equipped = manager.EquipAbility(ability);

        Log(equipped
            ? $"Equipped ability '{ability.AbilityName}' on {SafeName(combatant)}."
            : $"Could not equip ability '{ability.AbilityName}' on {SafeName(combatant)}.");

        RefreshAll();
    }

    private void UnequipSelectedAbility()
    {
        var combatant = ResolveSubject(GetSelectedSubject(_abilitySubjectSelect));
        var manager = GetAbilityManager(combatant);
        var ability = GetSelectedAbility();

        if (manager == null || ability == null)
        {
            Log("Unequip ability failed: missing manager or selection.");
            return;
        }

        manager.UnequipAbility(ability);
        Log($"Unequipped ability '{ability.AbilityName}' from {SafeName(combatant)}.");
        RefreshAll();
    }

    private void TriggerSelectedAbility()
    {
        var combatant = ResolveSubject(GetSelectedSubject(_abilitySubjectSelect));
        var manager = GetAbilityManager(combatant);
        if (combatant == null || manager == null)
        {
            Log("Trigger ability failed: missing subject or AbilityManager.");
            return;
        }

        var trigger = GetSelectedAbilityTrigger();
        if (trigger == AbilityTrigger.None)
        {
            Log("Trigger ability failed: invalid trigger selection.");
            return;
        }

        var context = new AbilityEffectContext(combatant, trigger)
        {
            OverflowSystem = _overflowSystem
        };

        manager.ApplyTrigger(trigger, context);
        Log($"Triggered abilities on {SafeName(combatant)} for trigger {trigger}.");
        RefreshAll();
    }

    private void AddSubjectCharge()
    {
        var combatant = ResolveSubject(GetSelectedSubject(_resourceSubjectSelect));
        int amount = Mathf.Max(1, (int)_chargeAmountSpin.Value);

        _chargeSystem.AddCharges(combatant, amount);
        Log($"Added {amount} charge to {SafeName(combatant)}.");
        RefreshAll();
    }

    private void SpendSubjectCharge()
    {
        var combatant = ResolveSubject(GetSelectedSubject(_resourceSubjectSelect));
        int amount = Mathf.Max(1, (int)_chargeAmountSpin.Value);

        bool ok = _chargeSystem.TrySpendCharges(combatant, amount);
        Log(ok
            ? $"Spent {amount} charge from {SafeName(combatant)}."
            : $"Could not spend {amount} charge from {SafeName(combatant)}.");

        RefreshAll();
    }

    private void SetSubjectCharge()
    {
        var combatant = ResolveSubject(GetSelectedSubject(_resourceSubjectSelect));
        int desired = Mathf.Max(0, (int)_chargeAmountSpin.Value);
        int current = _chargeSystem.GetCharges(combatant);

        if (desired > current)
        {
            _chargeSystem.AddCharges(combatant, desired - current);
        }
        else if (desired < current)
        {
            _chargeSystem.TrySpendCharges(combatant, current - desired);
        }

        Log($"Set charge for {SafeName(combatant)} to {desired}.");
        RefreshAll();
    }

    private void AddOverflow()
    {
        var side = GetSelectedOverflowSide();
        var source = ResolveRepresentativeForSide(side);
        int amount = Mathf.Max(1, (int)_overflowAmountSpin.Value);

        int applied = _overflowSystem.AddOverflow(source, amount, "SandboxAddOverflow");
        Log($"Added overflow ({side}) amount={amount}, applied={applied}.");
        RefreshAll();
    }

    private void SpendOverflow()
    {
        var side = GetSelectedOverflowSide();
        var source = ResolveRepresentativeForSide(side);
        int amount = Mathf.Max(1, (int)_overflowAmountSpin.Value);

        bool ok = _overflowSystem.TrySpend(source, amount, OverflowSpendType.Utility, "SandboxSpendOverflow", false, out var reason);
        Log(ok
            ? $"Spent overflow ({side}) amount={amount}."
            : $"Overflow spend failed ({side}): {reason}");

        RefreshAll();
    }

    private void SetOverflow()
    {
        var side = GetSelectedOverflowSide();
        var source = ResolveRepresentativeForSide(side);
        int desired = Mathf.Max(0, (int)_overflowAmountSpin.Value);
        int current = _overflowSystem.GetOverflowForSide(side);

        if (desired > current)
        {
            _overflowSystem.AddOverflow(source, desired - current, "SandboxSetOverflow");
        }
        else if (desired < current)
        {
            _overflowSystem.TrySpend(source, current - desired, OverflowSpendType.Utility, "SandboxSetOverflow", true, out _);
        }

        Log($"Set overflow ({side}) to {desired}.");
        RefreshAll();
    }

    private void SendOverflowPerfectHit()
    {
        var source = ResolveSubject(GetSelectedSubject(_resourceSubjectSelect));
        var target = source == _actor ? _target : _actor;

        _overflowSystem.ReportEvent(new OverflowEventData
        {
            TriggerType = OverflowTriggerType.PerfectTimedHit,
            Source = source,
            Target = target,
            Amount = 1f,
            Reason = "SandboxPerfectHit"
        });

        Log($"Reported perfect timed hit for {SafeName(source)}.");
        RefreshAll();
    }

    private void SendOverflowPerfectGuard()
    {
        var source = ResolveSubject(GetSelectedSubject(_resourceSubjectSelect));
        _overflowSystem.ReportPerfectTimedGuard(source);
        Log($"Reported perfect guard for {SafeName(source)}.");
        RefreshAll();
    }

    private void SendOverflowOverheal()
    {
        var source = ResolveSubject(GetSelectedSubject(_resourceSubjectSelect));
        var target = source;
        int amount = Mathf.Max(1, (int)_overflowAmountSpin.Value);

        _overflowSystem.ReportOverheal(source, target, amount);
        Log($"Reported overheal {amount} for {SafeName(source)}.");
        RefreshAll();
    }

    private void SendOverflowMpRestore()
    {
        var source = ResolveSubject(GetSelectedSubject(_resourceSubjectSelect));
        var target = source;
        int amount = Mathf.Max(1, (int)_overflowAmountSpin.Value);

        _overflowSystem.ReportMpRestored(source, target, amount);
        Log($"Reported MP restore {amount} for {SafeName(source)}.");
        RefreshAll();
    }

    private OverflowPartySide GetSelectedOverflowSide()
    {
        int idx = _overflowSideSelect?.Selected ?? 0;
        return idx == 1 ? OverflowPartySide.Enemy : OverflowPartySide.Player;
    }

    private Node ResolveRepresentativeForSide(OverflowPartySide side)
    {
        if (side == OverflowPartySide.Player)
        {
            if (_actor != null && _actor.IsInGroup(GameGroups.PlayerCharacters)) return _actor;
            if (_target != null && _target.IsInGroup(GameGroups.PlayerCharacters)) return _target;
            return _actor ?? _target;
        }

        if (_actor != null && !_actor.IsInGroup(GameGroups.PlayerCharacters)) return _actor;
        if (_target != null && !_target.IsInGroup(GameGroups.PlayerCharacters)) return _target;
        return _target ?? _actor;
    }

    private StatusEffect GetSelectedStatusEffect()
    {
        int idx = _statusSelect?.Selected ?? -1;
        if (idx < 0 || idx >= _statusOptions.Count) return null;
        return _statusOptions[idx];
    }

    private Ability GetSelectedAbility()
    {
        int idx = _abilitySelect?.Selected ?? -1;
        if (idx < 0 || idx >= _abilityOptions.Count) return null;
        return _abilityOptions[idx];
    }

    private AbilityTrigger GetSelectedAbilityTrigger()
    {
        int idx = _abilityTriggerSelect?.Selected ?? -1;
        if (idx < 0 || idx >= _abilityTriggers.Count) return AbilityTrigger.None;
        return _abilityTriggers[idx];
    }

    private ActionData GetSelectedMenuAction()
    {
        int idx = _menuActionList?.GetSelectedItems().FirstOrDefault() ?? -1;
        if (idx < 0 || idx >= _menuActions.Count) return null;
        return _menuActions[idx];
    }

    private PackedScene GetSelectedCharacterScene(OptionButton option)
    {
        int idx = option?.Selected ?? -1;
        if (idx < 0 || idx >= _characterOptions.Count) return null;
        return _characterOptions[idx];
    }

    private static StatsComponent GetStats(Node combatant)
    {
        return combatant?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
    }

    private static StatusEffectManager GetStatusManager(Node combatant)
    {
        return combatant?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
    }

    private static ActionManager GetActionManager(Node combatant)
    {
        return combatant?.GetNodeOrNull<ActionManager>(ActionManager.DefaultName);
    }

    private static AbilityManager GetAbilityManager(Node combatant)
    {
        return combatant?.GetNodeOrNull<AbilityManager>(AbilityManager.NodeName);
    }

    private Node ResolveSubject(SandboxSubject subject)
    {
        return subject == SandboxSubject.Target ? _target : _actor;
    }

    private Node ResolveSelectedUnit(OptionButton option)
    {
        int idx = option?.Selected ?? 0;
        return idx == 1 ? _target : _actor;
    }

    private static string SafeName(Node node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node)) return "(none)";
        return node.Name;
    }

    private SandboxSubject GetSelectedSubject(OptionButton option)
    {
        int idx = option?.Selected ?? 0;
        return idx == 1 ? SandboxSubject.Target : SandboxSubject.Actor;
    }

    private AttackData BuildActionTemplate(string name, ActionCategory category, float physicalRatio, int power, int accuracy, int critChance, ActionFlags flags)
    {
        var action = new AttackData();
        action.CommandName = name;
        action.Description = $"Sandbox action: {name}";

        action.Set("Category", (int)category);
        action.Set("PhysicalRatio", Mathf.Clamp(physicalRatio, 0f, 1f));
        action.Set("CritChance", Mathf.Clamp(critChance, 0, 100));
        action.Set("TickCost", 0f);
        action.Set("Flags", (int)flags);
        action.Set("Power", Mathf.Max(1, power));
        action.Set("Accuracy", Mathf.Clamp(accuracy, 1, 100));
        action.Set("AllowedTargeting", (int)(TargetingType.AnyEnemy | TargetingType.AnyAlly | TargetingType.Self));
        action.Set("BaseTargeting", (int)TargetingType.AnySingleTarget);
        action.Set("CalculationStrategy", _defaultCalculation);

        return action;
    }

    private void SetMenuValidationText(string text)
    {
        if (_menuValidationLabel != null)
        {
            _menuValidationLabel.Text = text ?? string.Empty;
        }
    }

    private void Log(string message)
    {
        if (_logLabel == null)
        {
            GD.Print(message);
            return;
        }

        string line = $"[{Time.GetTimeStringFromSystem()}] {message}\n";
        _logLabel.AppendText(line);
        _logLabel.ScrollToLine(Mathf.Max(0, _logLabel.GetLineCount() - 1));
    }

    private enum SandboxSubject
    {
        Actor = 0,
        Target = 1
    }
}
