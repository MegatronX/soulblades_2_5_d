using Godot;

/// <summary>
/// Shared runtime context for atmosphere layers.
/// Holds resolved scene references, runtime multipliers, and baseline state.
/// </summary>
public sealed class SceneAtmosphereRuntimeContext
{
    public SceneAtmosphereRuntimeContext(Node3D systemNode, RandomNumberGenerator rng)
    {
        SystemNode = systemNode;
        Rng = rng ?? new RandomNumberGenerator();
    }

    public Node3D SystemNode { get; }
    public RandomNumberGenerator Rng { get; }

    public WorldEnvironment WorldEnvironment { get; set; }
    public DirectionalLight3D MainLight { get; set; }
    public Camera3D AtmosphereCamera { get; set; }

    public float RuntimeCanopyMultiplier { get; set; } = 1f;
    public float RuntimeSunShaftIntensityMultiplier { get; set; } = 1f;
    public float RuntimeDiffuseFillIntensityMultiplier { get; set; } = 1f;
    public float TimeSeconds { get; set; }

    private bool _hasEnvironmentBaseline;
    private bool _baseFogEnabled;
    private Color _baseAmbientColor = Colors.White;
    private Color _baseFogColor = Colors.White;
    private float _baseFogDensity;

    private bool _hasMainLightBaseline;
    private Color _baseMainLightColor = Colors.White;
    private float _baseMainLightEnergy = 1f;

    public Color BaseAmbientColor => _baseAmbientColor;
    public Color BaseFogColor => _baseFogColor;
    public float BaseFogDensity => _baseFogDensity;
    public bool BaseFogEnabled => _baseFogEnabled;

    public Color BaseMainLightColor => _baseMainLightColor;
    public float BaseMainLightEnergy => _baseMainLightEnergy;

    public void CaptureEnvironmentBaseline()
    {
        if (_hasEnvironmentBaseline) return;

        var env = WorldEnvironment?.Environment;
        if (env == null) return;

        _baseAmbientColor = env.AmbientLightColor;
        _baseFogColor = env.FogLightColor;
        _baseFogDensity = env.FogDensity;
        _baseFogEnabled = env.FogEnabled;
        _hasEnvironmentBaseline = true;
    }

    public void RestoreEnvironmentBaseline()
    {
        if (!_hasEnvironmentBaseline) return;

        var env = WorldEnvironment?.Environment;
        if (env == null) return;

        env.AmbientLightColor = _baseAmbientColor;
        env.FogLightColor = _baseFogColor;
        env.FogDensity = _baseFogDensity;
        env.FogEnabled = _baseFogEnabled;
    }

    public void CaptureMainLightBaseline()
    {
        if (_hasMainLightBaseline) return;
        if (MainLight == null) return;

        _baseMainLightColor = MainLight.LightColor;
        _baseMainLightEnergy = MainLight.LightEnergy;
        _hasMainLightBaseline = true;
    }

    public void RestoreMainLightBaseline()
    {
        if (!_hasMainLightBaseline) return;
        if (MainLight == null) return;

        MainLight.LightColor = _baseMainLightColor;
        MainLight.LightEnergy = _baseMainLightEnergy;
    }

    public float GetResolvedCanopy(SceneAtmosphereProfile profile)
    {
        if (profile == null) return 0f;
        return Mathf.Clamp(profile.CanopyThickness * RuntimeCanopyMultiplier, 0f, 1f);
    }

    public float ComputeSunHeightFactor()
    {
        if (MainLight == null) return 1f;

        Vector3 lightDir = (-MainLight.GlobalBasis.Z).Normalized();
        return Mathf.Clamp(-lightDir.Y, 0f, 1f);
    }
}
