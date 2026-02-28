using Godot;

/// <summary>
/// Applies canopy-driven main directional light modifications.
/// </summary>
public sealed class MainLightAtmosphereLayer : IAtmosphereLayer
{
    public void Apply(SceneAtmosphereRuntimeContext context, SceneAtmosphereProfile profile)
    {
        if (context == null || profile == null) return;
        if (context.MainLight == null) return;

        context.CaptureMainLightBaseline();

        if (!profile.EnableMainLightAdjustments)
        {
            context.MainLight.LightColor = context.BaseMainLightColor;
            context.MainLight.LightEnergy = context.BaseMainLightEnergy;
            return;
        }

        float canopy = context.GetResolvedCanopy(profile);
        float sunHeight = context.ComputeSunHeightFactor();

        float energyMul = Mathf.Lerp(
            profile.MainLightEnergySparseCanopy,
            profile.MainLightEnergyDenseCanopy,
            canopy);
        energyMul *= Mathf.Lerp(0.85f, 1.05f, sunHeight);
        energyMul = Mathf.Max(0.05f, energyMul);

        float tintT = Mathf.Clamp(profile.MainLightTintStrength * canopy, 0f, 1f);
        context.MainLight.LightColor = context.BaseMainLightColor.Lerp(profile.MainLightDenseCanopyTint, tintT);
        context.MainLight.LightEnergy = context.BaseMainLightEnergy * energyMul;
    }

    public void Update(SceneAtmosphereRuntimeContext context, SceneAtmosphereProfile profile, float delta)
    {
        // Static layer; no per-frame logic.
    }

    public void Clear(SceneAtmosphereRuntimeContext context)
    {
        context?.RestoreMainLightBaseline();
    }
}
