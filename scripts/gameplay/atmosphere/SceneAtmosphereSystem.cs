using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scene-level atmosphere orchestrator.
/// Delegates rendering/state behavior to pluggable atmosphere layers.
/// </summary>
[GlobalClass]
public partial class SceneAtmosphereSystem : Node3D
{
    public const string NodeName = "SceneAtmosphereSystem";

    [Signal]
    public delegate void AtmosphereChangedEventHandler(SceneAtmosphereProfile profile);

    [ExportGroup("Atmosphere")]
    [Export]
    public SceneAtmosphereProfile InitialProfile { get; private set; }

    [Export]
    public Godot.Collections.Array<SceneAtmosphereProfile> ProfileLibrary { get; private set; } = new();

    [Export]
    public bool ApplyInitialProfileOnReady { get; private set; } = false;

    [ExportGroup("Scene References")]
    [Export]
    public WorldEnvironment WorldEnvironment { get; private set; }

    [Export]
    public DirectionalLight3D MainLight { get; private set; }

    [Export]
    public Camera3D AtmosphereCamera { get; private set; }

    [Export]
    public SceneVisualDirector VisualDirector { get; private set; }

    [ExportGroup("Placement")]
    [Export]
    public bool FollowCamera { get; private set; } = true;

    [Export]
    public Vector3 CameraFollowOffset { get; private set; } = Vector3.Zero;

    [ExportGroup("Runtime Multipliers")]
    [Export(PropertyHint.Range, "0,2,0.01")]
    public float RuntimeCanopyMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0,3,0.01")]
    public float RuntimeSunShaftIntensityMultiplier { get; private set; } = 1f;

    [Export(PropertyHint.Range, "0,3,0.01")]
    public float RuntimeDiffuseFillIntensityMultiplier { get; private set; } = 1f;

    private SceneAtmosphereProfile _activeProfile;
    private readonly RandomNumberGenerator _rng = new();
    private SceneAtmosphereRuntimeContext _runtimeContext;
    private readonly List<IAtmosphereLayer> _layers = new();
    private bool _layersConfigured;
    private bool _configuredForVisualDirector;
    private string _visualContributionId;

    public SceneAtmosphereProfile ActiveProfile => _activeProfile;
    private bool UseVisualDirector => VisualDirector != null && GodotObject.IsInstanceValid(VisualDirector);

    public override void _Ready()
    {
        _rng.Randomize();
        ResolveSceneReferences();
        EnsureRuntimePipeline();
        SyncRuntimeContext();
        _visualContributionId = $"{Name}:{GetInstanceId()}:Atmosphere";

        if (ApplyInitialProfileOnReady && InitialProfile != null)
        {
            SetProfile(InitialProfile);
        }
    }

    public override void _ExitTree()
    {
        ClearVisualContribution();
    }

    public override void _Process(double delta)
    {
        ResolveSceneReferences();
        EnsureRuntimePipeline();
        SyncRuntimeContext();

        UpdateAnchor();
        if (_activeProfile == null) return;

        _runtimeContext.TimeSeconds += Mathf.Max(0f, (float)delta);
        foreach (var layer in _layers)
        {
            layer.Update(_runtimeContext, _activeProfile, Mathf.Max(0f, (float)delta));
        }

        PushVisualContribution();
    }

    public void SetProfile(SceneAtmosphereProfile profile)
    {
        ResolveSceneReferences();
        EnsureRuntimePipeline();
        SyncRuntimeContext();

        if (_activeProfile == profile)
        {
            ReapplyVisuals();
            return;
        }

        _activeProfile = profile;
        ReapplyVisuals();
        EmitSignal(SignalName.AtmosphereChanged, profile);
    }

    public void ClearProfile()
    {
        SetProfile(null);
    }

    public void SetCanopyMultiplier(float multiplier)
    {
        RuntimeCanopyMultiplier = Mathf.Max(0f, multiplier);
        ReapplyVisuals();
    }

    public void SetSunShaftIntensityMultiplier(float multiplier)
    {
        RuntimeSunShaftIntensityMultiplier = Mathf.Max(0f, multiplier);
        ReapplyVisuals();
    }

    public void SetDiffuseFillIntensityMultiplier(float multiplier)
    {
        RuntimeDiffuseFillIntensityMultiplier = Mathf.Max(0f, multiplier);
        ReapplyVisuals();
    }

    public void AddLayer(IAtmosphereLayer layer, bool reapplyIfActive = true)
    {
        if (layer == null) return;
        EnsureRuntimePipeline();

        _layers.Add(layer);
        if (reapplyIfActive)
        {
            ReapplyVisuals();
        }
    }

    public bool RemoveLayer(IAtmosphereLayer layer, bool reapplyIfActive = true)
    {
        if (layer == null) return false;
        EnsureRuntimePipeline();

        bool removed = _layers.Remove(layer);
        if (!removed) return false;

        layer.Clear(_runtimeContext);
        if (reapplyIfActive)
        {
            ReapplyVisuals();
        }

        return true;
    }

    public void SetLayers(IEnumerable<IAtmosphereLayer> layers, bool reapplyIfActive = true)
    {
        EnsureRuntimePipeline();

        foreach (var layer in _layers)
        {
            layer.Clear(_runtimeContext);
        }

        _layers.Clear();
        if (layers != null)
        {
            foreach (var layer in layers.Where(l => l != null))
            {
                _layers.Add(layer);
            }
        }

        if (reapplyIfActive)
        {
            ReapplyVisuals();
        }
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

        if (AtmosphereCamera == null)
        {
            AtmosphereCamera = GetViewport()?.GetCamera3D();
            if (AtmosphereCamera == null)
            {
                AtmosphereCamera = GetTree()?.CurrentScene?.FindChild("Camera3D", true, false) as Camera3D;
            }
        }

        if (VisualDirector == null)
        {
            VisualDirector = GetTree()?.CurrentScene?.FindChild(SceneVisualDirector.NodeName, true, false) as SceneVisualDirector;
        }
    }

    private void EnsureRuntimePipeline()
    {
        _runtimeContext ??= new SceneAtmosphereRuntimeContext(this, _rng);
        bool useVisualDirector = UseVisualDirector;
        if (_layersConfigured && _configuredForVisualDirector == useVisualDirector) return;

        foreach (var layer in _layers)
        {
            layer.Clear(_runtimeContext);
        }

        _layers.Clear();
        if (!useVisualDirector)
        {
            _layers.Add(new AmbientFogAtmosphereLayer());
            _layers.Add(new MainLightAtmosphereLayer());
        }
        _layers.Add(new SunShaftAtmosphereLayer());
        _layers.Add(new DiffuseFillAtmosphereLayer());
        _layersConfigured = true;
        _configuredForVisualDirector = useVisualDirector;
    }

    private void SyncRuntimeContext()
    {
        if (_runtimeContext == null) return;

        _runtimeContext.WorldEnvironment = WorldEnvironment;
        _runtimeContext.MainLight = MainLight;
        _runtimeContext.AtmosphereCamera = AtmosphereCamera;
        _runtimeContext.RuntimeCanopyMultiplier = RuntimeCanopyMultiplier;
        _runtimeContext.RuntimeSunShaftIntensityMultiplier = RuntimeSunShaftIntensityMultiplier;
        _runtimeContext.RuntimeDiffuseFillIntensityMultiplier = RuntimeDiffuseFillIntensityMultiplier;
    }

    private void ReapplyVisuals()
    {
        EnsureRuntimePipeline();
        SyncRuntimeContext();

        if (_activeProfile == null)
        {
            foreach (var layer in _layers)
            {
                layer.Clear(_runtimeContext);
            }
            ClearVisualContribution();
            return;
        }

        foreach (var layer in _layers)
        {
            layer.Apply(_runtimeContext, _activeProfile);
        }

        PushVisualContribution();
    }

    private void UpdateAnchor()
    {
        if (!FollowCamera) return;

        var camera = AtmosphereCamera;
        if (camera == null || !GodotObject.IsInstanceValid(camera))
        {
            camera = GetViewport()?.GetCamera3D();
            if (camera == null || !GodotObject.IsInstanceValid(camera))
            {
                return;
            }

            AtmosphereCamera = camera;
        }

        GlobalPosition = camera.GlobalPosition + CameraFollowOffset;
    }

    private void PushVisualContribution()
    {
        if (!UseVisualDirector || _activeProfile == null || _runtimeContext == null) return;
        VisualDirector.SetContribution(_visualContributionId, BuildAtmosphereVisualIntent(_activeProfile));
    }

    private void ClearVisualContribution()
    {
        if (!UseVisualDirector) return;
        VisualDirector.ClearContribution(_visualContributionId);
    }

    private SceneVisualIntent BuildAtmosphereVisualIntent(SceneAtmosphereProfile profile)
    {
        float canopy = _runtimeContext.GetResolvedCanopy(profile);
        float sunHeight = _runtimeContext.ComputeSunHeightFactor();

        var intent = new SceneVisualIntent
        {
            Layer = (int)SceneVisualContributionLayer.BaseBiome
        };

        if (profile.EnableAmbientTint)
        {
            intent.UseAmbientTint = true;
            intent.AmbientTint = profile.AmbientTint;
            intent.AmbientTintStrength = Mathf.Clamp(profile.AmbientTintStrength * canopy, 0f, 1f);
        }

        float fogDensityAdd = Mathf.Max(0f, profile.FogDensityBoost * canopy);
        intent.FogDensityAdd = fogDensityAdd;
        if (profile.FogTintStrength > 0f)
        {
            intent.UseFogTint = true;
            intent.FogTint = profile.FogTint;
            intent.FogTintStrength = Mathf.Clamp(profile.FogTintStrength * canopy, 0f, 1f);
        }
        if (fogDensityAdd > 0f || intent.FogTintStrength > 0.01f)
        {
            intent.FogEnabledOverride = true;
        }

        if (profile.EnableMainLightAdjustments)
        {
            float energyMul = Mathf.Lerp(
                profile.MainLightEnergySparseCanopy,
                profile.MainLightEnergyDenseCanopy,
                canopy);
            energyMul *= Mathf.Lerp(0.85f, 1.05f, sunHeight);
            intent.MainLightEnergyMultiplier = Mathf.Max(0.05f, energyMul);

            float tintT = Mathf.Clamp(profile.MainLightTintStrength * canopy, 0f, 1f);
            if (tintT > 0.001f)
            {
                intent.UseMainLightTint = true;
                intent.MainLightTint = profile.MainLightDenseCanopyTint;
                intent.MainLightTintStrength = tintT;
            }
        }

        return intent;
    }
}
