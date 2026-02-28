using Godot;

/// <summary>
/// Pluggable scene-atmosphere layer contract.
/// </summary>
public interface IAtmosphereLayer
{
    void Apply(SceneAtmosphereRuntimeContext context, SceneAtmosphereProfile profile);
    void Update(SceneAtmosphereRuntimeContext context, SceneAtmosphereProfile profile, float delta);
    void Clear(SceneAtmosphereRuntimeContext context);
}
