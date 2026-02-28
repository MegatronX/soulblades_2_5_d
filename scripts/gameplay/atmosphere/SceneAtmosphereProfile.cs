using Godot;

/// <summary>
/// Scene atmosphere profile for biome-style presentation layers (forest canopy, shafts, diffuse fill).
/// Works alongside weather and battle systems.
/// </summary>
[GlobalClass]
public partial class SceneAtmosphereProfile : Resource
{
    [ExportGroup("Identity")]
    [Export]
    public string ProfileName { get; private set; } = "Atmosphere";

    [Export(PropertyHint.MultilineText)]
    public string Description { get; private set; } = string.Empty;

    [ExportGroup("Core")]
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float CanopyThickness { get; private set; } = 0.5f;

    [ExportGroup("Ambient and Fog")]
    [Export]
    public bool EnableAmbientTint { get; private set; } = true;

    [Export]
    public Color AmbientTint { get; private set; } = new Color(0.74f, 0.83f, 0.70f, 1f);

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float AmbientTintStrength { get; private set; } = 0.34f;

    [Export(PropertyHint.Range, "0,0.05,0.0001")]
    public float FogDensityBoost { get; private set; } = 0.0045f;

    [Export]
    public Color FogTint { get; private set; } = new Color(0.66f, 0.75f, 0.66f, 1f);

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float FogTintStrength { get; private set; } = 0.25f;

    [ExportGroup("Main Light")]
    [Export]
    public bool EnableMainLightAdjustments { get; private set; } = true;

    [Export(PropertyHint.Range, "0.1,2.5,0.01")]
    public float MainLightEnergySparseCanopy { get; private set; } = 1.0f;

    [Export(PropertyHint.Range, "0.1,2.5,0.01")]
    public float MainLightEnergyDenseCanopy { get; private set; } = 0.58f;

    [Export]
    public Color MainLightDenseCanopyTint { get; private set; } = new Color(0.92f, 0.97f, 0.88f, 1f);

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float MainLightTintStrength { get; private set; } = 0.30f;

    [ExportGroup("Sun Shafts")]
    [Export]
    public bool EnableSunShafts { get; private set; } = true;

    [Export(PropertyHint.Range, "0,24,1")]
    public int SunShaftCount { get; private set; } = 6;

    [Export(PropertyHint.Range, "1,60,0.1")]
    public float SunShaftRadius { get; private set; } = 12f;

    [Export(PropertyHint.Range, "1,40,0.1")]
    public float SunShaftHeight { get; private set; } = 12f;

    [Export(PropertyHint.Range, "1,80,0.1")]
    public float SunShaftRange { get; private set; } = 28f;

    [Export(PropertyHint.Range, "5,70,0.1")]
    public float SunShaftAngle { get; private set; } = 18f;

    [Export]
    public Color SunShaftColor { get; private set; } = new Color(1.0f, 0.96f, 0.84f, 1f);

    [Export(PropertyHint.Range, "0,6,0.01")]
    public float SunShaftEnergy { get; private set; } = 1.05f;

    [Export(PropertyHint.Range, "0,0.8,0.01")]
    public float SunShaftEnergyJitter { get; private set; } = 0.12f;

    [Export(PropertyHint.Range, "0.1,10,0.01")]
    public float SunShaftJitterSpeed { get; private set; } = 1.25f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float SunHeightThreshold { get; private set; } = 0.42f;

    [Export(PropertyHint.Range, "0.2,4,0.01")]
    public float SunShaftCanopyPower { get; private set; } = 1.1f;

    [Export]
    public bool SunShaftShadowsEnabled { get; private set; } = false;

    [ExportGroup("Diffuse Fill")]
    [Export]
    public bool EnableDiffuseFillLights { get; private set; } = true;

    [Export(PropertyHint.Range, "0,24,1")]
    public int DiffuseFillCount { get; private set; } = 4;

    [Export(PropertyHint.Range, "1,60,0.1")]
    public float DiffuseFillRadius { get; private set; } = 9f;

    [Export(PropertyHint.Range, "0.2,20,0.1")]
    public float DiffuseFillHeight { get; private set; } = 2.2f;

    [Export(PropertyHint.Range, "1,80,0.1")]
    public float DiffuseFillRange { get; private set; } = 16f;

    [Export]
    public Color DiffuseFillColor { get; private set; } = new Color(0.72f, 0.83f, 0.69f, 1f);

    [Export(PropertyHint.Range, "0,4,0.01")]
    public float DiffuseFillEnergy { get; private set; } = 0.35f;

    [Export]
    public bool DiffuseFillScalesWithCanopy { get; private set; } = true;
}
