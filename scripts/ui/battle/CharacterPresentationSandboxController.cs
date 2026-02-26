using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Focused sandbox for testing one character's presentation changes from statuses,
/// abilities, and equipment without running full battle flow.
/// </summary>
public partial class CharacterPresentationSandboxController : Control
{
    [ExportGroup("Resource Roots")]
    [Export]
    public string StatusResourceRoot { get; set; } = "res://assets/resources/status_effects";

    [Export]
    public string AbilityResourceRoot { get; set; } = "res://assets/resources/abilities";

    [Export]
    public string ItemResourceRoot { get; set; } = "res://assets/resources/items";

    [ExportGroup("Characters")]
    [Export]
    public Godot.Collections.Array<PackedScene> CharacterSceneLibrary { get; set; } = new();

    [Export]
    public PackedScene DefaultCharacterScene { get; set; }

    [Export]
    public BaseStats FallbackBaseStats { get; set; }

    [ExportGroup("UI")]
    [Export]
    public PackedScene PartyStatusRowScene { get; set; }

    [ExportGroup("Preview Overlays")]
    [Export]
    public bool ShowFloatingStatsOverlay { get; set; } = false;

    [Export]
    public bool HideBackdropInFocusView { get; set; } = true;

    [Export]
    public bool UseNativePreviewWindow { get; set; } = true;

    [ExportGroup("Focus Camera")]
    [Export(PropertyHint.Range, "1.0,20.0,0.1")]
    public float FocusCameraDistance { get; set; } = 3.8f;

    [Export(PropertyHint.Range, "0.5,12.0,0.1")]
    public float FocusCameraMinDistance { get; set; } = 2.0f;

    [Export(PropertyHint.Range, "1.0,24.0,0.1")]
    public float FocusCameraMaxDistance { get; set; } = 14.0f;

    [Export(PropertyHint.Range, "-2.0,4.0,0.05")]
    public float FocusCameraHeightOffset { get; set; } = 1.25f;

    [Export(PropertyHint.Range, "0.0,4.0,0.05")]
    public float FocusCameraLookHeight { get; set; } = 1.0f;

    [Export(PropertyHint.Range, "0.05,1.0,0.01")]
    public float FocusCameraFollowLerp { get; set; } = 1.0f;

    [Export]
    public bool FocusCameraFollowEnabled { get; set; } = true;

    [Export]
    public bool EnableMouseWheelFocusZoom { get; set; } = true;

    [Export(PropertyHint.Range, "0.05,2.0,0.05")]
    public float FocusCameraZoomStep { get; set; } = 0.55f;

    [ExportGroup("Focus Lighting")]
    [Export]
    public bool BoostLightingInFocusView { get; set; } = true;

    [Export(PropertyHint.Range, "0.0,8.0,0.05")]
    public float FocusViewDirectionalLightEnergy { get; set; } = 2.2f;

    [Export]
    public bool UseFocusFillLight { get; set; } = true;

    [Export(PropertyHint.Range, "0.0,8.0,0.05")]
    public float FocusFillLightEnergy { get; set; } = 1.4f;

    private const string PreviewWindowToggleTooltip = "Open/close a secondary preview window that renders the live character scene.";
    private const string PreviewWindowDisabledTooltip = "Detached preview requires Display > Window > Subwindows > Embed Subwindows = false. Restart after changing project settings.";

    private Node3D _characterAnchor;
    private Camera3D _sandboxCamera;
    private ColorRect _backdrop;
    private DirectionalLight3D _directionalLight;
    private OmniLight3D _focusFillLight;
    private Control _mainSplit;
    private Button _toggleUiButton;
    private Button _togglePreviewWindowButton;
    private Window _characterPreviewWindow;
    private SubViewport _characterPreviewViewport;
    private Camera3D _characterPreviewCamera;
    private bool _panelsVisible = true;

    private OptionButton _characterSceneSelect;
    private Button _respawnCharacterButton;

    private OptionButton _statusSelect;
    private Button _applyStatusButton;
    private Button _removeStatusButton;
    private Button _clearStatusesButton;
    private ItemList _activeStatusList;

    private OptionButton _abilitySelect;
    private Button _equipAbilityButton;
    private Button _unequipAbilityButton;
    private Button _unequipAllAbilitiesButton;
    private OptionButton _abilityTriggerSelect;
    private Button _triggerAbilityButton;
    private ItemList _equippedAbilityList;

    private OptionButton _equipmentSlotSelect;
    private OptionButton _equipmentItemSelect;
    private Button _equipItemButton;
    private Button _unequipSlotButton;
    private ItemList _equippedItemsList;

    private SpinBox _setHpSpin;
    private SpinBox _setMpSpin;
    private Button _setHpButton;
    private Button _setMpButton;
    private SpinBox _deltaSpin;
    private Button _damageButton;
    private Button _healButton;
    private Button _gainMpButton;
    private Button _spendMpButton;

    private SpinBox _chargeSpin;
    private Button _addChargeButton;
    private Button _spendChargeButton;
    private Button _setChargeButton;

    private Button _turnStartButton;
    private Button _turnEndButton;

    private Control _rowHost;
    private RichTextLabel _detailedStatsLabel;
    private RichTextLabel _componentTreeLabel;
    private RichTextLabel _logLabel;

    private PanelContainer _floatingStatsPanel;
    private Label _floatingStatsLabel;

    private Node _character;
    private BattlePartyStatusRow _partyStatusRow;
    private ChargeSystem _chargeSystem;
    private float _focusCameraDistanceCurrent;
    private float _baseDirectionalLightEnergy = -1f;

    private readonly List<PackedScene> _characterOptions = new();
    private readonly List<StatusEffect> _statusOptions = new();
    private readonly List<Ability> _abilityOptions = new();
    private readonly List<ItemData> _equipmentItemOptions = new();
    private readonly List<EquipmentSlot> _equipmentSlots = new();
    private readonly List<AbilityTrigger> _abilityTriggers = new();

    private StatsComponent _boundStats;
    private StatusEffectManager _boundStatusManager;
    private AbilityManager _boundAbilityManager;
    private EquipmentManager _boundEquipmentManager;

    private readonly IRandomNumberGenerator _rng = new GodotRandomNumberGenerator();

    private const string PlayerSceneFallback = "res://assets/resources/characters/ceira/ceira.tscn";
    private const string EnemySceneFallback = "res://assets/resources/characters/goblin_lv5/goblin_lv5_animated.tscn";
    private const string TemplateSceneFallback = "res://assets/resources/characters/CharacterTemplate.tscn";

    public override void _Ready()
    {
        CacheNodes();
        InitializeFocusCamera();
        EnsureSystems();
        ConfigurePreviewWindowMode();
        EnsureCharacterLibrary();
        LoadResourceLibraries();
        PopulateTriggerSelector();
        PopulateCharacterSelector();
        PopulateStatusSelector();
        PopulateAbilitySelector();
        PopulateEquipmentItemSelector();

        WireUiEvents();
        BindPreviewViewportWorld();
        UpdatePanelToggleButtonText();
        UpdatePreviewWindowButtonText();
        RespawnCharacter();

        Log("Character presentation sandbox ready.");
    }

    public override void _ExitTree()
    {
        UnbindCharacterSignals();
        if (_chargeSystem != null)
        {
            _chargeSystem.ChargesChanged -= OnChargesChanged;
        }
        if (_characterPreviewWindow != null)
        {
            var closeCallable = Callable.From(OnPreviewWindowCloseRequested);
            if (_characterPreviewWindow.IsConnected(Window.SignalName.CloseRequested, closeCallable))
            {
                _characterPreviewWindow.Disconnect(Window.SignalName.CloseRequested, closeCallable);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!_panelsVisible)
        {
            UpdateFocusCameraFollow();
        }
        UpdateFloatingStatsPanelPosition();
        UpdatePreviewCameraFollow();
    }

    public override void _Input(InputEvent @event)
    {
        if (!EnableMouseWheelFocusZoom || _panelsVisible) return;
        if (_sandboxCamera == null) return;
        if (_character is not Node3D || !GodotObject.IsInstanceValid(_character)) return;

        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed) return;

        if (mouseButton.ButtonIndex == MouseButton.WheelUp)
        {
            AdjustFocusCameraDistance(-FocusCameraZoomStep);
            GetViewport().SetInputAsHandled();
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
        {
            AdjustFocusCameraDistance(FocusCameraZoomStep);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            RepositionPreviewWindow();
        }
    }

    private void CacheNodes()
    {
        _characterAnchor = GetNodeOrNull<Node3D>("SandboxWorld/CharacterAnchor");
        _sandboxCamera = GetNodeOrNull<Camera3D>("SandboxWorld/Camera3D");
        _backdrop = GetNodeOrNull<ColorRect>("Backdrop");
        _directionalLight = GetNodeOrNull<DirectionalLight3D>("SandboxWorld/DirectionalLight3D");
        _focusFillLight = GetNodeOrNull<OmniLight3D>("SandboxWorld/FocusFillLight3D");
        _mainSplit = GetNodeOrNull<Control>("UI/MainSplit");
        _toggleUiButton = GetNodeOrNull<Button>("UI/PreviewToolbar/ToggleUiButton");
        _togglePreviewWindowButton = GetNodeOrNull<Button>("UI/PreviewToolbar/TogglePreviewWindowButton");
        _characterPreviewWindow = GetNodeOrNull<Window>("CharacterPreviewWindow");
        _characterPreviewViewport = GetNodeOrNull<SubViewport>("CharacterPreviewWindow/SubViewportContainer/SubViewport");
        _characterPreviewCamera = GetNodeOrNull<Camera3D>("CharacterPreviewWindow/SubViewportContainer/SubViewport/PreviewCamera3D");

        _characterSceneSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftPanel/CharacterPanel/Body/CharacterSceneSelect");
        _respawnCharacterButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/CharacterPanel/Body/RespawnCharacterButton");

        _statusSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftPanel/StatusPanel/Body/StatusSelect");
        _applyStatusButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/StatusPanel/Body/StatusButtonRow/ApplyStatusButton");
        _removeStatusButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/StatusPanel/Body/StatusButtonRow/RemoveStatusButton");
        _clearStatusesButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/StatusPanel/Body/StatusButtonRow/ClearStatusesButton");
        _activeStatusList = GetNodeOrNull<ItemList>("UI/MainSplit/LeftPanel/StatusPanel/Body/ActiveStatusList");

        _abilitySelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftPanel/AbilityPanel/Body/AbilitySelect");
        _equipAbilityButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/AbilityPanel/Body/AbilityButtonRow/EquipAbilityButton");
        _unequipAbilityButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/AbilityPanel/Body/AbilityButtonRow/UnequipAbilityButton");
        _unequipAllAbilitiesButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/AbilityPanel/Body/AbilityButtonRow/UnequipAllAbilitiesButton");
        _abilityTriggerSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftPanel/AbilityPanel/Body/AbilityTriggerRow/AbilityTriggerSelect");
        _triggerAbilityButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/AbilityPanel/Body/AbilityTriggerRow/TriggerAbilityButton");
        _equippedAbilityList = GetNodeOrNull<ItemList>("UI/MainSplit/LeftPanel/AbilityPanel/Body/EquippedAbilityList");

        _equipmentSlotSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftPanel/EquipmentPanel/Body/EquipmentControlRow/EquipmentSlotSelect");
        _equipmentItemSelect = GetNodeOrNull<OptionButton>("UI/MainSplit/LeftPanel/EquipmentPanel/Body/EquipmentControlRow/EquipmentItemSelect");
        _equipItemButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/EquipmentPanel/Body/EquipmentButtonRow/EquipItemButton");
        _unequipSlotButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/EquipmentPanel/Body/EquipmentButtonRow/UnequipSlotButton");
        _equippedItemsList = GetNodeOrNull<ItemList>("UI/MainSplit/LeftPanel/EquipmentPanel/Body/EquippedItemsList");

        _setHpSpin = GetNodeOrNull<SpinBox>("UI/MainSplit/LeftPanel/UtilityPanel/Body/SetRow/SetHpSpin");
        _setMpSpin = GetNodeOrNull<SpinBox>("UI/MainSplit/LeftPanel/UtilityPanel/Body/SetRow/SetMpSpin");
        _setHpButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/UtilityPanel/Body/SetRow/SetHpButton");
        _setMpButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/UtilityPanel/Body/SetRow/SetMpButton");

        _deltaSpin = GetNodeOrNull<SpinBox>("UI/MainSplit/LeftPanel/UtilityPanel/Body/DeltaRow/DeltaSpin");
        _damageButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/UtilityPanel/Body/DeltaRow/DamageButton");
        _healButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/UtilityPanel/Body/DeltaRow/HealButton");
        _gainMpButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/UtilityPanel/Body/DeltaRow/GainMpButton");
        _spendMpButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/UtilityPanel/Body/DeltaRow/SpendMpButton");

        _chargeSpin = GetNodeOrNull<SpinBox>("UI/MainSplit/LeftPanel/UtilityPanel/Body/ChargeRow/ChargeSpin");
        _addChargeButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/UtilityPanel/Body/ChargeRow/AddChargeButton");
        _spendChargeButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/UtilityPanel/Body/ChargeRow/SpendChargeButton");
        _setChargeButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/UtilityPanel/Body/ChargeRow/SetChargeButton");

        _turnStartButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/UtilityPanel/Body/TurnRow/TurnStartButton");
        _turnEndButton = GetNodeOrNull<Button>("UI/MainSplit/LeftPanel/UtilityPanel/Body/TurnRow/TurnEndButton");

        _rowHost = GetNodeOrNull<Control>("UI/MainSplit/RightPanel/StatusRowPanel/Body/RowHost");
        _detailedStatsLabel = GetNodeOrNull<RichTextLabel>("UI/MainSplit/RightPanel/DetailedStatsPanel/Body/DetailedStatsLabel");
        _componentTreeLabel = GetNodeOrNull<RichTextLabel>("UI/MainSplit/RightPanel/ComponentTreePanel/Body/ComponentTreeLabel");
        _logLabel = GetNodeOrNull<RichTextLabel>("UI/MainSplit/RightPanel/LogPanel/Body/LogLabel");

        _floatingStatsPanel = GetNodeOrNull<PanelContainer>("UI/FloatingStatsPanel");
        _floatingStatsLabel = GetNodeOrNull<Label>("UI/FloatingStatsPanel/FloatingStatsLabel");
    }

    private void InitializeFocusCamera()
    {
        _focusCameraDistanceCurrent = FocusCameraDistance;
        ClampFocusCameraDistance();
        EnsureSandboxCameraCurrent();
        InitializeFocusLighting();
        ApplyFocusLightingState();
    }

    private void InitializeFocusLighting()
    {
        if (_directionalLight != null && _baseDirectionalLightEnergy < 0f)
        {
            _baseDirectionalLightEnergy = _directionalLight.LightEnergy;
        }

        if (_focusFillLight != null)
        {
            _focusFillLight.LightEnergy = Mathf.Max(0f, FocusFillLightEnergy);
            _focusFillLight.Visible = false;
        }
    }

    private void EnsureSystems()
    {
        _chargeSystem = GetNodeOrNull<ChargeSystem>("ChargeSystem");
        if (_chargeSystem == null)
        {
            _chargeSystem = new ChargeSystem { Name = "ChargeSystem" };
            AddChild(_chargeSystem);
        }

        _chargeSystem.Initialize(null);
        _chargeSystem.ChargesChanged -= OnChargesChanged;
        _chargeSystem.ChargesChanged += OnChargesChanged;

        if (PartyStatusRowScene == null)
        {
            PartyStatusRowScene = GD.Load<PackedScene>("res://assets/scenes/battle/ui/BattlePartyStatusRow.tscn");
        }
    }

    private void ConfigurePreviewWindowMode()
    {
        if (_characterPreviewWindow == null) return;

        _characterPreviewWindow.Borderless = false;
        _characterPreviewWindow.Unresizable = false;
        _characterPreviewWindow.PopupWindow = false;
        _characterPreviewWindow.Transient = false;
        _characterPreviewWindow.Exclusive = false;
        _characterPreviewWindow.AlwaysOnTop = false;
        _characterPreviewWindow.Unfocusable = false;
        _characterPreviewWindow.Visible = false;

        RefreshPreviewWindowAvailability();
        RepositionPreviewWindow();
    }

    private void RefreshPreviewWindowAvailability()
    {
        if (_characterPreviewWindow == null) return;

        bool embedded = GetViewport().GuiEmbedSubwindows;
        bool canUseDetachedPreview = !embedded;

        if (!canUseDetachedPreview)
        {
            _characterPreviewWindow.Visible = false;
        }

        if (_togglePreviewWindowButton == null) return;

        _togglePreviewWindowButton.Disabled = !canUseDetachedPreview;
        _togglePreviewWindowButton.TooltipText = canUseDetachedPreview
            ? PreviewWindowToggleTooltip
            : PreviewWindowDisabledTooltip;

        if (!canUseDetachedPreview)
        {
            _togglePreviewWindowButton.Text = "Open Character Window (Detached Only)";
            return;
        }

        UpdatePreviewWindowButtonText();
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

        if (DefaultCharacterScene == null)
        {
            DefaultCharacterScene = _characterOptions.FirstOrDefault();
        }
    }

    private void LoadResourceLibraries()
    {
        _statusOptions.Clear();
        LoadResourcesRecursive(StatusResourceRoot, resource =>
        {
            if (resource is StatusEffect effect)
            {
                _statusOptions.Add(effect);
            }
        });
        _statusOptions.Sort((a, b) => string.Compare(a?.EffectName, b?.EffectName, StringComparison.OrdinalIgnoreCase));

        _abilityOptions.Clear();
        LoadResourcesRecursive(AbilityResourceRoot, resource =>
        {
            if (resource is Ability ability)
            {
                _abilityOptions.Add(ability);
            }
        });
        _abilityOptions.Sort((a, b) => string.Compare(a?.AbilityName, b?.AbilityName, StringComparison.OrdinalIgnoreCase));

        _equipmentItemOptions.Clear();
        LoadResourcesRecursive(ItemResourceRoot, resource =>
        {
            if (resource is not ItemData item) return;
            if (TryGetEquippableComponent(item, out _))
            {
                _equipmentItemOptions.Add(item);
            }
        });

        if (_equipmentItemOptions.Count == 0)
        {
            _equipmentItemOptions.AddRange(CreateDebugEquipmentItems());
        }

        _equipmentItemOptions.Sort((a, b) => string.Compare(a?.ItemName, b?.ItemName, StringComparison.OrdinalIgnoreCase));
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

    private void PopulateCharacterSelector()
    {
        if (_characterSceneSelect == null) return;

        _characterSceneSelect.Clear();
        int selected = 0;
        for (int i = 0; i < _characterOptions.Count; i++)
        {
            var scene = _characterOptions[i];
            _characterSceneSelect.AddItem(GetSceneDisplayName(scene), i);
            if (scene == DefaultCharacterScene)
            {
                selected = i;
            }
        }

        if (_characterOptions.Count > 0)
        {
            _characterSceneSelect.Selected = Mathf.Clamp(selected, 0, _characterOptions.Count - 1);
        }
    }

    private static string GetSceneDisplayName(PackedScene scene)
    {
        if (scene == null) return "(none)";
        if (!string.IsNullOrEmpty(scene.ResourcePath)) return scene.ResourcePath.GetFile().GetBaseName();
        return string.IsNullOrEmpty(scene.ResourceName) ? "(unnamed scene)" : scene.ResourceName;
    }

    private void PopulateStatusSelector()
    {
        if (_statusSelect == null) return;
        _statusSelect.Clear();
        foreach (var effect in _statusOptions)
        {
            _statusSelect.AddItem(effect?.EffectName ?? "(unnamed)");
        }
    }

    private void PopulateAbilitySelector()
    {
        if (_abilitySelect == null) return;
        _abilitySelect.Clear();
        foreach (var ability in _abilityOptions)
        {
            _abilitySelect.AddItem(ability?.AbilityName ?? "(unnamed)");
        }
    }

    private void PopulateTriggerSelector()
    {
        _abilityTriggers.Clear();
        if (_abilityTriggerSelect == null) return;

        _abilityTriggerSelect.Clear();
        foreach (AbilityTrigger trigger in Enum.GetValues(typeof(AbilityTrigger)))
        {
            if (trigger == AbilityTrigger.None) continue;
            _abilityTriggers.Add(trigger);
            _abilityTriggerSelect.AddItem(trigger.ToString());
        }

        if (_abilityTriggerSelect.ItemCount > 0)
        {
            _abilityTriggerSelect.Selected = 0;
        }
    }

    private void PopulateEquipmentItemSelector()
    {
        if (_equipmentItemSelect == null) return;

        _equipmentItemSelect.Clear();
        foreach (var item in _equipmentItemOptions)
        {
            string label = BuildItemLabel(item);
            _equipmentItemSelect.AddItem(label);
        }
    }

    private static string BuildItemLabel(ItemData item)
    {
        if (item == null) return "(null item)";
        if (TryGetEquippableComponent(item, out var equip))
        {
            return $"{item.ItemName} ({equip.SlotType})";
        }

        return item.ItemName;
    }

    private void WireUiEvents()
    {
        _toggleUiButton?.Connect(Button.SignalName.Pressed, Callable.From(TogglePanelsVisibility));
        _togglePreviewWindowButton?.Connect(Button.SignalName.Pressed, Callable.From(TogglePreviewWindowVisibility));
        _respawnCharacterButton?.Connect(Button.SignalName.Pressed, Callable.From(RespawnCharacter));
        if (_characterPreviewWindow != null)
        {
            var closeCallable = Callable.From(OnPreviewWindowCloseRequested);
            if (!_characterPreviewWindow.IsConnected(Window.SignalName.CloseRequested, closeCallable))
            {
                _characterPreviewWindow.Connect(Window.SignalName.CloseRequested, closeCallable);
            }
        }

        _applyStatusButton?.Connect(Button.SignalName.Pressed, Callable.From(ApplySelectedStatus));
        _removeStatusButton?.Connect(Button.SignalName.Pressed, Callable.From(RemoveSelectedStatus));
        _clearStatusesButton?.Connect(Button.SignalName.Pressed, Callable.From(ClearStatuses));

        _equipAbilityButton?.Connect(Button.SignalName.Pressed, Callable.From(EquipSelectedAbility));
        _unequipAbilityButton?.Connect(Button.SignalName.Pressed, Callable.From(UnequipSelectedAbility));
        _unequipAllAbilitiesButton?.Connect(Button.SignalName.Pressed, Callable.From(UnequipAllAbilities));
        _triggerAbilityButton?.Connect(Button.SignalName.Pressed, Callable.From(TriggerSelectedAbility));

        _equipItemButton?.Connect(Button.SignalName.Pressed, Callable.From(EquipSelectedItemToSlot));
        _unequipSlotButton?.Connect(Button.SignalName.Pressed, Callable.From(UnequipSelectedSlot));

        _setHpButton?.Connect(Button.SignalName.Pressed, Callable.From(SetHp));
        _setMpButton?.Connect(Button.SignalName.Pressed, Callable.From(SetMp));

        _damageButton?.Connect(Button.SignalName.Pressed, Callable.From(ApplyDamage));
        _healButton?.Connect(Button.SignalName.Pressed, Callable.From(ApplyHealing));
        _gainMpButton?.Connect(Button.SignalName.Pressed, Callable.From(GainMp));
        _spendMpButton?.Connect(Button.SignalName.Pressed, Callable.From(SpendMp));

        _addChargeButton?.Connect(Button.SignalName.Pressed, Callable.From(AddCharge));
        _spendChargeButton?.Connect(Button.SignalName.Pressed, Callable.From(SpendCharge));
        _setChargeButton?.Connect(Button.SignalName.Pressed, Callable.From(SetCharge));

        _turnStartButton?.Connect(Button.SignalName.Pressed, Callable.From(SimulateTurnStart));
        _turnEndButton?.Connect(Button.SignalName.Pressed, Callable.From(SimulateTurnEnd));
    }

    private void TogglePanelsVisibility()
    {
        _panelsVisible = !_panelsVisible;
        if (_mainSplit != null)
        {
            _mainSplit.Visible = _panelsVisible;
        }
        if (_backdrop != null)
        {
            _backdrop.Visible = _panelsVisible || !HideBackdropInFocusView;
        }

        if (!_panelsVisible)
        {
            EnsureSandboxCameraCurrent();
            PositionFocusCameraImmediate();
        }

        ApplyFocusLightingState();
        UpdatePanelToggleButtonText();
    }

    private void ApplyFocusLightingState()
    {
        if (_directionalLight != null && BoostLightingInFocusView)
        {
            if (_baseDirectionalLightEnergy < 0f)
            {
                _baseDirectionalLightEnergy = _directionalLight.LightEnergy;
            }

            _directionalLight.LightEnergy = _panelsVisible
                ? _baseDirectionalLightEnergy
                : Mathf.Max(_baseDirectionalLightEnergy, FocusViewDirectionalLightEnergy);
        }

        if (_focusFillLight != null)
        {
            _focusFillLight.LightEnergy = Mathf.Max(0f, FocusFillLightEnergy);
            _focusFillLight.Visible = !_panelsVisible && UseFocusFillLight;
        }
    }

    private void UpdatePanelToggleButtonText()
    {
        if (_toggleUiButton == null) return;
        _toggleUiButton.Text = _panelsVisible ? "Focus Character View" : "Show Panels";
    }

    private void TogglePreviewWindowVisibility()
    {
        if (_characterPreviewWindow == null) return;
        if (GetViewport().GuiEmbedSubwindows)
        {
            _characterPreviewWindow.Visible = false;
            RefreshPreviewWindowAvailability();
            Log("Character preview window is disabled in embedded-subwindow mode. Set Project Settings > Display > Window > Subwindows > Embed Subwindows = false and restart.");
            return;
        }

        bool shouldShow = !_characterPreviewWindow.Visible;
        _characterPreviewWindow.Visible = shouldShow;

        if (shouldShow)
        {
            BindPreviewViewportWorld();
            RepositionPreviewWindow();
            PositionPreviewCameraImmediate();
            if (UseNativePreviewWindow && GetViewport().GuiEmbedSubwindows)
            {
                Log("Preview window is embedded. For a true OS window, set Project Settings > Display > Window > Subwindows > Embed Subwindows = false and restart.");
            }
        }

        UpdatePreviewWindowButtonText();
    }

    private void OnPreviewWindowCloseRequested()
    {
        if (_characterPreviewWindow == null) return;
        _characterPreviewWindow.Visible = false;
        UpdatePreviewWindowButtonText();
    }

    private void UpdatePreviewWindowButtonText()
    {
        if (_togglePreviewWindowButton == null) return;
        if (_togglePreviewWindowButton.Disabled) return;

        bool visible = _characterPreviewWindow != null && _characterPreviewWindow.Visible;
        _togglePreviewWindowButton.Text = visible ? "Close Character Window" : "Open Character Window";
    }

    private void BindPreviewViewportWorld()
    {
        if (_characterPreviewViewport == null) return;

        // Share the same world so the preview window shows the live character instance.
        _characterPreviewViewport.World3D = GetViewport().World3D;
    }

    private void RepositionPreviewWindow()
    {
        if (_characterPreviewWindow == null) return;

        var viewportSize = GetViewportRect().Size;
        var size = _characterPreviewWindow.Size;
        int x = Mathf.Max(0, Mathf.RoundToInt(viewportSize.X - size.X - 20f));
        int y = 56;
        _characterPreviewWindow.Position = new Vector2I(x, y);
    }

    private void RespawnCharacter()
    {
        var selectedScene = GetSelectedCharacterScene() ?? DefaultCharacterScene;
        _character = SpawnCharacter(_character, selectedScene, _characterAnchor, "PreviewCharacter");

        EnsureSandboxCameraCurrent();
        PositionFocusCameraImmediate();
        EnsurePartyStatusRow();
        BindCharacterSignals();
        RefreshAll();
        PositionPreviewCameraImmediate();

        Log($"Spawned character: {SafeName(_character)}.");
    }

    private PackedScene GetSelectedCharacterScene()
    {
        if (_characterSceneSelect == null || _characterOptions.Count == 0) return null;
        int index = Mathf.Clamp(_characterSceneSelect.Selected, 0, _characterOptions.Count - 1);
        return _characterOptions[index];
    }

    private Node SpawnCharacter(Node existing, PackedScene scene, Node3D anchor, string fallbackName)
    {
        if (existing != null && GodotObject.IsInstanceValid(existing))
        {
            existing.QueueFree();
        }

        if (anchor == null)
        {
            Log("Spawn failed: CharacterAnchor missing.");
            return null;
        }

        Node character = scene?.Instantiate();
        if (character == null)
        {
            character = CreateFallbackCharacter(fallbackName);
        }

        character.Name = fallbackName;
        anchor.AddChild(character);
        character.AddToGroup(GameGroups.PlayerCharacters);

        EnsureCombatantComponents(character);
        PositionCharacter(character);
        return character;
    }

    private static void PositionCharacter(Node character)
    {
        if (character is not Node3D node3D) return;
        node3D.Position = Vector3.Zero;
        node3D.Rotation = Vector3.Zero;
    }

    private Node CreateFallbackCharacter(string name)
    {
        var fallback = new BaseCharacter { Name = name };

        var stats = new StatsComponent { Name = StatsComponent.NodeName };
        stats.SetBaseStatsResource(ResolveFallbackStats());
        fallback.AddChild(stats);

        fallback.AddChild(new StatusEffectManager { Name = StatusEffectManager.NodeName });
        fallback.AddChild(new AbilityManager { Name = AbilityManager.NodeName });

        var visual = new CharacterVisualStateController { Name = CharacterVisualStateController.NodeName };
        fallback.AddChild(visual);

        var equipment = BuildEquipmentManagerWithDefaultSlots();
        fallback.AddChild(equipment);

        return fallback;
    }

    private void EnsureCombatantComponents(Node character)
    {
        if (character == null) return;

        var stats = GetStats(character);
        if (stats == null)
        {
            stats = new StatsComponent { Name = StatsComponent.NodeName };
            character.AddChild(stats);
        }

        if (stats.GetStatValue(StatType.HP) <= 0 || stats.GetStatValue(StatType.MP) <= 0)
        {
            stats.SetBaseStatsResource(ResolveFallbackStats());
        }

        if (GetStatusManager(character) == null)
        {
            character.AddChild(new StatusEffectManager { Name = StatusEffectManager.NodeName });
        }

        if (GetAbilityManager(character) == null)
        {
            character.AddChild(new AbilityManager { Name = AbilityManager.NodeName });
        }

        if (character.GetNodeOrNull<CharacterVisualStateController>(CharacterVisualStateController.NodeName) == null)
        {
            character.AddChild(new CharacterVisualStateController { Name = CharacterVisualStateController.NodeName });
        }

        var equipment = GetEquipmentManager(character);
        if (equipment == null)
        {
            character.AddChild(BuildEquipmentManagerWithDefaultSlots());
        }
        else if (equipment.GetSlots() == null || equipment.GetSlots().Count == 0)
        {
            character.RemoveChild(equipment);
            equipment.QueueFree();
            character.AddChild(BuildEquipmentManagerWithDefaultSlots());
        }
    }

    private EquipmentManager BuildEquipmentManagerWithDefaultSlots()
    {
        var manager = new EquipmentManager { Name = "EquipmentManager" };

        manager.AddChild(new EquipmentSlot { Name = "Weapon", SlotType = EquipmentSlotType.Weapon });
        manager.AddChild(new EquipmentSlot { Name = "Shield", SlotType = EquipmentSlotType.Shield });
        manager.AddChild(new EquipmentSlot { Name = "Head", SlotType = EquipmentSlotType.Head });
        manager.AddChild(new EquipmentSlot { Name = "Body", SlotType = EquipmentSlotType.Body });
        manager.AddChild(new EquipmentSlot { Name = "Accessory", SlotType = EquipmentSlotType.Accessory });

        return manager;
    }

    private BaseStats ResolveFallbackStats()
    {
        return FallbackBaseStats ?? new BaseStats
        {
            HP = 800,
            MP = 220,
            Strength = 42,
            Defense = 34,
            Magic = 38,
            MagicDefense = 30,
            Speed = 20,
            Evasion = 7,
            MgEvasion = 7,
            Accuracy = 12,
            MgAccuracy = 12,
            Luck = 10,
            AP = 40
        };
    }

    private void EnsurePartyStatusRow()
    {
        if (_rowHost == null) return;

        if (_partyStatusRow == null || !GodotObject.IsInstanceValid(_partyStatusRow))
        {
            _partyStatusRow = _rowHost.GetChildren().OfType<BattlePartyStatusRow>().FirstOrDefault();
        }

        if ((_partyStatusRow == null || !GodotObject.IsInstanceValid(_partyStatusRow)) && PartyStatusRowScene != null)
        {
            _partyStatusRow = PartyStatusRowScene.Instantiate<BattlePartyStatusRow>();
            _rowHost.AddChild(_partyStatusRow);
        }

        _partyStatusRow?.Bind(_character, _chargeSystem);
        _partyStatusRow?.SetActive(true);
    }

    private void BindCharacterSignals()
    {
        UnbindCharacterSignals();

        _boundStats = GetStats(_character);
        _boundStatusManager = GetStatusManager(_character);
        _boundAbilityManager = GetAbilityManager(_character);
        _boundEquipmentManager = GetEquipmentManager(_character);

        if (_boundStats != null)
        {
            _boundStats.CurrentHPChanged += OnHpChanged;
            _boundStats.CurrentMPChanged += OnMpChanged;
            _boundStats.StatValueChanged += OnStatValueChanged;
        }

        if (_boundStatusManager != null)
        {
            _boundStatusManager.StatusEffectApplied += OnStatusChanged;
            _boundStatusManager.StatusEffectRemoved += OnStatusChanged;
        }

        if (_boundAbilityManager != null)
        {
            _boundAbilityManager.KnownAbilitiesChanged += OnAbilityListChanged;
            _boundAbilityManager.EquippedAbilitiesChanged += OnAbilityListChanged;
        }

        if (_boundEquipmentManager != null)
        {
            _boundEquipmentManager.EquipmentChanged += OnEquipmentChanged;
            _boundEquipmentManager.SlotAdded += OnEquipmentSlotAdded;
        }
    }

    private void UnbindCharacterSignals()
    {
        if (_boundStats != null)
        {
            _boundStats.CurrentHPChanged -= OnHpChanged;
            _boundStats.CurrentMPChanged -= OnMpChanged;
            _boundStats.StatValueChanged -= OnStatValueChanged;
        }

        if (_boundStatusManager != null)
        {
            _boundStatusManager.StatusEffectApplied -= OnStatusChanged;
            _boundStatusManager.StatusEffectRemoved -= OnStatusChanged;
        }

        if (_boundAbilityManager != null)
        {
            _boundAbilityManager.KnownAbilitiesChanged -= OnAbilityListChanged;
            _boundAbilityManager.EquippedAbilitiesChanged -= OnAbilityListChanged;
        }

        if (_boundEquipmentManager != null)
        {
            _boundEquipmentManager.EquipmentChanged -= OnEquipmentChanged;
            _boundEquipmentManager.SlotAdded -= OnEquipmentSlotAdded;
        }

        _boundStats = null;
        _boundStatusManager = null;
        _boundAbilityManager = null;
        _boundEquipmentManager = null;
    }

    private void RefreshAll()
    {
        RefreshStatusList();
        RefreshAbilityLists();
        RefreshEquipmentViews();
        RefreshUtilitySpinRanges();
        RefreshDetailedStatsPanel();
        RefreshComponentTreePanel();
        RefreshFloatingStatsText();
        EnsurePartyStatusRow();
    }

    private void RefreshStatusList()
    {
        if (_activeStatusList == null) return;
        _activeStatusList.Clear();

        var manager = GetStatusManager(_character);
        if (manager == null)
        {
            _activeStatusList.AddItem("(No StatusEffectManager)");
            return;
        }

        var active = manager.GetActiveEffects();
        if (active == null || active.Count == 0)
        {
            _activeStatusList.AddItem("(No active statuses)");
            return;
        }

        foreach (var instance in active)
        {
            if (instance?.EffectData == null) continue;
            _activeStatusList.AddItem($"{instance.EffectData.EffectName} | turns={instance.RemainingTurns} | stacks={instance.Stacks}");
        }
    }

    private void RefreshAbilityLists()
    {
        if (_equippedAbilityList == null) return;
        _equippedAbilityList.Clear();

        var manager = GetAbilityManager(_character);
        if (manager == null)
        {
            _equippedAbilityList.AddItem("(No AbilityManager)");
            return;
        }

        var equipped = manager.GetEquippedAbilities();
        if (equipped == null || equipped.Count == 0)
        {
            _equippedAbilityList.AddItem("(No equipped abilities)");
            return;
        }

        foreach (var ability in equipped)
        {
            if (ability == null) continue;
            _equippedAbilityList.AddItem($"{ability.AbilityName} (AP {ability.ApCost})");
        }
    }

    private void RefreshEquipmentViews()
    {
        RefreshEquipmentSlotSelector();
        RefreshEquippedItemsList();
    }

    private void RefreshEquipmentSlotSelector()
    {
        _equipmentSlots.Clear();

        if (_equipmentSlotSelect == null) return;
        _equipmentSlotSelect.Clear();

        var manager = GetEquipmentManager(_character);
        if (manager == null)
        {
            _equipmentSlotSelect.AddItem("(No EquipmentManager)", -1);
            _equipmentSlotSelect.Selected = 0;
            return;
        }

        int i = 0;
        foreach (var slot in manager.GetSlots())
        {
            if (slot == null) continue;
            _equipmentSlots.Add(slot);
            _equipmentSlotSelect.AddItem($"{slot.Name} ({slot.SlotType})", i);
            i++;
        }

        if (_equipmentSlots.Count == 0)
        {
            _equipmentSlotSelect.AddItem("(No slots)", -1);
            _equipmentSlotSelect.Selected = 0;
        }
        else
        {
            _equipmentSlotSelect.Selected = 0;
        }
    }

    private void RefreshEquippedItemsList()
    {
        if (_equippedItemsList == null) return;
        _equippedItemsList.Clear();

        var manager = GetEquipmentManager(_character);
        if (manager == null)
        {
            _equippedItemsList.AddItem("(No EquipmentManager)");
            return;
        }

        foreach (var slot in manager.GetSlots())
        {
            if (slot == null) continue;
            string itemName = slot.EquippedItem?.ItemName ?? "(Empty)";
            _equippedItemsList.AddItem($"{slot.Name} [{slot.SlotType}] -> {itemName}");
        }
    }

    private void RefreshUtilitySpinRanges()
    {
        var stats = GetStats(_character);
        if (stats == null) return;

        int maxHp = Mathf.Max(1, stats.GetStatValue(StatType.HP));
        int maxMp = Mathf.Max(1, stats.GetStatValue(StatType.MP));

        if (_setHpSpin != null)
        {
            _setHpSpin.MaxValue = maxHp;
            _setHpSpin.Value = stats.CurrentHP;
        }

        if (_setMpSpin != null)
        {
            _setMpSpin.MaxValue = maxMp;
            _setMpSpin.Value = stats.CurrentMP;
        }
    }

    private void RefreshDetailedStatsPanel()
    {
        if (_detailedStatsLabel == null)
        {
            return;
        }

        if (_character == null || !GodotObject.IsInstanceValid(_character))
        {
            _detailedStatsLabel.Text = "No character spawned.";
            return;
        }

        var stats = GetStats(_character);
        if (stats == null)
        {
            _detailedStatsLabel.Text = "Character has no StatsComponent.";
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Character: {SafeName(_character)}");
        builder.AppendLine($"HP: {stats.CurrentHP}/{stats.GetStatValue(StatType.HP)}");
        builder.AppendLine($"MP: {stats.CurrentMP}/{stats.GetStatValue(StatType.MP)}");
        builder.AppendLine($"Charge: {_chargeSystem?.GetCharges(_character) ?? 0}");
        builder.AppendLine();
        builder.AppendLine("Stats:");

        foreach (StatType statType in Enum.GetValues<StatType>())
        {
            int baseValue = stats.GetBaseStatValue(statType);
            int finalValue = stats.GetStatValue(statType);
            int delta = finalValue - baseValue;
            string deltaText = delta == 0 ? string.Empty : delta > 0 ? $" (+{delta})" : $" ({delta})";
            builder.AppendLine($"- {statType,-12}: {finalValue}{deltaText} [base {baseValue}]");
        }

        var status = GetStatusManager(_character);
        builder.AppendLine();
        builder.AppendLine("Statuses:");
        if (status == null || status.GetActiveEffects().Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            foreach (var instance in status.GetActiveEffects())
            {
                if (instance?.EffectData == null) continue;
                builder.AppendLine($"- {instance.EffectData.EffectName} (turns={instance.RemainingTurns}, stacks={instance.Stacks})");
            }
        }

        var ability = GetAbilityManager(_character);
        builder.AppendLine();
        builder.AppendLine("Equipped Abilities:");
        if (ability == null || ability.GetEquippedAbilities().Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            foreach (var equipped in ability.GetEquippedAbilities())
            {
                if (equipped == null) continue;
                builder.AppendLine($"- {equipped.AbilityName} (AP {equipped.ApCost})");
            }
        }

        var equipment = GetEquipmentManager(_character);
        builder.AppendLine();
        builder.AppendLine("Equipment:");
        if (equipment == null)
        {
            builder.AppendLine("- no EquipmentManager");
        }
        else
        {
            foreach (var slot in equipment.GetSlots())
            {
                if (slot == null) continue;
                builder.AppendLine($"- {slot.Name} [{slot.SlotType}]: {slot.EquippedItem?.ItemName ?? "(Empty)"}");
            }
        }

        _detailedStatsLabel.Text = builder.ToString();
    }

    private void RefreshComponentTreePanel()
    {
        if (_componentTreeLabel == null) return;

        if (_character == null || !GodotObject.IsInstanceValid(_character))
        {
            _componentTreeLabel.Text = "No character spawned.";
            return;
        }

        var builder = new StringBuilder();
        AppendNodeTree(builder, _character, depth: 0, maxDepth: 5);
        _componentTreeLabel.Text = builder.ToString();
    }

    private static void AppendNodeTree(StringBuilder builder, Node node, int depth, int maxDepth)
    {
        if (builder == null || node == null) return;

        string indent = new string(' ', depth * 2);
        string extra = string.Empty;
        if (node is Sprite3D sprite3D)
        {
            extra = $" frame={sprite3D.Frame}";
        }
        else if (node is AnimatedSprite3D animatedSprite3D)
        {
            extra = $" anim={animatedSprite3D.Animation}";
        }
        else if (node is AnimationPlayer animationPlayer)
        {
            extra = $" anim={animationPlayer.CurrentAnimation} speed={animationPlayer.SpeedScale:0.##}";
        }

        builder.AppendLine($"{indent}- {node.Name} [{node.GetType().Name}]{extra}");

        if (depth >= maxDepth) return;
        foreach (Node child in node.GetChildren())
        {
            AppendNodeTree(builder, child, depth + 1, maxDepth);
        }
    }

    private void RefreshFloatingStatsText()
    {
        if (!ShowFloatingStatsOverlay)
        {
            if (_floatingStatsPanel != null)
            {
                _floatingStatsPanel.Visible = false;
            }
            return;
        }

        if (_floatingStatsLabel == null)
        {
            return;
        }

        var stats = GetStats(_character);
        if (stats == null)
        {
            _floatingStatsLabel.Text = "No Stats";
            return;
        }

        int statuses = GetStatusManager(_character)?.GetActiveEffects()?.Count ?? 0;
        int abilities = GetAbilityManager(_character)?.GetEquippedAbilities()?.Count ?? 0;

        _floatingStatsLabel.Text =
            $"{SafeName(_character)}\n" +
            $"HP {stats.CurrentHP}/{stats.GetStatValue(StatType.HP)}\n" +
            $"MP {stats.CurrentMP}/{stats.GetStatValue(StatType.MP)}\n" +
            $"Charge {_chargeSystem?.GetCharges(_character) ?? 0}\n" +
            $"Status {statuses} | Ability {abilities}";
    }

    private void UpdateFloatingStatsPanelPosition()
    {
        if (_floatingStatsPanel == null)
        {
            return;
        }
        if (!ShowFloatingStatsOverlay)
        {
            _floatingStatsPanel.Visible = false;
            return;
        }

        if (_character is not Node3D character3D || !GodotObject.IsInstanceValid(character3D))
        {
            _floatingStatsPanel.Visible = false;
            return;
        }

        var camera = GetViewport().GetCamera3D();
        if (camera == null)
        {
            _floatingStatsPanel.Visible = false;
            return;
        }

        Vector3 worldPoint = character3D.GlobalPosition + new Vector3(0f, 2.25f, 0f);
        if (camera.IsPositionBehind(worldPoint))
        {
            _floatingStatsPanel.Visible = false;
            return;
        }

        Vector2 screenPoint = camera.UnprojectPosition(worldPoint);
        Vector2 desired = screenPoint + new Vector2(24f, -10f);
        Vector2 viewport = GetViewportRect().Size;
        Vector2 panelSize = _floatingStatsPanel.Size;
        float x = Mathf.Clamp(desired.X, 0f, Mathf.Max(0f, viewport.X - panelSize.X));
        float y = Mathf.Clamp(desired.Y, 0f, Mathf.Max(0f, viewport.Y - panelSize.Y));
        _floatingStatsPanel.GlobalPosition = new Vector2(x, y);
        _floatingStatsPanel.Visible = true;
    }

    private void PositionPreviewCameraImmediate()
    {
        if (_characterPreviewCamera == null) return;
        if (_character is not Node3D character3D || !GodotObject.IsInstanceValid(character3D)) return;

        Vector3 focusPoint = character3D.GlobalPosition + new Vector3(0f, 1.15f, 0f);
        _characterPreviewCamera.GlobalPosition = focusPoint + new Vector3(0f, 0.95f, 5.3f);
        _characterPreviewCamera.LookAt(focusPoint, Vector3.Up);
    }

    private void EnsureSandboxCameraCurrent()
    {
        if (_sandboxCamera == null) return;
        if (!_sandboxCamera.Current)
        {
            _sandboxCamera.MakeCurrent();
        }
    }

    private void ClampFocusCameraDistance()
    {
        _focusCameraDistanceCurrent = Mathf.Clamp(
            _focusCameraDistanceCurrent,
            Mathf.Max(0.25f, FocusCameraMinDistance),
            Mathf.Max(FocusCameraMinDistance, FocusCameraMaxDistance));
    }

    private void AdjustFocusCameraDistance(float delta)
    {
        _focusCameraDistanceCurrent += delta;
        ClampFocusCameraDistance();
        PositionFocusCameraImmediate();
    }

    private void PositionFocusCameraImmediate()
    {
        if (_sandboxCamera == null) return;
        if (_character is not Node3D character3D || !GodotObject.IsInstanceValid(character3D)) return;

        Vector3 focusPoint = character3D.GlobalPosition + new Vector3(0f, FocusCameraLookHeight, 0f);
        Vector3 desiredCamera = focusPoint + new Vector3(0f, FocusCameraHeightOffset, _focusCameraDistanceCurrent);
        _sandboxCamera.GlobalPosition = desiredCamera;
        _sandboxCamera.LookAt(focusPoint, Vector3.Up);
    }

    private void UpdateFocusCameraFollow()
    {
        if (!FocusCameraFollowEnabled) return;
        if (_sandboxCamera == null) return;
        if (_character is not Node3D character3D || !GodotObject.IsInstanceValid(character3D)) return;

        Vector3 focusPoint = character3D.GlobalPosition + new Vector3(0f, FocusCameraLookHeight, 0f);
        Vector3 desiredCamera = focusPoint + new Vector3(0f, FocusCameraHeightOffset, _focusCameraDistanceCurrent);
        float follow = Mathf.Clamp(FocusCameraFollowLerp, 0.01f, 1f);
        if (follow >= 0.999f)
        {
            _sandboxCamera.GlobalPosition = desiredCamera;
        }
        else
        {
            _sandboxCamera.GlobalPosition = _sandboxCamera.GlobalPosition.Lerp(desiredCamera, follow);
        }
        _sandboxCamera.LookAt(focusPoint, Vector3.Up);
    }

    private void UpdatePreviewCameraFollow()
    {
        if (_characterPreviewWindow == null || !_characterPreviewWindow.Visible) return;
        if (_characterPreviewCamera == null) return;
        if (_character is not Node3D character3D || !GodotObject.IsInstanceValid(character3D)) return;

        Vector3 focusPoint = character3D.GlobalPosition + new Vector3(0f, 1.15f, 0f);
        Vector3 desiredCamera = focusPoint + new Vector3(0f, 0.95f, 5.3f);
        _characterPreviewCamera.GlobalPosition = _characterPreviewCamera.GlobalPosition.Lerp(desiredCamera, 0.15f);
        _characterPreviewCamera.LookAt(focusPoint, Vector3.Up);
    }

    private void ApplySelectedStatus()
    {
        var manager = GetStatusManager(_character);
        var effect = GetSelectedStatus();
        if (manager == null || effect == null)
        {
            Log("Apply status failed: missing status manager or selection.");
            return;
        }

        bool applied = manager.TryApplyEffect(effect, null, 100f, _rng);
        Log(applied
            ? $"Applied status '{effect.EffectName}'."
            : $"Status '{effect.EffectName}' did not apply.");

        if (effect.VisualEffects != null && effect.VisualEffects.Count > 0)
        {
            var visualTypes = effect.VisualEffects
                .Select(v => v?.GetType().Name ?? "(null)")
                .ToArray();
            Log($"Status visual effects: {effect.VisualEffects.Count} [{string.Join(", ", visualTypes)}]");
        }

        var visualController = _character?.GetNodeOrNull<CharacterVisualStateController>(CharacterVisualStateController.NodeName);
        visualController?.RefreshAllVisualState(playIdleIfPossible: true);
        if (applied && effect is MirrorImagesStatusEffect)
        {
            Log($"Mirror ghost sprites active: {visualController?.GetActiveMirrorGhostCount() ?? 0}");
        }
        RefreshAll();
    }

    private void RemoveSelectedStatus()
    {
        var manager = GetStatusManager(_character);
        var effect = GetSelectedStatus();
        if (manager == null || effect == null)
        {
            Log("Remove status failed: missing status manager or selection.");
            return;
        }

        bool removed = manager.RemoveEffect(effect, null);
        Log(removed
            ? $"Removed status '{effect.EffectName}'."
            : $"Status '{effect.EffectName}' not active.");

        _character?.GetNodeOrNull<CharacterVisualStateController>(CharacterVisualStateController.NodeName)
            ?.RefreshAllVisualState(playIdleIfPossible: true);
        RefreshAll();
    }

    private void ClearStatuses()
    {
        var manager = GetStatusManager(_character);
        if (manager == null)
        {
            Log("Clear statuses failed: no StatusEffectManager.");
            return;
        }

        var active = manager.GetActiveEffects().ToList();
        foreach (var instance in active)
        {
            manager.RemoveEffect(instance, null);
        }

        Log("Cleared all statuses.");
        _character?.GetNodeOrNull<CharacterVisualStateController>(CharacterVisualStateController.NodeName)
            ?.RefreshAllVisualState(playIdleIfPossible: true);
        RefreshAll();
    }

    private void EquipSelectedAbility()
    {
        var manager = GetAbilityManager(_character);
        var ability = GetSelectedAbility();
        if (manager == null || ability == null)
        {
            Log("Equip ability failed: missing AbilityManager or selection.");
            return;
        }

        manager.LearnAbility(ability);
        bool equipped = manager.EquipAbility(ability);
        Log(equipped
            ? $"Equipped ability '{ability.AbilityName}'."
            : $"Could not equip '{ability.AbilityName}'.");

        RefreshAll();
    }

    private void UnequipSelectedAbility()
    {
        var manager = GetAbilityManager(_character);
        var ability = GetSelectedAbility();
        if (manager == null || ability == null)
        {
            Log("Unequip ability failed: missing AbilityManager or selection.");
            return;
        }

        manager.UnequipAbility(ability);
        Log($"Unequipped ability '{ability.AbilityName}'.");
        RefreshAll();
    }

    private void UnequipAllAbilities()
    {
        var manager = GetAbilityManager(_character);
        if (manager == null)
        {
            Log("Unequip all failed: no AbilityManager.");
            return;
        }

        var equipped = manager.GetEquippedAbilities().ToList();
        foreach (var ability in equipped)
        {
            if (ability == null) continue;
            manager.UnequipAbility(ability);
        }

        Log("Unequipped all abilities.");
        RefreshAll();
    }

    private void TriggerSelectedAbility()
    {
        var manager = GetAbilityManager(_character);
        AbilityTrigger trigger = GetSelectedAbilityTrigger();
        if (manager == null || trigger == AbilityTrigger.None)
        {
            Log("Trigger ability failed: missing manager or trigger.");
            return;
        }

        var context = new AbilityEffectContext(_character, trigger);
        manager.ApplyTrigger(trigger, context);

        Log($"Triggered equipped abilities on {trigger}.");
        RefreshAll();
    }

    private void EquipSelectedItemToSlot()
    {
        var manager = GetEquipmentManager(_character);
        var slot = GetSelectedEquipmentSlot();
        var item = GetSelectedEquipmentItem();

        if (manager == null || slot == null || item == null)
        {
            Log("Equip item failed: missing equipment manager, slot, or item selection.");
            return;
        }

        if (!TryGetEquippableComponent(item, out var equippable))
        {
            Log($"Equip item failed: '{item.ItemName}' is not equippable.");
            return;
        }

        if (equippable.SlotType != slot.SlotType)
        {
            Log($"Equip item failed: '{item.ItemName}' is {equippable.SlotType} but slot is {slot.SlotType}.");
            return;
        }

        manager.EquipItem(item, slot);
        if (slot.EquippedItem == item)
        {
            Log($"Equipped '{item.ItemName}' to slot '{slot.Name}'.");
        }
        else
        {
            Log($"Equip item failed for '{item.ItemName}' -> '{slot.Name}'.");
        }

        RefreshAll();
    }

    private void UnequipSelectedSlot()
    {
        var manager = GetEquipmentManager(_character);
        var slot = GetSelectedEquipmentSlot();

        if (manager == null || slot == null)
        {
            Log("Unequip slot failed: missing equipment manager or slot.");
            return;
        }

        var removed = manager.UnequipItem(slot);
        Log(removed != null
            ? $"Unequipped '{removed.ItemName}' from slot '{slot.Name}'."
            : $"Slot '{slot.Name}' was already empty.");

        RefreshAll();
    }

    private void SetHp()
    {
        var stats = GetStats(_character);
        if (stats == null) return;

        int desired = Mathf.Clamp((int)(_setHpSpin?.Value ?? 0.0), 0, stats.GetStatValue(StatType.HP));
        stats.ModifyCurrentHP(desired - stats.CurrentHP);
        Log($"Set HP to {desired}.");
        RefreshAll();
    }

    private void SetMp()
    {
        var stats = GetStats(_character);
        if (stats == null) return;

        int desired = Mathf.Clamp((int)(_setMpSpin?.Value ?? 0.0), 0, stats.GetStatValue(StatType.MP));
        stats.ModifyCurrentMP(desired - stats.CurrentMP);
        Log($"Set MP to {desired}.");
        RefreshAll();
    }

    private void ApplyDamage()
    {
        var stats = GetStats(_character);
        if (stats == null) return;

        int amount = Mathf.Max(1, (int)(_deltaSpin?.Value ?? 1.0));
        stats.ModifyCurrentHP(-amount);
        Log($"Applied direct damage {amount}.");
        RefreshAll();
    }

    private void ApplyHealing()
    {
        var stats = GetStats(_character);
        if (stats == null) return;

        int amount = Mathf.Max(1, (int)(_deltaSpin?.Value ?? 1.0));
        stats.ModifyCurrentHP(amount);
        Log($"Applied direct healing {amount}.");
        RefreshAll();
    }

    private void GainMp()
    {
        var stats = GetStats(_character);
        if (stats == null) return;

        int amount = Mathf.Max(1, (int)(_deltaSpin?.Value ?? 1.0));
        int adjusted = GetStatusManager(_character)?.ModifyIncomingMpRestore(amount) ?? amount;
        stats.ModifyCurrentMP(adjusted);
        Log($"Applied MP gain {amount} (adjusted {adjusted}).");
        RefreshAll();
    }

    private void SpendMp()
    {
        var stats = GetStats(_character);
        if (stats == null) return;

        int amount = Mathf.Max(1, (int)(_deltaSpin?.Value ?? 1.0));
        stats.ModifyCurrentMP(-amount);
        Log($"Spent MP {amount}.");
        RefreshAll();
    }

    private void AddCharge()
    {
        if (_character == null || _chargeSystem == null) return;
        int amount = Mathf.Max(1, (int)(_chargeSpin?.Value ?? 1.0));

        _chargeSystem.AddCharges(_character, amount);
        Log($"Added {amount} charge.");
        RefreshAll();
    }

    private void SpendCharge()
    {
        if (_character == null || _chargeSystem == null) return;
        int amount = Mathf.Max(1, (int)(_chargeSpin?.Value ?? 1.0));

        bool ok = _chargeSystem.TrySpendCharges(_character, amount);
        Log(ok ? $"Spent {amount} charge." : $"Could not spend {amount} charge.");
        RefreshAll();
    }

    private void SetCharge()
    {
        if (_character == null || _chargeSystem == null) return;

        int desired = Mathf.Max(0, (int)(_chargeSpin?.Value ?? 0.0));
        int current = _chargeSystem.GetCharges(_character);

        if (desired > current)
        {
            _chargeSystem.AddCharges(_character, desired - current);
        }
        else if (desired < current)
        {
            _chargeSystem.TrySpendCharges(_character, current - desired);
        }

        Log($"Set charge to {desired}.");
        RefreshAll();
    }

    private void SimulateTurnStart()
    {
        var status = GetStatusManager(_character);
        status?.OnTurnStart(null);

        var ability = GetAbilityManager(_character);
        ability?.ApplyTrigger(AbilityTrigger.TurnStart, new AbilityEffectContext(_character, AbilityTrigger.TurnStart));

        Log("Simulated Turn Start.");
        RefreshAll();
    }

    private void SimulateTurnEnd()
    {
        var status = GetStatusManager(_character);
        status?.OnTurnEnd(null);

        var ability = GetAbilityManager(_character);
        ability?.ApplyTrigger(AbilityTrigger.TurnEnd, new AbilityEffectContext(_character, AbilityTrigger.TurnEnd));

        Log("Simulated Turn End.");
        RefreshAll();
    }

    private StatusEffect GetSelectedStatus()
    {
        int index = _statusSelect?.Selected ?? -1;
        if (index < 0 || index >= _statusOptions.Count) return null;
        return _statusOptions[index];
    }

    private Ability GetSelectedAbility()
    {
        int index = _abilitySelect?.Selected ?? -1;
        if (index < 0 || index >= _abilityOptions.Count) return null;
        return _abilityOptions[index];
    }

    private AbilityTrigger GetSelectedAbilityTrigger()
    {
        int index = _abilityTriggerSelect?.Selected ?? -1;
        if (index < 0 || index >= _abilityTriggers.Count) return AbilityTrigger.None;
        return _abilityTriggers[index];
    }

    private ItemData GetSelectedEquipmentItem()
    {
        int index = _equipmentItemSelect?.Selected ?? -1;
        if (index < 0 || index >= _equipmentItemOptions.Count) return null;
        return _equipmentItemOptions[index];
    }

    private EquipmentSlot GetSelectedEquipmentSlot()
    {
        int index = _equipmentSlotSelect?.Selected ?? -1;
        if (index < 0 || index >= _equipmentSlots.Count) return null;
        return _equipmentSlots[index];
    }

    private static bool TryGetEquippableComponent(ItemData item, out EquippableComponentData equippable)
    {
        equippable = null;
        if (item?.Components == null) return false;

        foreach (var component in item.Components)
        {
            if (component is EquippableComponentData data)
            {
                equippable = data;
                return true;
            }
        }

        return false;
    }

    private static List<ItemData> CreateDebugEquipmentItems()
    {
        return new List<ItemData>
        {
            CreateDebugEquippable("Debug Blade", EquipmentSlotType.Weapon,
                new StatModifier(StatType.Strength, 18, ModifierType.Additive, null),
                new StatModifier(StatType.Speed, 1, ModifierType.Additive, null)),
            CreateDebugEquippable("Debug Shield", EquipmentSlotType.Shield,
                new StatModifier(StatType.Defense, 16, ModifierType.Additive, null),
                new StatModifier(StatType.MagicDefense, 8, ModifierType.Additive, null)),
            CreateDebugEquippable("Debug Circlet", EquipmentSlotType.Head,
                new StatModifier(StatType.Magic, 14, ModifierType.Additive, null),
                new StatModifier(StatType.MP, 60, ModifierType.Additive, null)),
            CreateDebugEquippable("Debug Armor", EquipmentSlotType.Body,
                new StatModifier(StatType.HP, 180, ModifierType.Additive, null),
                new StatModifier(StatType.Defense, 10, ModifierType.Additive, null)),
            CreateDebugEquippable("Debug Ring", EquipmentSlotType.Accessory,
                new StatModifier(StatType.Luck, 9, ModifierType.Additive, null),
                new StatModifier(StatType.Speed, 2, ModifierType.Additive, null))
        };
    }

    private static ItemData CreateDebugEquippable(string itemName, EquipmentSlotType slotType, params StatModifier[] modifiers)
    {
        var item = new ItemData
        {
            ItemName = itemName,
            Description = $"Sandbox debug item for {slotType}."
        };

        var equippable = new EquippableComponentData
        {
            SlotType = slotType
        };

        if (modifiers != null)
        {
            foreach (var modifier in modifiers)
            {
                if (modifier == null) continue;
                modifier.Source = item;
                equippable.StatBoosts.Add(modifier);
            }
        }

        item.Components.Add(equippable);
        return item;
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

    private static StatsComponent GetStats(Node character)
    {
        return character?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
    }

    private static StatusEffectManager GetStatusManager(Node character)
    {
        return character?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
    }

    private static AbilityManager GetAbilityManager(Node character)
    {
        return character?.GetNodeOrNull<AbilityManager>(AbilityManager.NodeName);
    }

    private static EquipmentManager GetEquipmentManager(Node character)
    {
        return character?.GetNodeOrNull<EquipmentManager>("EquipmentManager");
    }

    private static string SafeName(Node node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node)) return "(none)";
        return node.Name;
    }

    private void OnHpChanged(int newHp, int maxHp)
    {
        RefreshAll();
    }

    private void OnMpChanged(int newMp, int maxMp)
    {
        RefreshAll();
    }

    private void OnStatValueChanged(long statType, int newValue)
    {
        RefreshAll();
    }

    private void OnStatusChanged(StatusEffect effect, Node owner)
    {
        RefreshAll();
    }

    private void OnAbilityListChanged()
    {
        RefreshAll();
    }

    private void OnEquipmentChanged(EquipmentSlot slot, ItemData newItem)
    {
        RefreshAll();
    }

    private void OnEquipmentSlotAdded(EquipmentSlot slot)
    {
        RefreshAll();
    }

    private void OnChargesChanged(Node character, int newValue, int delta)
    {
        if (character != _character) return;
        RefreshAll();
    }
}
