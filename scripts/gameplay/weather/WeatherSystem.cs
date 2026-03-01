using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Reusable scene-level weather driver.
/// Handles weather visuals/audio for any scene, and applies battle weather hooks when a battle is present.
/// </summary>
[GlobalClass]
public partial class WeatherSystem : Node3D
{
    public const string NodeName = "WeatherSystem";

    [Signal]
    public delegate void WeatherChangedEventHandler(WeatherProfile weather);

    [Signal]
    public delegate void SceneConditionChangedEventHandler(float movementSpeedMultiplier, float visibilityMultiplier);

    [ExportGroup("Weather")]
    [Export]
    public WeatherProfile InitialWeather { get; private set; }

    [Export]
    public Godot.Collections.Array<WeatherProfile> WeatherLibrary { get; private set; } = new();

    [Export]
    public bool ApplyInitialWeatherOnReady { get; private set; } = true;

    [ExportGroup("Scene References")]
    [Export]
    public WorldEnvironment WorldEnvironment { get; private set; }

    [Export]
    public DirectionalLight3D MainLight { get; private set; }

    [Export]
    public BattlefieldEffectManager BattlefieldEffectManager { get; private set; }

    [Export]
    public BattleController BattleController { get; private set; }

    [Export]
    public Camera3D WeatherCamera { get; private set; }

    [Export]
    public SceneVisualDirector VisualDirector { get; private set; }

    [ExportGroup("Particle Placement")]
    [Export]
    public bool FollowCamera { get; private set; } = true;

    [Export]
    public Vector3 CameraFollowOffset { get; private set; } = new Vector3(0f, 10f, 0f);

    [Export]
    public Vector2 EmissionAreaSize { get; private set; } = new Vector2(24f, 18f);

    [Export]
    public bool EmitInCameraForwardOnly { get; private set; } = true;

    [Export(PropertyHint.Range, "0,4,0.01")]
    public float RuntimeDensityMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0,4,0.01")]
    public float RuntimeWindMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0.25,3,0.01")]
    public float RuntimeParticleWidthMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0.25,3,0.01")]
    public float RuntimeParticleLengthMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0.1,3,0.01")]
    public float RuntimeParticleAlphaMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0,40,0.1")]
    public float RuntimeCameraDepthOffset { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0,12,0.1")]
    public float RuntimeNearCameraExclusionDistance { get; private set; } = 2.5f;

    [Export(PropertyHint.Range, "0.1,3,0.01")]
    public float RuntimeFallSpeedMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0.25,3,0.01")]
    public float RuntimeEmissionHeightMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0,3,0.01")]
    public float RuntimeEnvironmentTintStrengthMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0.1,3,0.01")]
    public float RuntimeLightEnergyMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0.1,3,0.01")]
    public float RuntimeGlowIntensityMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0.1,3,0.01")]
    public float RuntimeLightningIntervalMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0,3,0.01")]
    public float RuntimeLightningIntensityMultiplier { get; private set; } = 1f;

    [ExportGroup("Time Of Day")]
    [Export]
    public bool EnableTimeOfDay { get; private set; } = false;

    [Export]
    public bool TreatAsOutdoorScene { get; private set; } = true;

    [Export]
    public bool AutoAdvanceTimeOfDay { get; private set; } = false;

    [Export(PropertyHint.Range, "0,24,0.01")]
    public float TimeOfDayHours { get; private set; } = 12f;

    [Export(PropertyHint.Range, "0.1,240,0.1")]
    public float FullDayDurationMinutes { get; private set; } = 20f;

    [Export(PropertyHint.Range, "-180,180,0.1")]
    public float SolarAzimuthDegrees { get; private set; } = -35f;

    [Export(PropertyHint.Range, "0,89,0.1")]
    public float MaxSolarAltitudeDegrees { get; private set; } = 72f;

    [Export(PropertyHint.Range, "0.01,1,0.01")]
    public float TwilightSoftness { get; private set; } = 0.22f;

    [Export]
    public bool EnableMoonlightAtNight { get; private set; } = true;

    [Export]
    public Color MoonlightTint { get; private set; } = new Color(0.60f, 0.70f, 0.95f, 1f);

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float MoonlightTintStrength { get; private set; } = 0.55f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float MoonlightDirectionalEnergyMultiplier { get; private set; } = 0.22f;

    [Export(PropertyHint.Range, "0.1,4,0.01")]
    public float NightAmbientEnergyMultiplier { get; private set; } = 1.35f;

    [Export(PropertyHint.Range, "0.05,1,0.01")]
    public float NightSceneBrightnessMultiplier { get; private set; } = 0.35f;

    [Export(PropertyHint.Range, "0.05,1,0.01")]
    public float NightSceneSaturationMultiplier { get; private set; } = 0.8f;

    [Export(PropertyHint.Range, "0.01,1,0.01")]
    public float NightGlowIntensityMultiplier { get; private set; } = 0.25f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float NightAmbientColorMultiplier { get; private set; } = 0.18f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float NightFogColorMultiplier { get; private set; } = 0.35f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float NightFogDensityMultiplier { get; private set; } = 0.5f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float NightSkyAmbientContributionMultiplier { get; private set; } = 0f;

    [Export]
    public bool DisableMainLightShadowsAtNight { get; private set; } = true;

    private WeatherProfile _activeWeather;
    private readonly List<BattlefieldEffect> _injectedBattlefieldEffects = new();
    private readonly List<WeatherTurnHazard> _activeTurnHazards = new();
    private readonly Dictionary<WeatherPrecipitationMode, Texture2D> _defaultPrecipitationTextures = new();

    private GpuParticles3D _precipitationParticles;
    private AudioStreamPlayer _ambientPlayer;
    private AudioStreamPlayer _oneshotPlayer;
    private OmniLight3D _lightningFlash;
    private Timer _lightningTimer;
    private CanvasLayer _lightningOverlayLayer;
    private ColorRect _lightningOverlayRect;
    private Tween _lightningOverlayTween;
    private Tween _lightningSceneTween;
    private Color _lightningOverlayTint = new Color(1f, 1f, 1f, 0f);
    private readonly RandomNumberGenerator _rng = new();
    private int _globalTurnCounter;

    private Color _baseAmbientLightColor = Colors.White;
    private Color _baseFogLightColor = Colors.White;
    private float _baseAmbientLightEnergy = 1f;
    private float _baseFogDensity = 0f;
    private float _baseAmbientSkyContribution = 1f;
    private float _baseGlowIntensity = 1f;
    private bool _baseAdjustmentEnabled;
    private float _baseAdjustmentBrightness = 1f;
    private float _baseAdjustmentSaturation = 1f;
    private Color _baseLightColor = Colors.White;
    private float _baseLightEnergy = 1f;
    private bool _baseLightShadowEnabled = true;
    private bool _capturedEnvironmentBaseline;
    private bool _capturedLightBaseline;
    private string _visualContributionId;
    private float _lastAppliedTimeOfDayHours = float.NaN;
    private bool _lastTimeOfDayActive;

    public WeatherProfile ActiveWeather => _activeWeather;
    public float MovementSpeedMultiplier => _activeWeather?.MovementSpeedMultiplier ?? 1f;
    public float VisibilityMultiplier => _activeWeather?.VisibilityMultiplier ?? 1f;
    private bool UseVisualDirector => VisualDirector != null && GodotObject.IsInstanceValid(VisualDirector);

    public override void _Ready()
    {
        EnsureNodes();
        ResolveSceneReferences();
        _visualContributionId = $"{Name}:{GetInstanceId()}:Weather";
        WireBattleHooks();

        if (ApplyInitialWeatherOnReady && InitialWeather != null)
        {
            SetWeather(InitialWeather);
        }
    }

    public override void _Process(double delta)
    {
        UpdateTimeOfDay(delta);
        UpdateParticleAnchor();
    }

    public override void _ExitTree()
    {
        ClearVisualContribution();
        UnwireBattleHooks();
    }

    public void SetWeather(WeatherProfile weather)
    {
        if (_activeWeather == weather)
        {
            ReapplyVisuals();
            return;
        }

        ClearInjectedBattlefieldEffects();
        _activeTurnHazards.Clear();
        _globalTurnCounter = 0;

        _activeWeather = weather;

        ApplyAudio();
        ApplyVisuals();
        InjectBattlefieldEffects();
        CacheTurnHazards();

        EmitSignal(SignalName.WeatherChanged, weather);
        EmitSignal(SignalName.SceneConditionChanged, MovementSpeedMultiplier, VisibilityMultiplier);
    }

    public void ClearWeather()
    {
        SetWeather(null);
    }

    public void SetDensityMultiplier(float multiplier)
    {
        RuntimeDensityMultiplier = Mathf.Max(0f, multiplier);
        ReapplyVisuals();
    }

    public void SetWindMultiplier(float multiplier)
    {
        RuntimeWindMultiplier = Mathf.Max(0f, multiplier);
        ReapplyVisuals();
    }

    public void SetParticleWidthMultiplier(float multiplier)
    {
        RuntimeParticleWidthMultiplier = Mathf.Max(0.1f, multiplier);
        ReapplyVisuals();
    }

    public void SetParticleLengthMultiplier(float multiplier)
    {
        RuntimeParticleLengthMultiplier = Mathf.Max(0.1f, multiplier);
        ReapplyVisuals();
    }

    public void SetParticleAlphaMultiplier(float multiplier)
    {
        RuntimeParticleAlphaMultiplier = Mathf.Max(0.05f, multiplier);
        ReapplyVisuals();
    }

    public void SetCameraDepthOffset(float depthOffset)
    {
        RuntimeCameraDepthOffset = Mathf.Max(0f, depthOffset);
        ReapplyVisuals();
    }

    public void SetNearCameraExclusionDistance(float distance)
    {
        RuntimeNearCameraExclusionDistance = Mathf.Max(0f, distance);
        ReapplyVisuals();
    }

    public void SetFallSpeedMultiplier(float multiplier)
    {
        RuntimeFallSpeedMultiplier = Mathf.Max(0.05f, multiplier);
        ReapplyVisuals();
    }

    public void SetEmissionHeightMultiplier(float multiplier)
    {
        RuntimeEmissionHeightMultiplier = Mathf.Max(0.05f, multiplier);
        ReapplyVisuals();
    }

    public void SetEnvironmentTintStrengthMultiplier(float multiplier)
    {
        RuntimeEnvironmentTintStrengthMultiplier = Mathf.Max(0f, multiplier);
        ReapplyVisuals();
    }

    public void SetLightEnergyMultiplier(float multiplier)
    {
        RuntimeLightEnergyMultiplier = Mathf.Max(0.05f, multiplier);
        ReapplyVisuals();
    }

    public void SetGlowIntensityMultiplier(float multiplier)
    {
        RuntimeGlowIntensityMultiplier = Mathf.Max(0.05f, multiplier);
        ReapplyVisuals();
    }

    public void SetLightningIntervalMultiplier(float multiplier)
    {
        RuntimeLightningIntervalMultiplier = Mathf.Max(0.05f, multiplier);
        ReapplyVisuals();
    }

    public void SetLightningIntensityMultiplier(float multiplier)
    {
        RuntimeLightningIntensityMultiplier = Mathf.Max(0f, multiplier);
        ReapplyVisuals();
    }

    public void ResetRuntimeTuning()
    {
        RuntimeDensityMultiplier = 1f;
        RuntimeWindMultiplier = 1f;
        RuntimeParticleWidthMultiplier = 1f;
        RuntimeParticleLengthMultiplier = 1f;
        RuntimeParticleAlphaMultiplier = 1f;
        RuntimeCameraDepthOffset = 0f;
        RuntimeNearCameraExclusionDistance = 2.5f;
        RuntimeFallSpeedMultiplier = 1f;
        RuntimeEmissionHeightMultiplier = 1f;
        RuntimeEnvironmentTintStrengthMultiplier = 1f;
        RuntimeLightEnergyMultiplier = 1f;
        RuntimeGlowIntensityMultiplier = 1f;
        RuntimeLightningIntervalMultiplier = 1f;
        RuntimeLightningIntensityMultiplier = 1f;
        ReapplyVisuals();
    }

    public string GetPrecipitationDebugDescription()
    {
        if (_activeWeather == null || _activeWeather.PrecipitationMode == WeatherPrecipitationMode.None)
        {
            return "Precipitation: disabled";
        }

        float density = Mathf.Max(0f, _activeWeather.ParticleDensity * RuntimeDensityMultiplier);
        var size = GetResolvedParticleSize(_activeWeather);
        float depth = GetResolvedCameraDepthOffset(_activeWeather);
        float alpha = GetResolvedParticleAlpha(_activeWeather);
        string textureMode = _activeWeather.ParticleTexture != null ? "custom" : "default";

        return
            $"Precipitation={_activeWeather.PrecipitationMode} " +
            $"density={density:0.00} " +
            $"size={size.X:0.000}x{size.Y:0.000} " +
            $"depth={depth:0.0} " +
            $"nearCull={RuntimeNearCameraExclusionDistance:0.0} " +
            $"forwardOnly={(EmitInCameraForwardOnly ? "yes" : "no")} " +
            $"alpha={alpha:0.00} " +
            $"tex={textureMode} " +
            $"fall={GetResolvedFallSpeed(_activeWeather):0.0} " +
            $"wind={_activeWeather.WindStrength * RuntimeWindMultiplier:0.0} " +
            $"light={GetResolvedLightEnergyMultiplier(_activeWeather):0.00}x " +
            $"ltgInt={RuntimeLightningIntensityMultiplier:0.00}x " +
            $"ltgRate={RuntimeLightningIntervalMultiplier:0.00}x";
    }

    /// <summary>
    /// Debug/testing helper for weather sandboxes to emulate one "turn started" tick without a live battle.
    /// </summary>
    public void DebugSimulateTurnTick(Node activeTurnOwner = null)
    {
        if (_activeTurnHazards.Count == 0) return;

        List<Node> candidates = new();
        if (BattleController != null)
        {
            candidates.AddRange(BattleController.GetLivingCombatants().Where(CombatantUtils.IsValidLivingCombatant));
        }
        else
        {
            candidates.AddRange(GetTree().GetNodesInGroup(GameGroups.PlayerCharacters).Where(CombatantUtils.IsValidLivingCombatant));
            // In debug scenes enemies may simply be non-player BaseCharacters.
            foreach (var node in GetTree().CurrentScene?.FindChildren("*", "BaseCharacter", true, false) ?? new Godot.Collections.Array<Node>())
            {
                if (node is Node n && CombatantUtils.IsValidLivingCombatant(n) && !candidates.Contains(n))
                {
                    candidates.Add(n);
                }
            }
        }

        ApplyTurnHazards(activeTurnOwner, candidates);
    }

    private void EnsureNodes()
    {
        _precipitationParticles = GetNodeOrNull<GpuParticles3D>("PrecipitationParticles");
        if (_precipitationParticles == null)
        {
            _precipitationParticles = new GpuParticles3D { Name = "PrecipitationParticles" };
            AddChild(_precipitationParticles);
        }

        _ambientPlayer = GetNodeOrNull<AudioStreamPlayer>("WeatherAmbientPlayer");
        if (_ambientPlayer == null)
        {
            _ambientPlayer = new AudioStreamPlayer { Name = "WeatherAmbientPlayer" };
            AddChild(_ambientPlayer);
        }
        var ambientFinishedCallable = Callable.From(OnAmbientFinished);
        if (_ambientPlayer.IsConnected(AudioStreamPlayer.SignalName.Finished, ambientFinishedCallable))
        {
            _ambientPlayer.Disconnect(AudioStreamPlayer.SignalName.Finished, ambientFinishedCallable);
        }
        _ambientPlayer.Connect(AudioStreamPlayer.SignalName.Finished, ambientFinishedCallable);

        _oneshotPlayer = GetNodeOrNull<AudioStreamPlayer>("WeatherOneShotPlayer");
        if (_oneshotPlayer == null)
        {
            _oneshotPlayer = new AudioStreamPlayer { Name = "WeatherOneShotPlayer" };
            AddChild(_oneshotPlayer);
        }

        _lightningFlash = GetNodeOrNull<OmniLight3D>("LightningFlash");
        if (_lightningFlash == null)
        {
            _lightningFlash = new OmniLight3D
            {
                Name = "LightningFlash",
                LightColor = new Color(0.85f, 0.9f, 1.0f),
                LightEnergy = 0f,
                OmniRange = 40f
            };
            AddChild(_lightningFlash);
        }

        _lightningTimer = GetNodeOrNull<Timer>("LightningTimer");
        if (_lightningTimer == null)
        {
            _lightningTimer = new Timer { Name = "LightningTimer", OneShot = true };
            AddChild(_lightningTimer);
        }
        if (!_lightningTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnLightningTimerTimeout)))
        {
            _lightningTimer.Timeout += OnLightningTimerTimeout;
        }

        _lightningOverlayLayer = GetNodeOrNull<CanvasLayer>("LightningOverlayLayer");
        if (_lightningOverlayLayer == null)
        {
            _lightningOverlayLayer = new CanvasLayer
            {
                Name = "LightningOverlayLayer",
                Layer = 256
            };
            AddChild(_lightningOverlayLayer);
        }

        _lightningOverlayRect = _lightningOverlayLayer.GetNodeOrNull<ColorRect>("LightningOverlayRect");
        if (_lightningOverlayRect == null)
        {
            _lightningOverlayRect = new ColorRect
            {
                Name = "LightningOverlayRect",
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            _lightningOverlayRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _lightningOverlayRect.OffsetLeft = 0f;
            _lightningOverlayRect.OffsetTop = 0f;
            _lightningOverlayRect.OffsetRight = 0f;
            _lightningOverlayRect.OffsetBottom = 0f;
            _lightningOverlayLayer.AddChild(_lightningOverlayRect);
        }
        SetLightningOverlayAlpha(0f);
    }

    private void ResolveSceneReferences()
    {
        if (WorldEnvironment == null)
        {
            WorldEnvironment = GetTree()?.CurrentScene?.FindChild("WorldEnvironment", true, false) as WorldEnvironment;
        }

        if (MainLight == null)
        {
            MainLight = GetTree()?.CurrentScene?.FindChild("MainLight", true, false) as DirectionalLight3D
                ?? GetTree()?.CurrentScene?.FindChild("DirectionalLight3D", true, false) as DirectionalLight3D;
        }

        if (BattlefieldEffectManager == null)
        {
            BattlefieldEffectManager = GetTree()?.CurrentScene?.FindChild(BattlefieldEffectManager.NodeName, true, false) as BattlefieldEffectManager;
        }

        if (BattleController == null)
        {
            BattleController = GetTree()?.CurrentScene?.FindChild("BattleController", true, false) as BattleController;
        }

        if (WeatherCamera == null)
        {
            WeatherCamera = GetViewport()?.GetCamera3D();
            if (WeatherCamera == null)
            {
                WeatherCamera = GetTree()?.CurrentScene?.FindChild("Camera3D", true, false) as Camera3D;
            }
        }

        if (VisualDirector == null)
        {
            VisualDirector = GetTree()?.CurrentScene?.FindChild(SceneVisualDirector.NodeName, true, false) as SceneVisualDirector;
        }
    }

    private void WireBattleHooks()
    {
        if (BattleController == null) return;

        BattleController.TurnStarted -= OnBattleTurnStarted;
        BattleController.TurnStarted += OnBattleTurnStarted;
    }

    private void UnwireBattleHooks()
    {
        if (BattleController == null) return;
        BattleController.TurnStarted -= OnBattleTurnStarted;
    }

    private void OnBattleTurnStarted(TurnManager.TurnData turnData)
    {
        if (_activeTurnHazards.Count == 0) return;
        if (!Multiplayer.IsServer()) return;
        if (BattleController == null) return;

        var activeOwner = turnData?.Combatant;
        var living = BattleController.GetLivingCombatants()
            .Where(CombatantUtils.IsValidLivingCombatant)
            .ToList();

        ApplyTurnHazards(activeOwner, living);
    }

    private void ApplyTurnHazards(Node activeTurnOwner, List<Node> livingCombatants)
    {
        if (_activeTurnHazards.Count == 0 || livingCombatants == null || livingCombatants.Count == 0) return;

        _globalTurnCounter++;
        foreach (var hazard in _activeTurnHazards)
        {
            if (hazard == null || !hazard.Enabled) continue;

            int interval = Mathf.Max(1, hazard.TriggerEveryTurnStarts);
            if (_globalTurnCounter % interval != 0) continue;

            float chance = Mathf.Clamp(hazard.TriggerChancePercent, 0f, 100f);
            if (chance < 100f && _rng.RandfRange(0f, 100f) > chance) continue;

            List<Node> targets = ResolveHazardTargets(hazard, activeTurnOwner, livingCombatants);
            foreach (var target in targets)
            {
                ApplyHazardToTarget(hazard, target);
            }
        }
    }

    private List<Node> ResolveHazardTargets(WeatherTurnHazard hazard, Node activeTurnOwner, List<Node> livingCombatants)
    {
        var available = livingCombatants
            .Where(c => hazard.CanTargetActiveTurnOwner || c != activeTurnOwner)
            .ToList();

        if (available.Count == 0) return new List<Node>();
        if (hazard.ApplyToAllCombatants) return available;

        int count = Mathf.Clamp(hazard.RandomTargetCount, 1, available.Count);
        RandomListUtils.ShuffleInPlace(available, _rng);
        return available.Take(count).ToList();
    }

    private void ApplyHazardToTarget(WeatherTurnHazard hazard, Node target)
    {
        if (target == null || !CombatantUtils.IsValidLivingCombatant(target)) return;

        var stats = target.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null) return;

        int flatMin = Mathf.Max(0, hazard.FlatDamageMin);
        int flatMax = Mathf.Max(flatMin, hazard.FlatDamageMax);
        int flat = flatMax > flatMin ? _rng.RandiRange(flatMin, flatMax) : flatMin;

        int maxHp = Mathf.Max(1, stats.GetStatValue(StatType.HP));
        int percentDamage = Mathf.Max(0, Mathf.RoundToInt(maxHp * Mathf.Max(0f, hazard.PercentOfMaxHpDamage)));
        int finalDamage = Mathf.Max(1, flat + percentDamage);

        if (hazard.Element != ElementType.None)
        {
            var elemental = target.GetNodeOrNull<ElementalComponent>(ElementalComponent.NodeName);
            if (elemental != null)
            {
                finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * elemental.GetResistanceMultiplier(hazard.Element)));
            }
        }

        stats.ModifyCurrentHP(-finalDamage);

        if (!string.IsNullOrEmpty(hazard.CombatLogMessage))
        {
            GD.Print($"[Weather] {hazard.CombatLogMessage} -> {target.Name} ({finalDamage})");
        }
        else
        {
            GD.Print($"[Weather] {hazard.HazardName} hits {target.Name} for {finalDamage}.");
        }

        PlayHazardVfxSfx(hazard, target);
    }

    private void PlayHazardVfxSfx(WeatherTurnHazard hazard, Node target)
    {
        if (hazard == null || target == null) return;

        if (hazard.ImpactVfx != null)
        {
            var vfxNode = hazard.ImpactVfx.Instantiate();
            if (vfxNode is Node3D vfx3D)
            {
                var targetPosition = target is Node3D target3D ? target3D.GlobalPosition : GlobalPosition;
                vfx3D.GlobalPosition = targetPosition + hazard.ImpactVfxOffset;
            }
            GetTree()?.CurrentScene?.AddChild(vfxNode);
        }

        if (hazard.ImpactSfx != null)
        {
            float pitchMin = Mathf.Min(hazard.ImpactPitchMin, hazard.ImpactPitchMax);
            float pitchMax = Mathf.Max(hazard.ImpactPitchMin, hazard.ImpactPitchMax);
            float pitch = _rng.RandfRange(pitchMin, pitchMax);

            if (SceneAudioController.Instance != null && target is Node3D target3D)
            {
                SceneAudioController.Instance.PlaySFX3D(hazard.ImpactSfx, target3D.GlobalPosition, hazard.ImpactSfxVolumeDb, pitch);
            }
            else
            {
                _oneshotPlayer.Stream = hazard.ImpactSfx;
                _oneshotPlayer.VolumeDb = hazard.ImpactSfxVolumeDb;
                _oneshotPlayer.PitchScale = pitch;
                _oneshotPlayer.Play();
            }
        }
    }

    private void CacheTurnHazards()
    {
        _activeTurnHazards.Clear();
        if (_activeWeather == null) return;

        foreach (var hazard in _activeWeather.EnumerateTurnHazards())
        {
            if (hazard == null) continue;
            if (hazard.Duplicate(true) is WeatherTurnHazard runtime)
            {
                _activeTurnHazards.Add(runtime);
            }
        }
    }

    private void InjectBattlefieldEffects()
    {
        if (_activeWeather == null || BattlefieldEffectManager == null) return;

        foreach (var effect in _activeWeather.EnumerateBattlefieldEffects())
        {
            if (effect == null) continue;
            if (effect.Duplicate(true) is not BattlefieldEffect runtime) continue;
            if (!BattlefieldEffectManager.AddEffect(runtime)) continue;
            _injectedBattlefieldEffects.Add(runtime);
        }
    }

    private void ClearInjectedBattlefieldEffects()
    {
        if (BattlefieldEffectManager == null || _injectedBattlefieldEffects.Count == 0)
        {
            _injectedBattlefieldEffects.Clear();
            return;
        }

        foreach (var effect in _injectedBattlefieldEffects)
        {
            BattlefieldEffectManager.RemoveEffect(effect);
        }
        _injectedBattlefieldEffects.Clear();
    }

    private void ReapplyVisuals()
    {
        ApplyVisuals();
    }

    private void ApplyAudio()
    {
        if (_ambientPlayer == null) return;

        if (_activeWeather?.AmbientLoop == null)
        {
            _ambientPlayer.Stop();
            _ambientPlayer.Stream = null;
            return;
        }

        _ambientPlayer.Stream = _activeWeather.AmbientLoop;
        _ambientPlayer.VolumeDb = _activeWeather.AmbientVolumeDb;
        _ambientPlayer.PitchScale = _activeWeather.AmbientPitchScale;
        _ambientPlayer.Play();
    }

    private void OnAmbientFinished()
    {
        if (_ambientPlayer == null) return;
        if (_activeWeather?.AmbientLoop == null) return;
        if (!_activeWeather.ForceAmbientLoop) return;
        if (_ambientPlayer.Stream != _activeWeather.AmbientLoop)
        {
            _ambientPlayer.Stream = _activeWeather.AmbientLoop;
        }
        _ambientPlayer.Play();
    }

    private void ApplyVisuals()
    {
        ApplyPrecipitation();
        if (UseVisualDirector)
        {
            PushWeatherVisualContribution();
        }
        else
        {
            ApplyEnvironmentModifiers();
            ApplyLightModifiers();
        }
        ApplyLightningState();
    }

    private void PushWeatherVisualContribution()
    {
        if (!UseVisualDirector) return;

        // Keep time-of-day contribution active even when no weather profile is selected.
        if (_activeWeather == null && !IsTimeOfDayActive())
        {
            ClearVisualContribution();
            return;
        }

        VisualDirector.SetContribution(_visualContributionId, BuildWeatherVisualIntent());
    }

    private void ClearVisualContribution()
    {
        if (!UseVisualDirector) return;
        VisualDirector.ClearContribution(_visualContributionId);
    }

    private SceneVisualIntent BuildWeatherVisualIntent()
    {
        var intent = new SceneVisualIntent
        {
            Layer = (int)SceneVisualContributionLayer.Weather,
            AmbientEnergyMultiplier = ResolveWeatherAmbientEnergyMultiplier(),
            MainLightEnergyMultiplier = ResolveWeatherMainLightEnergyMultiplier(),
            GlowIntensityMultiplier = ResolveWeatherGlowIntensityMultiplier()
        };

        if (_activeWeather != null && _activeWeather.EnableEnvironmentTint)
        {
            float t = GetResolvedEnvironmentTintStrength(_activeWeather);
            intent.UseAmbientTint = true;
            intent.AmbientTint = _activeWeather.EnvironmentTint;
            intent.AmbientTintStrength = t;
            intent.UseFogTint = true;
            intent.FogTint = _activeWeather.EnvironmentTint;
            intent.FogTintStrength = t;
            intent.FogEnabledOverride = true;
        }

        if (_activeWeather != null && _activeWeather.EnableLightColorOverride)
        {
            intent.OverrideMainLightColor = true;
            intent.MainLightColorOverride = _activeWeather.LightColorOverride;
        }

        bool disableShadows = false;
        if (_activeWeather != null
            && _activeWeather.EnableOvercastDiffuseLighting
            && _activeWeather.DisableMainLightShadowsInOvercast)
        {
            disableShadows = true;
        }

        if (IsTimeOfDayActive())
        {
            float dayBlend = GetDaylightBlend();
            float nightBlend = 1f - dayBlend;

            intent.AmbientColorMultiplier = Mathf.Lerp(Mathf.Clamp(NightAmbientColorMultiplier, 0f, 1f), 1f, dayBlend);
            intent.FogColorMultiplier = Mathf.Lerp(Mathf.Clamp(NightFogColorMultiplier, 0f, 1f), 1f, dayBlend);
            intent.FogDensityMultiplier = Mathf.Lerp(Mathf.Clamp(NightFogDensityMultiplier, 0f, 1f), 1f, dayBlend);
            intent.AmbientSkyContributionMultiplier = Mathf.Lerp(Mathf.Clamp(NightSkyAmbientContributionMultiplier, 0f, 1f), 1f, dayBlend);

            intent.AdjustmentEnabledOverride = true;
            intent.AdjustmentBrightnessMultiplier = Mathf.Lerp(Mathf.Clamp(NightSceneBrightnessMultiplier, 0.01f, 1f), 1f, dayBlend);
            intent.AdjustmentSaturationMultiplier = Mathf.Lerp(Mathf.Clamp(NightSceneSaturationMultiplier, 0.01f, 1f), 1f, dayBlend);

            if (EnableMoonlightAtNight && nightBlend > 0.001f)
            {
                intent.UseMainLightTint = true;
                intent.MainLightTint = MoonlightTint;
                intent.MainLightTintStrength = Mathf.Clamp(MoonlightTintStrength * nightBlend, 0f, 1f);
            }

            if (DisableMainLightShadowsAtNight && nightBlend > 0.5f)
            {
                disableShadows = true;
            }
        }

        if (disableShadows)
        {
            intent.MainLightShadowEnabledOverride = false;
        }

        return intent;
    }

    private void ApplyPrecipitation()
    {
        if (_precipitationParticles == null) return;

        if (_activeWeather == null || _activeWeather.PrecipitationMode == WeatherPrecipitationMode.None)
        {
            DisablePrecipitation();
            return;
        }

        var weather = _activeWeather;
        float density = Mathf.Max(0f, weather.ParticleDensity * RuntimeDensityMultiplier);
        if (density <= 0f)
        {
            DisablePrecipitation();
            return;
        }

        var processMaterial = BuildParticleMaterial(weather);
        var drawMaterial = BuildParticleDrawMaterial(weather);

        _precipitationParticles.ProcessMaterial = processMaterial;
        _precipitationParticles.DrawPass1 = BuildParticleMesh(weather);
        _precipitationParticles.DrawPasses = 1;

        if (_precipitationParticles.DrawPass1 is PrimitiveMesh primitiveMesh)
        {
            primitiveMesh.Material = drawMaterial;
        }

        float fallSpeed = GetResolvedFallSpeed(weather);
        float lifetime = GetParticleLifetime(weather.PrecipitationMode, fallSpeed);
        lifetime *= Mathf.Max(0.1f, weather.ParticleLifetimeMultiplier);
        _precipitationParticles.Amount = Mathf.Clamp(Mathf.RoundToInt(GetBaseParticleCount(weather.PrecipitationMode) * density), 1, 24000);
        _precipitationParticles.Lifetime = lifetime;
        _precipitationParticles.OneShot = false;
        UpdatePrecipitationVisibilityBounds(weather, lifetime, fallSpeed);
        _precipitationParticles.Emitting = true;
        _precipitationParticles.Restart();
    }

    private void DisablePrecipitation()
    {
        if (_precipitationParticles == null) return;

        _precipitationParticles.Emitting = false;
        // Godot requires Amount >= 1 even when emission is disabled.
        if (_precipitationParticles.Amount < 1)
        {
            _precipitationParticles.Amount = 1;
        }
    }

    private ParticleProcessMaterial BuildParticleMaterial(WeatherProfile weather)
    {
        var mat = new ParticleProcessMaterial();
        mat.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box;
        mat.EmissionBoxExtents = new Vector3(
            EmissionAreaSize.X * 0.5f,
            GetResolvedEmissionHeight(weather),
            EmissionAreaSize.Y * 0.5f);

        Vector3 windDir = weather.WindDirection;
        if (windDir.LengthSquared() <= 0.0001f) windDir = Vector3.Right;
        windDir = windDir.Normalized() * (weather.WindStrength * RuntimeWindMultiplier);

        float fall = Mathf.Max(1f, GetResolvedFallSpeed(weather));
        mat.Direction = new Vector3(windDir.X, -fall, windDir.Z).Normalized();
        mat.InitialVelocityMin = fall * 0.8f;
        mat.InitialVelocityMax = fall * 1.2f;
        mat.Gravity = new Vector3(windDir.X * 0.2f, -fall * 2.0f, windDir.Z * 0.2f);
        mat.Spread = 8f;
        mat.DampingMin = 0f;
        mat.DampingMax = 0.2f;

        float scaleMin;
        float scaleMax;
        switch (weather.PrecipitationMode)
        {
            case WeatherPrecipitationMode.Rain:
                scaleMin = 0.65f;
                scaleMax = 1.2f;
                break;
            case WeatherPrecipitationMode.Snow:
                scaleMin = 0.75f;
                scaleMax = 1.35f;
                mat.AngularVelocityMin = -2.5f;
                mat.AngularVelocityMax = 2.5f;
                break;
            case WeatherPrecipitationMode.Hail:
                scaleMin = 0.8f;
                scaleMax = 1.1f;
                break;
            case WeatherPrecipitationMode.Sand:
                scaleMin = 0.7f;
                scaleMax = 1.3f;
                mat.AngularVelocityMin = -1.2f;
                mat.AngularVelocityMax = 1.2f;
                break;
            default:
                scaleMin = 0.8f;
                scaleMax = 1.2f;
                break;
        }

        if (weather.ParticleScaleMinOverride > 0f) scaleMin = weather.ParticleScaleMinOverride;
        if (weather.ParticleScaleMaxOverride > 0f) scaleMax = weather.ParticleScaleMaxOverride;
        if (scaleMax < scaleMin) scaleMax = scaleMin;

        mat.ScaleMin = scaleMin;
        mat.ScaleMax = scaleMax;

        return mat;
    }

    private Material BuildParticleDrawMaterial(WeatherProfile weather)
    {
        Color tint = weather.ParticleTint;
        tint.A = GetResolvedParticleAlpha(weather);

        Texture2D precipitationTexture = ResolvePrecipitationTexture(weather);
        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = tint,
            AlbedoTexture = precipitationTexture,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled
        };
        mat.ProximityFadeEnabled = RuntimeNearCameraExclusionDistance > 0.05f;
        mat.ProximityFadeDistance = Mathf.Max(0.1f, RuntimeNearCameraExclusionDistance);
        return mat;
    }

    private Mesh BuildParticleMesh(WeatherProfile weather)
    {
        return new QuadMesh { Size = GetResolvedParticleSize(weather) };
    }

    private static int GetBaseParticleCount(WeatherPrecipitationMode mode)
    {
        return mode switch
        {
            WeatherPrecipitationMode.Rain => 3600,
            WeatherPrecipitationMode.Snow => 2200,
            WeatherPrecipitationMode.Hail => 2600,
            WeatherPrecipitationMode.Sand => 3000,
            _ => 0
        };
    }

    private static float GetParticleLifetime(WeatherPrecipitationMode mode, float fallSpeed)
    {
        float speed = Mathf.Max(1f, fallSpeed);
        return mode switch
        {
            WeatherPrecipitationMode.Rain => Mathf.Clamp(2.2f * (18f / speed), 0.6f, 5.0f),
            WeatherPrecipitationMode.Snow => Mathf.Clamp(4.8f * (9f / speed), 1.4f, 9.0f),
            WeatherPrecipitationMode.Hail => Mathf.Clamp(1.8f * (20f / speed), 0.5f, 4.0f),
            WeatherPrecipitationMode.Sand => Mathf.Clamp(3.2f * (13f / speed), 0.8f, 6.0f),
            _ => 2.5f
        };
    }

    private void UpdatePrecipitationVisibilityBounds(WeatherProfile weather, float lifetime, float resolvedFallSpeed)
    {
        if (_precipitationParticles == null || weather == null) return;

        float wind = Mathf.Abs(weather.WindStrength * RuntimeWindMultiplier);
        float areaExtent = Mathf.Max(EmissionAreaSize.X, EmissionAreaSize.Y) * 0.5f;
        float horizontalExtent = areaExtent + (wind * lifetime * 2f) + 10f + GetResolvedCameraDepthOffset(weather);
        float downwardTravel = Mathf.Max(12f, resolvedFallSpeed * lifetime * 2.6f);
        float upwardSlack = 10f + GetResolvedEmissionHeight(weather);

        _precipitationParticles.VisibilityAabb = new Aabb(
            new Vector3(-horizontalExtent, -downwardTravel, -horizontalExtent),
            new Vector3(horizontalExtent * 2f, downwardTravel + upwardSlack, horizontalExtent * 2f));
    }

    private void ApplyEnvironmentModifiers()
    {
        var env = WorldEnvironment?.Environment;
        if (env == null) return;

        if (!_capturedEnvironmentBaseline)
        {
            _capturedEnvironmentBaseline = true;
            _baseAmbientLightColor = env.AmbientLightColor;
            _baseFogLightColor = env.FogLightColor;
            _baseAmbientLightEnergy = env.AmbientLightEnergy;
            _baseFogDensity = env.FogDensity;
            _baseAmbientSkyContribution = env.AmbientLightSkyContribution;
            _baseGlowIntensity = env.GlowIntensity;
            _baseAdjustmentEnabled = env.AdjustmentEnabled;
            _baseAdjustmentBrightness = env.AdjustmentBrightness;
            _baseAdjustmentSaturation = env.AdjustmentSaturation;
        }

        if (_activeWeather != null && _activeWeather.EnableEnvironmentTint)
        {
            float t = GetResolvedEnvironmentTintStrength(_activeWeather);
            env.AmbientLightColor = _baseAmbientLightColor.Lerp(_activeWeather.EnvironmentTint, t);
            env.FogEnabled = true;
            env.FogLightColor = _baseFogLightColor.Lerp(_activeWeather.EnvironmentTint, t);
        }
        else
        {
            env.AmbientLightColor = _baseAmbientLightColor;
            env.FogLightColor = _baseFogLightColor;
        }

        if (IsTimeOfDayActive())
        {
            float dayBlend = GetDaylightBlend();
            float ambientColorMul = Mathf.Lerp(Mathf.Clamp(NightAmbientColorMultiplier, 0f, 1f), 1f, dayBlend);
            float fogColorMul = Mathf.Lerp(Mathf.Clamp(NightFogColorMultiplier, 0f, 1f), 1f, dayBlend);
            float fogDensityMul = Mathf.Lerp(Mathf.Clamp(NightFogDensityMultiplier, 0f, 1f), 1f, dayBlend);
            float skyAmbientMul = Mathf.Lerp(Mathf.Clamp(NightSkyAmbientContributionMultiplier, 0f, 1f), 1f, dayBlend);

            env.AmbientLightColor *= ambientColorMul;
            env.FogLightColor *= fogColorMul;
            env.FogDensity = _baseFogDensity * fogDensityMul;
            env.AmbientLightSkyContribution = _baseAmbientSkyContribution * skyAmbientMul;
        }
        else
        {
            env.AmbientLightSkyContribution = _baseAmbientSkyContribution;
        }

        env.AmbientLightEnergy = ResolveWeatherAmbientLightEnergy();

        float glowIntensity = _baseGlowIntensity * ResolveWeatherGlowIntensityMultiplier();
        if (_activeWeather != null && _activeWeather.EnableGlowBoost)
        {
            env.GlowEnabled = true;
        }
        env.GlowIntensity = glowIntensity;

        ApplyTimeOfDayPostAdjustments(env);
    }

    private void ApplyLightModifiers()
    {
        if (MainLight == null) return;

        if (!_capturedLightBaseline)
        {
            _capturedLightBaseline = true;
            _baseLightColor = MainLight.LightColor;
            _baseLightEnergy = MainLight.LightEnergy;
            _baseLightShadowEnabled = MainLight.ShadowEnabled;
        }

        Color resolvedColor = _baseLightColor;
        if (_activeWeather != null && _activeWeather.EnableLightColorOverride)
        {
            resolvedColor = _activeWeather.LightColorOverride;
        }

        if (IsTimeOfDayActive())
        {
            float nightBlend = GetNightBlend();
            if (EnableMoonlightAtNight && nightBlend > 0.001f)
            {
                float tintStrength = Mathf.Clamp(MoonlightTintStrength * nightBlend, 0f, 1f);
                resolvedColor = resolvedColor.Lerp(MoonlightTint, tintStrength);
            }
        }

        MainLight.LightColor = resolvedColor;
        MainLight.LightEnergy = ResolveWeatherMainLightEnergy();
        MainLight.ShadowEnabled = ResolveMainLightShadowEnabled();
    }

    private void ApplyLightningState()
    {
        if (_lightningTimer == null || _lightningFlash == null) return;

        if (_activeWeather == null || !_activeWeather.EnableLightningFlashes)
        {
            _lightningTimer.Stop();
            _lightningFlash.LightEnergy = 0f;
            _lightningSceneTween?.Kill();
            _lightningOverlayTween?.Kill();
            SetLightningOverlayAlpha(0f);
            RestoreWeatherLightState();
            return;
        }

        ScheduleNextLightning();
    }

    private void ScheduleNextLightning()
    {
        if (_activeWeather == null || !_activeWeather.EnableLightningFlashes || _lightningTimer == null) return;
        if (_lightningTimer.TimeLeft > 0f) return;

        float intervalScale = Mathf.Max(0.05f, RuntimeLightningIntervalMultiplier);
        float min = Mathf.Min(_activeWeather.LightningFlashIntervalMin, _activeWeather.LightningFlashIntervalMax) * intervalScale;
        float max = Mathf.Max(_activeWeather.LightningFlashIntervalMin, _activeWeather.LightningFlashIntervalMax) * intervalScale;
        _lightningTimer.WaitTime = _rng.RandfRange(Mathf.Max(0.1f, min), Mathf.Max(0.1f, max));
        _lightningTimer.Start();
    }

    private void OnLightningTimerTimeout()
    {
        if (_activeWeather == null || !_activeWeather.EnableLightningFlashes) return;
        TriggerLightningFlash();
        ScheduleNextLightning();
    }

    private void TriggerLightningFlash()
    {
        if (_lightningFlash == null || _activeWeather == null) return;

        float intensityScale = Mathf.Max(0f, RuntimeLightningIntensityMultiplier);
        float duration = Mathf.Max(0.05f, _activeWeather.LightningFlashDuration);

        PositionLightningFlash();
        _lightningFlash.LightColor = _activeWeather.LightningFlashColor;
        _lightningFlash.OmniRange = Mathf.Max(4f, _activeWeather.LightningFlashRange);
        _lightningFlash.LightEnergy = Mathf.Max(0f, _activeWeather.LightningFlashIntensity * intensityScale);

        _lightningSceneTween?.Kill();
        _lightningSceneTween = CreateTween();
        _lightningSceneTween.SetParallel(true);
        _lightningSceneTween.TweenProperty(_lightningFlash, "light_energy", 0f, duration);

        if (MainLight != null && _activeWeather.LightningMainLightEnergyBoost > 1f)
        {
            float baseMainLightEnergy = UseVisualDirector
                ? MainLight.LightEnergy
                : ResolveWeatherMainLightEnergy();
            float boostScale = Mathf.Max(0.15f, intensityScale);
            float boostedEnergy = baseMainLightEnergy * _activeWeather.LightningMainLightEnergyBoost * boostScale;
            MainLight.LightEnergy = Mathf.Max(MainLight.LightEnergy, boostedEnergy);
            _lightningSceneTween.TweenProperty(MainLight, "light_energy", baseMainLightEnergy, duration);
        }

        var env = WorldEnvironment?.Environment;
        float envFlashStrength = Mathf.Clamp(_activeWeather.LightningEnvironmentFlashStrength * Mathf.Max(0.15f, intensityScale), 0f, 1f);
        if (env != null && envFlashStrength > 0f)
        {
            Color ambientBase = UseVisualDirector ? env.AmbientLightColor : ResolveWeatherAmbientLightColor();
            Color fogBase = UseVisualDirector ? env.FogLightColor : ResolveWeatherFogLightColor();
            Color flashColor = _activeWeather.LightningFlashColor;
            env.AmbientLightColor = ambientBase.Lerp(flashColor, envFlashStrength);
            env.FogLightColor = fogBase.Lerp(flashColor, envFlashStrength * 0.9f);
            _lightningSceneTween.TweenProperty(env, "ambient_light_color", ambientBase, duration);
            _lightningSceneTween.TweenProperty(env, "fog_light_color", fogBase, duration);
        }

        float overlayAlpha = Mathf.Clamp(_activeWeather.LightningScreenFlashStrength * Mathf.Max(0.15f, intensityScale), 0f, 1f);
        if (_lightningOverlayRect != null && overlayAlpha > 0f)
        {
            _lightningOverlayTween?.Kill();
            _lightningOverlayTint = _activeWeather.LightningFlashColor;
            SetLightningOverlayAlpha(overlayAlpha);
            _lightningOverlayTween = CreateTween();
            _lightningOverlayTween.TweenMethod(Callable.From<float>(SetLightningOverlayAlpha), overlayAlpha, 0f, duration * 1.15f);
        }

        var lightningSfx = ResolveLightningSfx();
        if (lightningSfx != null)
        {
            _oneshotPlayer.Stream = lightningSfx;
            _oneshotPlayer.VolumeDb = _activeWeather.LightningSfxVolumeDb;
            _oneshotPlayer.PitchScale = _rng.RandfRange(0.95f, 1.05f);
            _oneshotPlayer.Play();
        }
    }

    private AudioStream ResolveLightningSfx()
    {
        if (_activeWeather == null) return null;

        if (_activeWeather.LightningSfxOptions != null && _activeWeather.LightningSfxOptions.Count > 0)
        {
            var valid = new List<AudioStream>();
            foreach (var stream in _activeWeather.LightningSfxOptions)
            {
                if (stream != null)
                {
                    valid.Add(stream);
                }
            }

            if (valid.Count > 0)
            {
                int index = _rng.RandiRange(0, valid.Count - 1);
                return valid[index];
            }
        }

        return _activeWeather.LightningSfx;
    }

    private void UpdateTimeOfDay(double delta)
    {
        bool timeOfDayActive = IsTimeOfDayActive();
        if (!timeOfDayActive)
        {
            if (_lastTimeOfDayActive)
            {
                _lastTimeOfDayActive = false;
                _lastAppliedTimeOfDayHours = float.NaN;
                if (UseVisualDirector)
                {
                    PushWeatherVisualContribution();
                }
                else
                {
                    ApplyLightModifiers();
                    ApplyEnvironmentModifiers();
                }
            }
            return;
        }

        bool changed = false;
        if (AutoAdvanceTimeOfDay)
        {
            float cycleMinutes = Mathf.Max(0.1f, FullDayDurationMinutes);
            float hoursPerSecond = 24f / (cycleMinutes * 60f);
            TimeOfDayHours = Mathf.PosMod(TimeOfDayHours + ((float)delta * hoursPerSecond), 24f);
            changed = true;
        }

        if (!_lastTimeOfDayActive || !Mathf.IsEqualApprox(_lastAppliedTimeOfDayHours, TimeOfDayHours))
        {
            changed = true;
        }
        if (!changed) return;

        ApplyMainLightTimeOfDayOrientation();
        if (UseVisualDirector)
        {
            PushWeatherVisualContribution();
        }
        else
        {
            ApplyLightModifiers();
            ApplyEnvironmentModifiers();
        }

        _lastTimeOfDayActive = true;
        _lastAppliedTimeOfDayHours = TimeOfDayHours;
    }

    private void ApplyMainLightTimeOfDayOrientation()
    {
        if (MainLight == null) return;

        float elevation = GetSolarElevationNormalized() * Mathf.Clamp(MaxSolarAltitudeDegrees, 0f, 89f);
        float elevationRad = Mathf.DegToRad(elevation);
        float azimuthRad = Mathf.DegToRad(SolarAzimuthDegrees);

        Vector3 sunDirection = new Vector3(
            Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad),
            Mathf.Sin(elevationRad),
            Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad));
        Vector3 rayDirection = (-sunDirection).Normalized();

        if (rayDirection.LengthSquared() <= 0.00001f)
        {
            rayDirection = Vector3.Down;
        }

        MainLight.Basis = Basis.LookingAt(rayDirection, Vector3.Up);
    }

    private bool IsTimeOfDayActive()
    {
        return EnableTimeOfDay && TreatAsOutdoorScene;
    }

    private float GetSolarElevationNormalized()
    {
        float phase = Mathf.PosMod(TimeOfDayHours, 24f) / 24f;
        return Mathf.Sin((phase - 0.25f) * Mathf.Tau);
    }

    private float GetDaylightBlend()
    {
        if (!IsTimeOfDayActive()) return 1f;
        float softness = Mathf.Clamp(TwilightSoftness, 0.01f, 1f);
        float elevation = GetSolarElevationNormalized();
        return Mathf.SmoothStep(-softness, softness, elevation);
    }

    private float GetNightBlend()
    {
        return 1f - GetDaylightBlend();
    }

    private void PositionLightningFlash()
    {
        if (_lightningFlash == null || _activeWeather == null) return;

        var camera = ResolveWeatherCamera();
        if (camera == null || !GodotObject.IsInstanceValid(camera))
        {
            _lightningFlash.GlobalPosition = GlobalPosition + (Vector3.Up * _activeWeather.LightningFlashVerticalOffset);
            return;
        }

        _lightningFlash.GlobalPosition = camera.GlobalPosition
            + (camera.GlobalBasis.Y * _activeWeather.LightningFlashVerticalOffset)
            + ((-camera.GlobalBasis.Z) * _activeWeather.LightningFlashForwardOffset);
    }

    private Camera3D ResolveWeatherCamera()
    {
        var camera = WeatherCamera;
        if (camera == null || !GodotObject.IsInstanceValid(camera))
        {
            camera = GetViewport()?.GetCamera3D();
            if (camera != null && GodotObject.IsInstanceValid(camera))
            {
                WeatherCamera = camera;
            }
        }

        return camera;
    }

    private void SetLightningOverlayAlpha(float alpha)
    {
        if (_lightningOverlayRect == null) return;
        var color = _lightningOverlayTint;
        color.A = Mathf.Clamp(alpha, 0f, 1f);
        _lightningOverlayRect.Color = color;
    }

    private void RestoreWeatherLightState()
    {
        if (UseVisualDirector)
        {
            VisualDirector.Reapply();
            return;
        }

        if (MainLight != null)
        {
            MainLight.LightEnergy = ResolveWeatherMainLightEnergy();
            MainLight.ShadowEnabled = ResolveMainLightShadowEnabled();
        }

        var env = WorldEnvironment?.Environment;
        if (env != null)
        {
            env.AmbientLightColor = ResolveWeatherAmbientLightColor();
            env.FogLightColor = ResolveWeatherFogLightColor();
            env.AmbientLightEnergy = ResolveWeatherAmbientLightEnergy();
        }
    }

    private Color ResolveWeatherAmbientLightColor()
    {
        if (_activeWeather == null || !_activeWeather.EnableEnvironmentTint)
        {
            return _baseAmbientLightColor;
        }

        float t = GetResolvedEnvironmentTintStrength(_activeWeather);
        return _baseAmbientLightColor.Lerp(_activeWeather.EnvironmentTint, t);
    }

    private Color ResolveWeatherFogLightColor()
    {
        if (_activeWeather == null || !_activeWeather.EnableEnvironmentTint)
        {
            return _baseFogLightColor;
        }

        float t = GetResolvedEnvironmentTintStrength(_activeWeather);
        return _baseFogLightColor.Lerp(_activeWeather.EnvironmentTint, t);
    }

    private float ResolveWeatherAmbientLightEnergy()
    {
        return _baseAmbientLightEnergy * ResolveWeatherAmbientEnergyMultiplier();
    }

    private float ResolveWeatherAmbientEnergyMultiplier()
    {
        float multiplier = 1f;
        if (_activeWeather != null && _activeWeather.EnableOvercastDiffuseLighting)
        {
            float overcastAmbientMultiplier = Mathf.Max(0.05f, _activeWeather.OvercastAmbientLightEnergyMultiplier);
            if (IsTimeOfDayActive())
            {
                // Keep overcast diffuse boost primarily a daytime behavior.
                // At night, ambient should be governed by night multipliers instead of brightening from cloud cover.
                float dayBlend = GetDaylightBlend();
                multiplier *= Mathf.Lerp(1f, overcastAmbientMultiplier, dayBlend);
            }
            else
            {
                multiplier *= overcastAmbientMultiplier;
            }
        }

        if (IsTimeOfDayActive())
        {
            multiplier *= Mathf.Lerp(Mathf.Max(0.05f, NightAmbientEnergyMultiplier), 1f, GetDaylightBlend());
        }

        return multiplier;
    }

    private float ResolveWeatherMainLightEnergy()
    {
        return _baseLightEnergy * ResolveWeatherMainLightEnergyMultiplier();
    }

    private float ResolveWeatherMainLightEnergyMultiplier()
    {
        float multiplier = 1f;
        if (_activeWeather != null)
        {
            multiplier *= GetResolvedLightEnergyMultiplier(_activeWeather);
        }

        if (_activeWeather != null && _activeWeather.EnableOvercastDiffuseLighting)
        {
            multiplier *= Mathf.Clamp(_activeWeather.OvercastDirectionalLightEnergyMultiplier, 0f, 1f);
        }

        if (IsTimeOfDayActive())
        {
            float dayBlend = GetDaylightBlend();
            if (EnableMoonlightAtNight)
            {
                multiplier *= Mathf.Lerp(Mathf.Clamp(MoonlightDirectionalEnergyMultiplier, 0f, 1f), 1f, dayBlend);
            }
            else
            {
                multiplier *= dayBlend;
            }
        }

        return multiplier;
    }

    private float ResolveWeatherGlowIntensityMultiplier()
    {
        float multiplier = 1f;
        if (_activeWeather != null && _activeWeather.EnableGlowBoost)
        {
            multiplier *= GetResolvedGlowIntensityMultiplier(_activeWeather);
        }

        if (IsTimeOfDayActive())
        {
            multiplier *= Mathf.Lerp(
                Mathf.Clamp(NightGlowIntensityMultiplier, 0.01f, 1f),
                1f,
                GetDaylightBlend());
        }

        return multiplier;
    }

    private void ApplyTimeOfDayPostAdjustments(Environment env)
    {
        if (env == null) return;

        if (!IsTimeOfDayActive())
        {
            env.AdjustmentEnabled = _baseAdjustmentEnabled;
            env.AdjustmentBrightness = _baseAdjustmentBrightness;
            env.AdjustmentSaturation = _baseAdjustmentSaturation;
            return;
        }

        float dayBlend = GetDaylightBlend();
        float nightBrightness = _baseAdjustmentBrightness * Mathf.Clamp(NightSceneBrightnessMultiplier, 0.01f, 1f);
        float nightSaturation = _baseAdjustmentSaturation * Mathf.Clamp(NightSceneSaturationMultiplier, 0.01f, 1f);

        env.AdjustmentEnabled = true;
        env.AdjustmentBrightness = Mathf.Lerp(nightBrightness, _baseAdjustmentBrightness, dayBlend);
        env.AdjustmentSaturation = Mathf.Lerp(nightSaturation, _baseAdjustmentSaturation, dayBlend);
    }

    private bool ResolveMainLightShadowEnabled()
    {
        bool shadows = _baseLightShadowEnabled;

        if (_activeWeather != null
            && _activeWeather.EnableOvercastDiffuseLighting
            && _activeWeather.DisableMainLightShadowsInOvercast)
        {
            shadows = false;
        }

        if (IsTimeOfDayActive()
            && DisableMainLightShadowsAtNight
            && GetNightBlend() > 0.5f)
        {
            shadows = false;
        }

        return shadows;
    }

    private void UpdateParticleAnchor()
    {
        if (_precipitationParticles == null || !_precipitationParticles.Emitting) return;
        if (!FollowCamera) return;

        var camera = ResolveWeatherCamera();
        if (camera == null || !GodotObject.IsInstanceValid(camera)) return;

        Vector3 anchor = camera.GlobalPosition + CameraFollowOffset;
        if (_activeWeather != null)
        {
            float resolvedDepthOffset = GetResolvedCameraDepthOffset(_activeWeather);
            if (EmitInCameraForwardOnly)
            {
                float minForwardDepth = (EmissionAreaSize.Y * 0.5f) + RuntimeNearCameraExclusionDistance;
                resolvedDepthOffset = Mathf.Max(resolvedDepthOffset, minForwardDepth);
            }

            anchor += (-camera.GlobalBasis.Z) * resolvedDepthOffset;
            _precipitationParticles.GlobalBasis = EmitInCameraForwardOnly ? camera.GlobalBasis : Basis.Identity;
        }

        _precipitationParticles.GlobalPosition = anchor;
    }

    private static Vector2 GetDefaultParticleSize(WeatherPrecipitationMode mode)
    {
        return mode switch
        {
            WeatherPrecipitationMode.Rain => new Vector2(0.022f, 0.26f),
            WeatherPrecipitationMode.Snow => new Vector2(0.11f, 0.11f),
            WeatherPrecipitationMode.Hail => new Vector2(0.07f, 0.07f),
            WeatherPrecipitationMode.Sand => new Vector2(0.06f, 0.06f),
            _ => new Vector2(0.05f, 0.05f)
        };
    }

    private Vector2 GetResolvedParticleSize(WeatherProfile weather)
    {
        Vector2 size = GetDefaultParticleSize(weather.PrecipitationMode);

        if (weather.ParticleWidthOverride > 0f)
        {
            size.X = weather.ParticleWidthOverride;
        }

        if (weather.ParticleLengthOverride > 0f)
        {
            size.Y = weather.ParticleLengthOverride;
        }

        size.X *= Mathf.Max(0.1f, RuntimeParticleWidthMultiplier);
        size.Y *= Mathf.Max(0.1f, RuntimeParticleLengthMultiplier);

        size.X = Mathf.Max(0.001f, size.X);
        size.Y = Mathf.Max(0.001f, size.Y);
        return size;
    }

    private float GetResolvedParticleAlpha(WeatherProfile weather)
    {
        float alpha = weather.ParticleTint.A;
        alpha *= Mathf.Max(0f, weather.ParticleAlphaMultiplier);
        alpha *= Mathf.Max(0f, RuntimeParticleAlphaMultiplier);
        return Mathf.Clamp(alpha, 0f, 1f);
    }

    private float GetResolvedCameraDepthOffset(WeatherProfile weather)
    {
        return Mathf.Max(0f, weather.CameraDepthOffset + RuntimeCameraDepthOffset);
    }

    private Texture2D ResolvePrecipitationTexture(WeatherProfile weather)
    {
        if (weather?.ParticleTexture != null)
        {
            return weather.ParticleTexture;
        }

        WeatherPrecipitationMode mode = weather?.PrecipitationMode ?? WeatherPrecipitationMode.None;
        if (!_defaultPrecipitationTextures.TryGetValue(mode, out var texture) || texture == null)
        {
            texture = BuildDefaultPrecipitationTexture(mode);
            _defaultPrecipitationTextures[mode] = texture;
        }

        return texture;
    }

    private static Texture2D BuildDefaultPrecipitationTexture(WeatherPrecipitationMode mode)
    {
        return mode switch
        {
            WeatherPrecipitationMode.Rain => BuildRainDropTexture(),
            WeatherPrecipitationMode.Snow => BuildSoftDiscTexture(28, 0.48f),
            WeatherPrecipitationMode.Hail => BuildSoftDiscTexture(20, 0.30f),
            WeatherPrecipitationMode.Sand => BuildSoftDiscTexture(16, 0.62f),
            _ => BuildSoftDiscTexture(16, 0.45f)
        };
    }

    private static Texture2D BuildRainDropTexture()
    {
        const int width = 12;
        const int height = 56;
        Image image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / (height - 1);
            // Bright center, softer tail and tip.
            float longitudinal = Mathf.Pow(1f - Mathf.Abs((v * 2f) - 1f), 0.55f);
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / (width - 1);
                float radial = 1f - Mathf.Abs((u * 2f) - 1f);
                radial = Mathf.Pow(Mathf.Max(0f, radial), 1.35f);

                float alpha = longitudinal * radial;
                // Soften the very top/bottom to avoid hard rectangular traces.
                if (v < 0.06f) alpha *= (v / 0.06f);
                if (v > 0.94f) alpha *= ((1f - v) / 0.06f);

                image.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp(alpha, 0f, 1f)));
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D BuildSoftDiscTexture(int size, float hardness)
    {
        int s = Mathf.Max(4, size);
        Image image = Image.CreateEmpty(s, s, false, Image.Format.Rgba8);
        Vector2 center = new Vector2((s - 1) * 0.5f, (s - 1) * 0.5f);
        float radius = s * 0.5f;
        float exponent = Mathf.Lerp(1.35f, 3.3f, Mathf.Clamp(hardness, 0f, 1f));

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Vector2 p = new Vector2(x, y);
                float dist = p.DistanceTo(center) / radius;
                float falloff = Mathf.Clamp(1f - dist, 0f, 1f);
                float alpha = Mathf.Pow(falloff, exponent);
                image.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    private float GetResolvedFallSpeed(WeatherProfile weather)
    {
        return Mathf.Max(0.1f, weather.FallSpeed * Mathf.Max(0.05f, RuntimeFallSpeedMultiplier));
    }

    private float GetResolvedEmissionHeight(WeatherProfile weather)
    {
        return Mathf.Max(0.5f, weather.EmissionHeight * Mathf.Max(0.05f, RuntimeEmissionHeightMultiplier));
    }

    private float GetResolvedEnvironmentTintStrength(WeatherProfile weather)
    {
        float strength = weather.EnvironmentTintStrength * Mathf.Max(0f, RuntimeEnvironmentTintStrengthMultiplier);
        return Mathf.Clamp(strength, 0f, 1f);
    }

    private float GetResolvedLightEnergyMultiplier(WeatherProfile weather)
    {
        return Mathf.Max(0.05f, weather.LightEnergyMultiplier * Mathf.Max(0.05f, RuntimeLightEnergyMultiplier));
    }

    private float GetResolvedGlowIntensityMultiplier(WeatherProfile weather)
    {
        return Mathf.Max(0f, weather.GlowIntensityMultiplier * Mathf.Max(0.05f, RuntimeGlowIntensityMultiplier));
    }

}
