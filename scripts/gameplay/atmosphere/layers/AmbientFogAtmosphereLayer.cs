using Godot;

/// <summary>
/// Applies canopy-driven ambient/fog presentation to WorldEnvironment.
/// </summary>
public sealed class AmbientFogAtmosphereLayer : IAtmosphereLayer
{
    public void Apply(SceneAtmosphereRuntimeContext context, SceneAtmosphereProfile profile)
    {
        if (context == null || profile == null) return;

        var env = context.WorldEnvironment?.Environment;
        if (env == null) return;

        context.CaptureEnvironmentBaseline();
        float canopy = context.GetResolvedCanopy(profile);

        if (profile.EnableAmbientTint)
        {
            float t = Mathf.Clamp(profile.AmbientTintStrength * canopy, 0f, 1f);
            env.AmbientLightColor = context.BaseAmbientColor.Lerp(profile.AmbientTint, t);
        }
        else
        {
            env.AmbientLightColor = context.BaseAmbientColor;
        }

        float fogDensity = Mathf.Max(0f, context.BaseFogDensity + (profile.FogDensityBoost * canopy));
        float fogT = Mathf.Clamp(profile.FogTintStrength * canopy, 0f, 1f);
        env.FogLightColor = context.BaseFogColor.Lerp(profile.FogTint, fogT);
        env.FogDensity = fogDensity;
        env.FogEnabled = context.BaseFogEnabled || fogDensity > context.BaseFogDensity + 0.00001f || fogT > 0.01f;
    }

    public void Update(SceneAtmosphereRuntimeContext context, SceneAtmosphereProfile profile, float delta)
    {
        // Static layer; no per-frame logic.
    }

    public void Clear(SceneAtmosphereRuntimeContext context)
    {
        context?.RestoreEnvironmentBaseline();
    }
}
