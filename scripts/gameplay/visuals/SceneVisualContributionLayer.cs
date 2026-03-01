/// <summary>
/// Ordered blend layers for scene visual contributions.
/// Lower values are applied first.
/// </summary>
public enum SceneVisualContributionLayer
{
    BaseBiome = 100,
    TimeOfDay = 200,
    Weather = 300,
    ZoneOverride = 400,
    Scripted = 500,
    Debug = 600
}
