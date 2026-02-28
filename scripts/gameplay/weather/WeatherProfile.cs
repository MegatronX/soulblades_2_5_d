using Godot;
using System.Collections.Generic;

/// <summary>
/// Scene-level weather profile used by WeatherSystem.
/// Supports visuals/audio, scene condition modifiers, and optional battle hooks.
/// </summary>
[GlobalClass]
public partial class WeatherProfile : Resource
{
    [ExportGroup("Identity")]
    [Export]
    public string WeatherName { get; private set; } = "Weather";

    [Export]
    public WeatherType WeatherType { get; private set; } = WeatherType.None;

    [Export(PropertyHint.MultilineText)]
    public string Description { get; private set; } = string.Empty;

    [ExportGroup("Scene Conditions")]
    [Export(PropertyHint.Range, "0.1,3,0.01")]
    public float MovementSpeedMultiplier { get; private set; } = 1.0f;

    [Export(PropertyHint.Range, "0.1,3,0.01")]
    public float VisibilityMultiplier { get; private set; } = 1.0f;

    [ExportGroup("Ambient Audio")]
    [Export]
    public AudioStream AmbientLoop { get; private set; }

    [Export]
    public float AmbientVolumeDb { get; private set; } = -8f;

    [Export]
    public float AmbientPitchScale { get; private set; } = 1.0f;

    [Export]
    public bool ForceAmbientLoop { get; private set; } = true;

    [ExportGroup("Precipitation")]
    [Export]
    public WeatherPrecipitationMode PrecipitationMode { get; private set; } = WeatherPrecipitationMode.None;

    [Export(PropertyHint.Range, "0,4,0.01")]
    public float ParticleDensity { get; private set; } = 1.0f;

    [Export(PropertyHint.Range, "0,30,0.1")]
    public float CameraDepthOffset { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0.5,24,0.1")]
    public float EmissionHeight { get; private set; } = 4.5f;

    [Export(PropertyHint.Range, "0,60,0.1")]
    public float FallSpeed { get; private set; } = 18f;

    [Export(PropertyHint.Range, "0,40,0.1")]
    public float WindStrength { get; private set; } = 3f;

    [Export]
    public Vector3 WindDirection { get; private set; } = Vector3.Right;

    [Export]
    public Color ParticleTint { get; private set; } = Colors.White;

    [Export]
    public Texture2D ParticleTexture { get; private set; }

    [Export(PropertyHint.Range, "0.25,3,0.01")]
    public float ParticleAlphaMultiplier { get; private set; } = 1.0f;

    [Export(PropertyHint.Range, "0.25,3,0.01")]
    public float ParticleLifetimeMultiplier { get; private set; } = 1.0f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float ParticleWidthOverride { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float ParticleLengthOverride { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0,4,0.01")]
    public float ParticleScaleMinOverride { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0,4,0.01")]
    public float ParticleScaleMaxOverride { get; private set; } = 0f;

    [ExportGroup("Environment Lighting")]
    [Export]
    public bool EnableEnvironmentTint { get; private set; } = false;

    [Export]
    public Color EnvironmentTint { get; private set; } = Colors.White;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float EnvironmentTintStrength { get; private set; } = 0.2f;

    [Export]
    public bool EnableGlowBoost { get; private set; } = false;

    [Export(PropertyHint.Range, "0,4,0.01")]
    public float GlowIntensityMultiplier { get; private set; } = 1.0f;

    [Export]
    public bool EnableLightColorOverride { get; private set; } = false;

    [Export]
    public Color LightColorOverride { get; private set; } = Colors.White;

    [Export(PropertyHint.Range, "0.2,3,0.01")]
    public float LightEnergyMultiplier { get; private set; } = 1.0f;

    [ExportGroup("Overcast Lighting")]
    [Export]
    public bool EnableOvercastDiffuseLighting { get; private set; } = false;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float OvercastDirectionalLightEnergyMultiplier { get; private set; } = 0.45f;

    [Export(PropertyHint.Range, "0.1,4,0.01")]
    public float OvercastAmbientLightEnergyMultiplier { get; private set; } = 1.35f;

    [Export]
    public bool DisableMainLightShadowsInOvercast { get; private set; } = true;

    [ExportGroup("Lightning")]
    [Export]
    public bool EnableLightningFlashes { get; private set; } = false;

    [Export(PropertyHint.Range, "0.2,30,0.01")]
    public float LightningFlashIntervalMin { get; private set; } = 3.5f;

    [Export(PropertyHint.Range, "0.2,30,0.01")]
    public float LightningFlashIntervalMax { get; private set; } = 8f;

    [Export(PropertyHint.Range, "0.1,4,0.01")]
    public float LightningFlashDuration { get; private set; } = 0.28f;

    [Export(PropertyHint.Range, "0,50,0.1")]
    public float LightningFlashIntensity { get; private set; } = 14f;

    [Export(PropertyHint.Range, "1,300,0.5")]
    public float LightningFlashRange { get; private set; } = 110f;

    [Export(PropertyHint.Range, "-40,40,0.1")]
    public float LightningFlashVerticalOffset { get; private set; } = 16f;

    [Export(PropertyHint.Range, "-40,40,0.1")]
    public float LightningFlashForwardOffset { get; private set; } = 8f;

    [Export]
    public Color LightningFlashColor { get; private set; } = new Color(0.9f, 0.95f, 1f, 1f);

    [Export(PropertyHint.Range, "0,5,0.01")]
    public float LightningMainLightEnergyBoost { get; private set; } = 2.4f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float LightningEnvironmentFlashStrength { get; private set; } = 0.45f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float LightningScreenFlashStrength { get; private set; } = 0.5f;

    /// <summary>
    /// Legacy/single lightning sound fallback.
    /// </summary>
    [Export]
    public AudioStream LightningSfx { get; private set; }

    /// <summary>
    /// Preferred lightning variation pool. A random non-null entry is chosen per flash.
    /// Falls back to LightningSfx when empty.
    /// </summary>
    [Export]
    public Godot.Collections.Array<AudioStream> LightningSfxOptions { get; private set; } = new();

    [Export]
    public float LightningSfxVolumeDb { get; private set; } = -3f;

    [ExportGroup("Battle Hooks")]
    [Export]
    public Godot.Collections.Array<Resource> BattlefieldEffects { get; private set; } = new();

    [Export]
    public Godot.Collections.Array<Resource> TurnHazards { get; private set; } = new();

    public IEnumerable<BattlefieldEffect> EnumerateBattlefieldEffects()
    {
        if (BattlefieldEffects == null) yield break;
        foreach (var resource in BattlefieldEffects)
        {
            if (resource is BattlefieldEffect effect)
            {
                yield return effect;
            }
        }
    }

    public IEnumerable<WeatherTurnHazard> EnumerateTurnHazards()
    {
        if (TurnHazards == null) yield break;
        foreach (var resource in TurnHazards)
        {
            if (resource is WeatherTurnHazard hazard)
            {
                yield return hazard;
            }
        }
    }
}
