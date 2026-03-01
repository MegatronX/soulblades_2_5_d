using Godot;

/// <summary>
/// Runtime visual contribution payload applied by <see cref="SceneVisualDirector"/>.
/// Uses composable operations (tints, multipliers, optional overrides).
/// </summary>
public sealed class SceneVisualIntent
{
    public int Layer { get; set; } = (int)SceneVisualContributionLayer.BaseBiome;

    public bool OverrideMainLightColor { get; set; }
    public Color MainLightColorOverride { get; set; } = Colors.White;

    public bool UseMainLightTint { get; set; }
    public Color MainLightTint { get; set; } = Colors.White;
    public float MainLightTintStrength { get; set; }

    public float MainLightEnergyMultiplier { get; set; } = 1f;
    public bool? MainLightShadowEnabledOverride { get; set; }

    public bool UseAmbientTint { get; set; }
    public Color AmbientTint { get; set; } = Colors.White;
    public float AmbientTintStrength { get; set; }

    public bool UseFogTint { get; set; }
    public Color FogTint { get; set; } = Colors.White;
    public float FogTintStrength { get; set; }

    public float AmbientColorMultiplier { get; set; } = 1f;
    public float FogColorMultiplier { get; set; } = 1f;
    public float AmbientEnergyMultiplier { get; set; } = 1f;
    public float GlowIntensityMultiplier { get; set; } = 1f;
    public float FogDensityMultiplier { get; set; } = 1f;
    public float FogDensityAdd { get; set; } = 0f;
    public bool? FogEnabledOverride { get; set; }
    public float AmbientSkyContributionMultiplier { get; set; } = 1f;

    public bool? AdjustmentEnabledOverride { get; set; }
    public float AdjustmentBrightnessMultiplier { get; set; } = 1f;
    public float AdjustmentSaturationMultiplier { get; set; } = 1f;
}
