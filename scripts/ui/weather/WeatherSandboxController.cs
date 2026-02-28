using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Sandbox scene controller for weather visualization and battle-side weather tuning.
/// </summary>
public partial class WeatherSandboxController : Control
{
    [ExportGroup("Weather")]
    [Export]
    public Godot.Collections.Array<WeatherProfile> WeatherProfiles { get; set; } = new();

    [ExportGroup("Characters")]
    [Export]
    public PackedScene DefaultPlayerScene { get; set; }

    [Export]
    public PackedScene DefaultEnemyScene { get; set; }

    [Export]
    public BaseStats FallbackBaseStats { get; set; }

    [ExportGroup("Camera Controls")]
    [Export(PropertyHint.Range, "0.05,4,0.01")]
    public float ScrollZoomStep { get; set; } = 0.85f;

    [Export(PropertyHint.Range, "0.5,120,0.1")]
    public float CameraMinDistance { get; set; } = 6.5f;

    [Export(PropertyHint.Range, "1,160,0.1")]
    public float CameraMaxDistance { get; set; } = 28f;

    [Export(PropertyHint.Range, "-3,8,0.01")]
    public float CameraFocusHeight { get; set; } = 1.1f;

    private Node3D _playerAnchor;
    private Node3D _enemyAnchor;
    private Camera3D _sandboxCamera;
    private WeatherSystem _weatherSystem;
    private BattleMechanics _battleMechanics;

    private OptionButton _weatherSelect;
    private Button _applyWeatherButton;
    private Button _clearWeatherButton;
    private Button _toggleUiButton;
    private HSlider _densitySlider;
    private Label _densityValueLabel;
    private HSlider _windSlider;
    private Label _windValueLabel;
    private HSlider _depthSlider;
    private Label _depthValueLabel;
    private HSlider _nearCullSlider;
    private Label _nearCullValueLabel;
    private HSlider _particleWidthSlider;
    private Label _particleWidthValueLabel;
    private HSlider _particleLengthSlider;
    private Label _particleLengthValueLabel;
    private HSlider _particleAlphaSlider;
    private Label _particleAlphaValueLabel;
    private HSlider _fallSpeedSlider;
    private Label _fallSpeedValueLabel;
    private HSlider _emissionHeightSlider;
    private Label _emissionHeightValueLabel;
    private HSlider _tintStrengthSlider;
    private Label _tintStrengthValueLabel;
    private HSlider _lightEnergySlider;
    private Label _lightEnergyValueLabel;
    private HSlider _glowSlider;
    private Label _glowValueLabel;
    private HSlider _lightningIntervalSlider;
    private Label _lightningIntervalValueLabel;
    private HSlider _lightningIntensitySlider;
    private Label _lightningIntensityValueLabel;
    private Button _resetTuningButton;
    private Button _simulateTurnButton;

    private Button _respawnButton;
    private OptionButton _sampleActionSelect;
    private Button _runSampleActionButton;
    private Control _uiHudPanel;
    private Control _summaryPanel;
    private Control _logPanel;
    private Label _summaryLabel;
    private RichTextLabel _logLabel;

    private Node _actor;
    private Node _target;
    private readonly List<WeatherProfile> _weatherOptions = new();
    private readonly List<ActionData> _sampleActions = new();
    private float _cameraDistance = 12f;
    private Vector3 _cameraDirection = new Vector3(0f, 0.22f, 0.98f);
    private ulong _lastTuneLogMs;
    private bool _suppressTuningLogs;
    private bool _tuningUiHidden;

    private const string PlayerSceneFallback = "res://assets/resources/characters/ceira/ceira.tscn";
    private const string EnemySceneFallback = "res://assets/resources/characters/goblin_lv5/goblin_lv5_animated.tscn";
    private const string FallbackStatsPath = "res://assets/resources/characters/ceira/ceira_starting_stats.tres";
    private const string ActionAttackPath = "res://assets/resources/actions/Attack.tres";
    private const string ActionFirePath = "res://assets/resources/actions/Magic/Fire.tres";
    private const string ActionWaterPath = "res://assets/resources/actions/Magic/Water.tres";
    private const string ActionThunderPath = "res://assets/resources/actions/Magic/Thunder.tres";

    public override void _Ready()
    {
        CacheNodes();
        EnsureFallbackAssets();
        EnsureBattleMechanicsDefaults();
        LoadWeatherOptions();
        LoadSampleActions();
        PopulateWeatherSelector();
        PopulateSampleActionSelector();
        WireUiEvents();
        SetTuningUiVisibility(false);
        RespawnCombatants();
        InitializeCameraZoom();
        ApplySelectedWeather();
        if (_weatherSystem?.ActiveWeather == null)
        {
            UpdateTuningUiForWeather(null);
        }
        RefreshSummary();
        Log("Weather sandbox ready.");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.F1)
        {
            ToggleTuningUiVisibility();
            GetViewport()?.SetInputAsHandled();
            return;
        }

        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
        {
            return;
        }

        if (mouseButton.ButtonIndex != MouseButton.WheelUp && mouseButton.ButtonIndex != MouseButton.WheelDown)
        {
            return;
        }

        var hoveredControl = GetViewport()?.GuiGetHoveredControl();
        if (hoveredControl != null && (hoveredControl == this || IsAncestorOf(hoveredControl)))
        {
            return;
        }

        float delta = mouseButton.ButtonIndex == MouseButton.WheelUp ? -ScrollZoomStep : ScrollZoomStep;
        AdjustCameraZoom(delta);
        GetViewport()?.SetInputAsHandled();
    }

    private void CacheNodes()
    {
        _playerAnchor = GetNodeOrNull<Node3D>("SandboxWorld/PlayerAnchor");
        _enemyAnchor = GetNodeOrNull<Node3D>("SandboxWorld/EnemyAnchor");
        _sandboxCamera = GetNodeOrNull<Camera3D>("SandboxWorld/Camera3D");
        _weatherSystem = GetNodeOrNull<WeatherSystem>("SandboxWorld/WeatherSystem");
        _battleMechanics = GetNodeOrNull<BattleMechanics>("BattleMechanics");

        _weatherSelect = ResolveNode<OptionButton>(
            "UI/HUD/Body/WeatherRow/WeatherSelect",
            "UI/MainRow/LeftPanel/WeatherPanel/Body/WeatherSelect");
        _applyWeatherButton = ResolveNode<Button>(
            "UI/HUD/Body/WeatherRow/ApplyWeatherButton",
            "UI/MainRow/LeftPanel/WeatherPanel/Body/WeatherButtonRow/ApplyWeatherButton");
        _clearWeatherButton = ResolveNode<Button>(
            "UI/HUD/Body/WeatherRow/ClearWeatherButton",
            "UI/MainRow/LeftPanel/WeatherPanel/Body/WeatherButtonRow/ClearWeatherButton");
        _toggleUiButton = ResolveNode<Button>(
            "UI/ToggleUiButton");
        _uiHudPanel = ResolveNode<Control>(
            "UI/HUD");
        _summaryPanel = ResolveNode<Control>(
            "UI/SummaryPanel");
        _logPanel = ResolveNode<Control>(
            "UI/LogPanel");
        _densitySlider = ResolveNode<HSlider>(
            "UI/HUD/Body/DensityRow/DensitySlider",
            "UI/MainRow/LeftPanel/WeatherPanel/Body/DensityRow/DensitySlider");
        _densityValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/DensityRow/DensityValue",
            "UI/MainRow/LeftPanel/WeatherPanel/Body/DensityRow/DensityValue");
        _windSlider = ResolveNode<HSlider>(
            "UI/HUD/Body/WindRow/WindSlider",
            "UI/MainRow/LeftPanel/WeatherPanel/Body/WindRow/WindSlider");
        _windValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/WindRow/WindValue",
            "UI/MainRow/LeftPanel/WeatherPanel/Body/WindRow/WindValue");
        _depthSlider = ResolveNode<HSlider>(
            "UI/HUD/Body/DepthRow/DepthSlider");
        _depthValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/DepthRow/DepthValue");
        _nearCullSlider = ResolveNode<HSlider>(
            "UI/HUD/Body/NearCullRow/NearCullSlider");
        _nearCullValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/NearCullRow/NearCullValue");
        _particleWidthSlider = ResolveNode<HSlider>(
            "UI/HUD/Body/WidthRow/WidthSlider");
        _particleWidthValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/WidthRow/WidthValue");
        _particleLengthSlider = ResolveNode<HSlider>(
            "UI/HUD/Body/LengthRow/LengthSlider");
        _particleLengthValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/LengthRow/LengthValue");
        _particleAlphaSlider = ResolveNode<HSlider>(
            "UI/HUD/Body/AlphaRow/AlphaSlider");
        _particleAlphaValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/AlphaRow/AlphaValue");
        _fallSpeedSlider = ResolveNode<HSlider>(
            "UI/HUD/Body/FallSpeedRow/FallSpeedSlider");
        _fallSpeedValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/FallSpeedRow/FallSpeedValue");
        _emissionHeightSlider = ResolveNode<HSlider>(
            "UI/HUD/Body/EmissionHeightRow/EmissionHeightSlider");
        _emissionHeightValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/EmissionHeightRow/EmissionHeightValue");
        _tintStrengthSlider = ResolveNode<HSlider>(
            "UI/HUD/Body/TintRow/TintSlider");
        _tintStrengthValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/TintRow/TintValue");
        _lightEnergySlider = ResolveNode<HSlider>(
            "UI/HUD/Body/LightEnergyRow/LightEnergySlider");
        _lightEnergyValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/LightEnergyRow/LightEnergyValue");
        _glowSlider = ResolveNode<HSlider>(
            "UI/HUD/Body/GlowRow/GlowSlider");
        _glowValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/GlowRow/GlowValue");
        _lightningIntervalSlider = ResolveNode<HSlider>(
            "UI/HUD/Body/LightningRateRow/LightningRateSlider");
        _lightningIntervalValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/LightningRateRow/LightningRateValue");
        _lightningIntensitySlider = ResolveNode<HSlider>(
            "UI/HUD/Body/LightningPowerRow/LightningPowerSlider");
        _lightningIntensityValueLabel = ResolveNode<Label>(
            "UI/HUD/Body/LightningPowerRow/LightningPowerValue");
        _resetTuningButton = ResolveNode<Button>(
            "UI/HUD/Body/WeatherRow/ResetTuningButton");
        _simulateTurnButton = ResolveNode<Button>(
            "UI/HUD/Body/ActionRow/SimulateTurnButton",
            "UI/MainRow/LeftPanel/WeatherPanel/Body/SimulateTurnButton");

        _respawnButton = ResolveNode<Button>(
            "UI/HUD/Body/ActionRow/RespawnButton",
            "UI/MainRow/LeftPanel/BattlePanel/Body/RespawnButton");
        _sampleActionSelect = ResolveNode<OptionButton>(
            "UI/HUD/Body/ActionRow/SampleActionSelect",
            "UI/MainRow/LeftPanel/BattlePanel/Body/SampleActionSelect");
        _runSampleActionButton = ResolveNode<Button>(
            "UI/HUD/Body/ActionRow/RunSampleActionButton",
            "UI/MainRow/LeftPanel/BattlePanel/Body/RunSampleActionButton");
        _summaryLabel = ResolveNode<Label>(
            "UI/SummaryPanel/SummaryLabel",
            "UI/MainRow/RightPanel/SummaryPanel/Body/SummaryLabel");
        _logLabel = ResolveNode<RichTextLabel>(
            "UI/LogPanel/LogLabel",
            "UI/MainRow/RightPanel/LogPanel/Body/LogLabel");
    }

    private void InitializeCameraZoom()
    {
        if (_sandboxCamera == null) return;

        Vector3 focusPoint = GetCameraFocusPoint();
        Vector3 offset = _sandboxCamera.GlobalPosition - focusPoint;
        if (offset.LengthSquared() <= 0.0001f)
        {
            offset = new Vector3(0f, 2.8f, 11.5f);
        }

        _cameraDirection = offset.Normalized();
        _cameraDistance = offset.Length();
        ClampCameraDistance();
        ApplyCameraZoomPosition();
    }

    private void ClampCameraDistance()
    {
        float minDistance = Mathf.Max(0.5f, CameraMinDistance);
        float maxDistance = Mathf.Max(minDistance, CameraMaxDistance);
        _cameraDistance = Mathf.Clamp(_cameraDistance, minDistance, maxDistance);
    }

    private void AdjustCameraZoom(float delta)
    {
        if (_sandboxCamera == null) return;
        _cameraDistance += delta;
        ClampCameraDistance();
        ApplyCameraZoomPosition();
    }

    private void ApplyCameraZoomPosition()
    {
        if (_sandboxCamera == null) return;

        Vector3 focusPoint = GetCameraFocusPoint();
        _sandboxCamera.GlobalPosition = focusPoint + (_cameraDirection * _cameraDistance);
        _sandboxCamera.LookAt(focusPoint, Vector3.Up);
    }

    private Vector3 GetCameraFocusPoint()
    {
        Vector3 total = Vector3.Zero;
        int count = 0;

        if (_actor is Node3D actor3D)
        {
            total += actor3D.GlobalPosition;
            count++;
        }

        if (_target is Node3D target3D)
        {
            total += target3D.GlobalPosition;
            count++;
        }

        if (count == 0 && _playerAnchor != null)
        {
            total += _playerAnchor.GlobalPosition;
            count++;
        }

        if (count == 0 && _enemyAnchor != null)
        {
            total += _enemyAnchor.GlobalPosition;
            count++;
        }

        if (count == 0 && _sandboxCamera != null)
        {
            return _sandboxCamera.GlobalPosition + (-_sandboxCamera.GlobalBasis.Z * 10f);
        }

        Vector3 focus = total / Mathf.Max(1, count);
        focus.Y += CameraFocusHeight;
        return focus;
    }

    private T ResolveNode<T>(params string[] paths) where T : Node
    {
        if (paths == null) return null;
        foreach (var path in paths)
        {
            if (string.IsNullOrEmpty(path)) continue;
            var node = GetNodeOrNull<T>(path);
            if (node != null) return node;
        }
        return null;
    }

    private void EnsureFallbackAssets()
    {
        DefaultPlayerScene ??= GD.Load<PackedScene>(PlayerSceneFallback);
        DefaultEnemyScene ??= GD.Load<PackedScene>(EnemySceneFallback);
        FallbackBaseStats ??= GD.Load<BaseStats>(FallbackStatsPath);
    }

    private void EnsureBattleMechanicsDefaults()
    {
        if (_battleMechanics == null) return;

        var strategy = new CalculationStrategy();
        strategy.Set("HitLogic", new StandardHitStrategy());
        strategy.Set("CritLogic", new StandardCritStrategy());
        strategy.Set("DamageLogic", new StandardDamageStrategy());
        _battleMechanics.Set("_defaultStrategy", strategy);
    }

    private void LoadWeatherOptions()
    {
        _weatherOptions.Clear();
        if (WeatherProfiles != null)
        {
            foreach (var profile in WeatherProfiles)
            {
                if (profile != null) _weatherOptions.Add(profile);
            }
        }
    }

    private void LoadSampleActions()
    {
        _sampleActions.Clear();

        var paths = new[]
        {
            ActionAttackPath,
            ActionFirePath,
            ActionWaterPath,
            ActionThunderPath
        };

        foreach (var path in paths)
        {
            var action = GD.Load<ActionData>(path);
            if (action != null)
            {
                _sampleActions.Add(action);
            }
        }
    }

    private void PopulateWeatherSelector()
    {
        if (_weatherSelect == null) return;
        _weatherSelect.Clear();

        if (_weatherOptions.Count == 0)
        {
            _weatherSelect.AddItem("(no weather profiles)");
            _weatherSelect.Disabled = true;
            return;
        }

        for (int i = 0; i < _weatherOptions.Count; i++)
        {
            var profile = _weatherOptions[i];
            string label = string.IsNullOrEmpty(profile.WeatherName) ? $"Weather {i + 1}" : profile.WeatherName;
            _weatherSelect.AddItem(label, i);
        }
        _weatherSelect.Selected = 0;
    }

    private void PopulateSampleActionSelector()
    {
        if (_sampleActionSelect == null) return;
        _sampleActionSelect.Clear();

        if (_sampleActions.Count == 0)
        {
            _sampleActionSelect.AddItem("(no actions)");
            _sampleActionSelect.Disabled = true;
            return;
        }

        for (int i = 0; i < _sampleActions.Count; i++)
        {
            var action = _sampleActions[i];
            _sampleActionSelect.AddItem(action?.CommandName ?? $"Action {i + 1}", i);
        }
        _sampleActionSelect.Selected = 0;
    }

    private void WireUiEvents()
    {
        _applyWeatherButton?.Connect(Button.SignalName.Pressed, Callable.From(ApplySelectedWeather));
        _clearWeatherButton?.Connect(Button.SignalName.Pressed, Callable.From(ClearWeather));
        _toggleUiButton?.Connect(Button.SignalName.Pressed, Callable.From(ToggleTuningUiVisibility));
        _resetTuningButton?.Connect(Button.SignalName.Pressed, Callable.From(ResetTuning));
        _simulateTurnButton?.Connect(Button.SignalName.Pressed, Callable.From(SimulateTurnTick));

        _respawnButton?.Connect(Button.SignalName.Pressed, Callable.From(RespawnCombatants));
        _runSampleActionButton?.Connect(Button.SignalName.Pressed, Callable.From(RunSampleAction));

        if (_densitySlider != null)
        {
            _densitySlider.ValueChanged += OnDensitySliderChanged;
            OnDensitySliderChanged(_densitySlider.Value);
        }

        if (_windSlider != null)
        {
            _windSlider.ValueChanged += OnWindSliderChanged;
            OnWindSliderChanged(_windSlider.Value);
        }

        if (_depthSlider != null)
        {
            _depthSlider.ValueChanged += OnDepthSliderChanged;
            OnDepthSliderChanged(_depthSlider.Value);
        }

        if (_nearCullSlider != null)
        {
            _nearCullSlider.ValueChanged += OnNearCullSliderChanged;
            OnNearCullSliderChanged(_nearCullSlider.Value);
        }

        if (_particleWidthSlider != null)
        {
            _particleWidthSlider.ValueChanged += OnParticleWidthSliderChanged;
            OnParticleWidthSliderChanged(_particleWidthSlider.Value);
        }

        if (_particleLengthSlider != null)
        {
            _particleLengthSlider.ValueChanged += OnParticleLengthSliderChanged;
            OnParticleLengthSliderChanged(_particleLengthSlider.Value);
        }

        if (_particleAlphaSlider != null)
        {
            _particleAlphaSlider.ValueChanged += OnParticleAlphaSliderChanged;
            OnParticleAlphaSliderChanged(_particleAlphaSlider.Value);
        }

        if (_fallSpeedSlider != null)
        {
            _fallSpeedSlider.ValueChanged += OnFallSpeedSliderChanged;
            OnFallSpeedSliderChanged(_fallSpeedSlider.Value);
        }

        if (_emissionHeightSlider != null)
        {
            _emissionHeightSlider.ValueChanged += OnEmissionHeightSliderChanged;
            OnEmissionHeightSliderChanged(_emissionHeightSlider.Value);
        }

        if (_tintStrengthSlider != null)
        {
            _tintStrengthSlider.ValueChanged += OnTintStrengthSliderChanged;
            OnTintStrengthSliderChanged(_tintStrengthSlider.Value);
        }

        if (_lightEnergySlider != null)
        {
            _lightEnergySlider.ValueChanged += OnLightEnergySliderChanged;
            OnLightEnergySliderChanged(_lightEnergySlider.Value);
        }

        if (_glowSlider != null)
        {
            _glowSlider.ValueChanged += OnGlowSliderChanged;
            OnGlowSliderChanged(_glowSlider.Value);
        }

        if (_lightningIntervalSlider != null)
        {
            _lightningIntervalSlider.ValueChanged += OnLightningIntervalSliderChanged;
            OnLightningIntervalSliderChanged(_lightningIntervalSlider.Value);
        }

        if (_lightningIntensitySlider != null)
        {
            _lightningIntensitySlider.ValueChanged += OnLightningIntensitySliderChanged;
            OnLightningIntensitySliderChanged(_lightningIntensitySlider.Value);
        }
    }

    private void RespawnCombatants()
    {
        _actor = SpawnCombatant(_actor, DefaultPlayerScene, _playerAnchor, true, "WeatherActor");
        _target = SpawnCombatant(_target, DefaultEnemyScene, _enemyAnchor, false, "WeatherTarget");
        RefreshSummary();
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
        if (spawned is Node3D node3D)
        {
            node3D.Position = Vector3.Zero;
            node3D.Rotation = new Vector3(0f, isPlayer ? 0.35f : -0.35f, 0f);
        }

        return spawned;
    }

    private Node CreateFallbackCombatant(string name)
    {
        var character = new BaseCharacter { Name = name };
        var stats = new StatsComponent { Name = StatsComponent.NodeName };
        stats.SetBaseStatsResource(FallbackBaseStats);
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
            stats.SetBaseStatsResource(FallbackBaseStats);
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
            combatant.AddChild(new ActionManager { Name = ActionManager.DefaultName });
        }
    }

    private void ApplySelectedWeather()
    {
        if (_weatherSystem == null) return;
        if (_weatherSelect == null || _weatherOptions.Count == 0) return;

        int index = Mathf.Clamp(_weatherSelect.Selected, 0, _weatherOptions.Count - 1);
        var profile = _weatherOptions[index];
        _weatherSystem.SetWeather(profile);
        UpdateTuningUiForWeather(profile);
        RefreshSummary();
        Log($"Applied weather: {profile?.WeatherName ?? "(none)"} | {_weatherSystem.GetPrecipitationDebugDescription()}");
    }

    private void ClearWeather()
    {
        _weatherSystem?.ClearWeather();
        UpdateTuningUiForWeather(null);
        RefreshSummary();
        Log("Cleared active weather. Precipitation disabled.");
    }

    private void ResetTuning()
    {
        if (_weatherSystem == null) return;

        _weatherSystem.ResetRuntimeTuning();
        _suppressTuningLogs = true;
        SetSliderValue(_densitySlider, 1.0);
        SetSliderValue(_windSlider, 1.0);
        SetSliderValue(_depthSlider, 0.0);
        SetSliderValue(_nearCullSlider, 2.5);
        SetSliderValue(_particleWidthSlider, 1.0);
        SetSliderValue(_particleLengthSlider, 1.0);
        SetSliderValue(_particleAlphaSlider, 1.0);
        SetSliderValue(_fallSpeedSlider, 1.0);
        SetSliderValue(_emissionHeightSlider, 1.0);
        SetSliderValue(_tintStrengthSlider, 1.0);
        SetSliderValue(_lightEnergySlider, 1.0);
        SetSliderValue(_glowSlider, 1.0);
        SetSliderValue(_lightningIntervalSlider, 1.0);
        SetSliderValue(_lightningIntensitySlider, 1.0);
        _suppressTuningLogs = false;

        var active = _weatherSystem.ActiveWeather;
        UpdateTuningUiForWeather(active);
        RefreshSummary();
        Log($"Reset weather tuning to defaults. {_weatherSystem.GetPrecipitationDebugDescription()}");
    }

    private void SimulateTurnTick()
    {
        if (_weatherSystem == null)
        {
            Log("No WeatherSystem available.");
            return;
        }

        _weatherSystem.DebugSimulateTurnTick(_actor);
        RefreshSummary();
        Log("Simulated one weather turn tick.");
    }

    private void RunSampleAction()
    {
        if (_battleMechanics == null || _actor == null || _target == null)
        {
            Log("Sample action failed: missing mechanics or combatants.");
            return;
        }

        if (_sampleActions.Count == 0 || _sampleActionSelect == null)
        {
            Log("Sample action failed: no sample actions configured.");
            return;
        }

        int index = Mathf.Clamp(_sampleActionSelect.Selected, 0, _sampleActions.Count - 1);
        var action = _sampleActions[index];
        if (action == null)
        {
            Log("Sample action failed: selected action is null.");
            return;
        }

        RestoreToMaxResources(_actor);
        RestoreToMaxResources(_target);

        var allCombatants = new List<Node> { _actor, _target };
        var allies = allCombatants.Where(c => c != _actor && IsAlly(c, _actor)).ToList();

        var context = new ActionContext(action, _actor, new[] { _target });
        _battleMechanics.ProcessInitiation(context, allies);
        _battleMechanics.ProcessGlobalMods(context, allCombatants);
        var targetContexts = _battleMechanics.ProcessTargeting(context, allCombatants);
        _battleMechanics.CalculatePreliminary(targetContexts);
        _battleMechanics.CalculateFinalAndApply(targetContexts);

        var targetContext = targetContexts.FirstOrDefault();
        var result = targetContext?.GetResult(_target);
        int damage = result?.FinalDamage ?? 0;
        bool hit = result?.IsHit ?? false;

        var targetStats = _target.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        Log($"Sample action '{action.CommandName}' -> hit={hit}, damage={damage}, targetHP={targetStats?.CurrentHP}");
        RefreshSummary();
    }

    private static bool IsAlly(Node candidate, Node reference)
    {
        if (candidate == null || reference == null) return false;
        bool candidatePlayer = candidate.IsInGroup(GameGroups.PlayerCharacters);
        bool referencePlayer = reference.IsInGroup(GameGroups.PlayerCharacters);
        return candidatePlayer == referencePlayer;
    }

    private static void RestoreToMaxResources(Node combatant)
    {
        var stats = combatant?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null) return;

        int maxHp = Mathf.Max(1, stats.GetStatValue(StatType.HP));
        int maxMp = Mathf.Max(1, stats.GetStatValue(StatType.MP));

        stats.ModifyCurrentHP(maxHp - stats.CurrentHP);
        stats.ModifyCurrentMP(maxMp - stats.CurrentMP);
    }

    private void OnDensitySliderChanged(double value)
    {
        _weatherSystem?.SetDensityMultiplier((float)value);
        if (_densityValueLabel != null)
        {
            _densityValueLabel.Text = value.ToString("0.00");
        }
        LogTuningChange("Density", value, true);
    }

    private void OnWindSliderChanged(double value)
    {
        _weatherSystem?.SetWindMultiplier((float)value);
        if (_windValueLabel != null)
        {
            _windValueLabel.Text = value.ToString("0.00");
        }
        LogTuningChange("Wind", value, true);
    }

    private void OnDepthSliderChanged(double value)
    {
        _weatherSystem?.SetCameraDepthOffset((float)value);
        if (_depthValueLabel != null)
        {
            _depthValueLabel.Text = value.ToString("0.0");
        }
        LogTuningChange("Depth", value, true);
    }

    private void OnNearCullSliderChanged(double value)
    {
        _weatherSystem?.SetNearCameraExclusionDistance((float)value);
        if (_nearCullValueLabel != null)
        {
            _nearCullValueLabel.Text = value.ToString("0.0");
        }
        LogTuningChange("NearCull", value, true);
    }

    private void OnParticleWidthSliderChanged(double value)
    {
        _weatherSystem?.SetParticleWidthMultiplier((float)value);
        if (_particleWidthValueLabel != null)
        {
            _particleWidthValueLabel.Text = value.ToString("0.00");
        }
        LogTuningChange("Width", value, true);
    }

    private void OnParticleLengthSliderChanged(double value)
    {
        _weatherSystem?.SetParticleLengthMultiplier((float)value);
        if (_particleLengthValueLabel != null)
        {
            _particleLengthValueLabel.Text = value.ToString("0.00");
        }
        LogTuningChange("Length", value, true);
    }

    private void OnParticleAlphaSliderChanged(double value)
    {
        _weatherSystem?.SetParticleAlphaMultiplier((float)value);
        if (_particleAlphaValueLabel != null)
        {
            _particleAlphaValueLabel.Text = value.ToString("0.00");
        }
        LogTuningChange("Alpha", value, true);
    }

    private void OnFallSpeedSliderChanged(double value)
    {
        _weatherSystem?.SetFallSpeedMultiplier((float)value);
        if (_fallSpeedValueLabel != null)
        {
            _fallSpeedValueLabel.Text = value.ToString("0.00");
        }
        LogTuningChange("FallSpeed", value, true);
    }

    private void OnEmissionHeightSliderChanged(double value)
    {
        _weatherSystem?.SetEmissionHeightMultiplier((float)value);
        if (_emissionHeightValueLabel != null)
        {
            _emissionHeightValueLabel.Text = value.ToString("0.00");
        }
        LogTuningChange("EmissionHeight", value, true);
    }

    private void OnTintStrengthSliderChanged(double value)
    {
        _weatherSystem?.SetEnvironmentTintStrengthMultiplier((float)value);
        if (_tintStrengthValueLabel != null)
        {
            _tintStrengthValueLabel.Text = value.ToString("0.00");
        }
        LogTuningChange("TintStrength", value, true);
    }

    private void OnLightEnergySliderChanged(double value)
    {
        _weatherSystem?.SetLightEnergyMultiplier((float)value);
        if (_lightEnergyValueLabel != null)
        {
            _lightEnergyValueLabel.Text = value.ToString("0.00");
        }
        LogTuningChange("LightEnergy", value, true);
    }

    private void OnGlowSliderChanged(double value)
    {
        _weatherSystem?.SetGlowIntensityMultiplier((float)value);
        if (_glowValueLabel != null)
        {
            _glowValueLabel.Text = value.ToString("0.00");
        }
        LogTuningChange("Glow", value, true);
    }

    private void OnLightningIntervalSliderChanged(double value)
    {
        _weatherSystem?.SetLightningIntervalMultiplier((float)value);
        if (_lightningIntervalValueLabel != null)
        {
            _lightningIntervalValueLabel.Text = value.ToString("0.00");
        }
        LogTuningChange("LightningRate", value, true);
    }

    private void OnLightningIntensitySliderChanged(double value)
    {
        _weatherSystem?.SetLightningIntensityMultiplier((float)value);
        if (_lightningIntensityValueLabel != null)
        {
            _lightningIntensityValueLabel.Text = value.ToString("0.00");
        }
        LogTuningChange("LightningPower", value, true);
    }

    private void UpdateTuningUiForWeather(WeatherProfile profile)
    {
        bool hasProfile = profile != null;
        bool hasPrecip = hasProfile && profile.PrecipitationMode != WeatherPrecipitationMode.None;
        bool hasLightning = hasProfile && profile.EnableLightningFlashes;
        bool hasTint = hasProfile && profile.EnableEnvironmentTint;
        bool hasGlow = hasProfile && profile.EnableGlowBoost;

        SetSliderRowEnabled(_densitySlider, _densityValueLabel, hasPrecip);
        SetSliderRowEnabled(_windSlider, _windValueLabel, hasPrecip);
        SetSliderRowEnabled(_depthSlider, _depthValueLabel, hasPrecip);
        SetSliderRowEnabled(_nearCullSlider, _nearCullValueLabel, hasPrecip);
        SetSliderRowEnabled(_particleWidthSlider, _particleWidthValueLabel, hasPrecip);
        SetSliderRowEnabled(_particleLengthSlider, _particleLengthValueLabel, hasPrecip);
        SetSliderRowEnabled(_particleAlphaSlider, _particleAlphaValueLabel, hasPrecip);
        SetSliderRowEnabled(_fallSpeedSlider, _fallSpeedValueLabel, hasPrecip);
        SetSliderRowEnabled(_emissionHeightSlider, _emissionHeightValueLabel, hasPrecip);

        SetSliderRowEnabled(_tintStrengthSlider, _tintStrengthValueLabel, hasTint);
        SetSliderRowEnabled(_lightEnergySlider, _lightEnergyValueLabel, hasProfile);
        SetSliderRowEnabled(_glowSlider, _glowValueLabel, hasGlow);

        SetSliderRowEnabled(_lightningIntervalSlider, _lightningIntervalValueLabel, hasLightning);
        SetSliderRowEnabled(_lightningIntensitySlider, _lightningIntensityValueLabel, hasLightning);

        if (_resetTuningButton != null)
        {
            _resetTuningButton.Disabled = !hasProfile;
            _resetTuningButton.TooltipText = hasProfile
                ? "Reset all weather tuning multipliers to 1.0 (depth to 0.0)."
                : "Apply a weather profile first.";
        }
    }

    private void RefreshSummary()
    {
        if (_summaryLabel == null) return;

        string weatherName = _weatherSystem?.ActiveWeather?.WeatherName ?? "(none)";
        var actorStats = _actor?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        var targetStats = _target?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);

        _summaryLabel.Text =
            $"Weather: {weatherName}\n" +
            $"Move Mult: {_weatherSystem?.MovementSpeedMultiplier ?? 1f:0.00} | Visibility Mult: {_weatherSystem?.VisibilityMultiplier ?? 1f:0.00}\n" +
            $"{_weatherSystem?.GetPrecipitationDebugDescription() ?? "Precipitation: disabled"}\n" +
            $"Actor HP: {actorStats?.CurrentHP}/{actorStats?.GetStatValue(StatType.HP)}\n" +
            $"Target HP: {targetStats?.CurrentHP}/{targetStats?.GetStatValue(StatType.HP)}";
    }

    private void Log(string message)
    {
        if (_logLabel == null) return;
        _logLabel.AppendText($"[{System.DateTime.Now:HH:mm:ss}] {message}\n");
        _logLabel.ScrollToLine(Mathf.Max(_logLabel.GetLineCount() - 1, 0));
    }

    private void LogTuningChange(string controlName, double value, bool includePrecipitationDetails = false)
    {
        if (_suppressTuningLogs)
        {
            return;
        }

        ulong now = Time.GetTicksMsec();
        if (now - _lastTuneLogMs < 250)
        {
            return;
        }

        _lastTuneLogMs = now;
        RefreshSummary();
        if (includePrecipitationDetails && _weatherSystem != null)
        {
            Log($"Tuning {controlName}={value:0.00} -> {_weatherSystem.GetPrecipitationDebugDescription()}");
            return;
        }

        Log($"Tuning {controlName}={value:0.00}");
    }

    private static void SetSliderRowEnabled(HSlider slider, Label valueLabel, bool enabled)
    {
        if (slider == null) return;

        slider.Editable = enabled;
        slider.Modulate = enabled ? Colors.White : new Color(1f, 1f, 1f, 0.45f);
        if (valueLabel != null)
        {
            valueLabel.Modulate = enabled ? Colors.White : new Color(1f, 1f, 1f, 0.55f);
        }

        if (slider.GetParent() is Control row)
        {
            row.Modulate = enabled ? Colors.White : new Color(1f, 1f, 1f, 0.6f);
        }
    }

    private static void SetSliderValue(HSlider slider, double value)
    {
        if (slider == null) return;
        slider.Value = Mathf.Clamp((float)value, (float)slider.MinValue, (float)slider.MaxValue);
    }

    private void ToggleTuningUiVisibility()
    {
        SetTuningUiVisibility(!_tuningUiHidden);
    }

    private void SetTuningUiVisibility(bool hide)
    {
        _tuningUiHidden = hide;

        if (_uiHudPanel != null)
        {
            _uiHudPanel.Visible = !hide;
        }

        if (_summaryPanel != null)
        {
            _summaryPanel.Visible = !hide;
        }

        if (_logPanel != null)
        {
            _logPanel.Visible = !hide;
        }

        if (_toggleUiButton != null)
        {
            _toggleUiButton.Text = hide ? "Show Tuning UI" : "Hide Tuning UI";
            _toggleUiButton.TooltipText = hide
                ? "Show weather tuning and debug panels."
                : "Hide weather tuning and debug panels for unobstructed preview.";
        }
    }
}
