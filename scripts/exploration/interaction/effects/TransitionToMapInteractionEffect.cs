using Godot;

/// <summary>
/// Moves the player to another map scene, optionally selecting a spawn id in the destination scene.
/// </summary>
[GlobalClass]
public partial class TransitionToMapInteractionEffect : InteractionEffect
{
    [Export(PropertyHint.File, "*.tscn")]
    public string TargetScenePath { get; private set; } = string.Empty;

    [Export]
    public string TargetSpawnId { get; private set; } = "default";

    public override void Execute(ExplorationInteractionContext context, Node source)
    {
        if (string.IsNullOrWhiteSpace(TargetScenePath))
        {
            GD.PrintErr("[TransitionToMapInteractionEffect] Missing TargetScenePath.");
            return;
        }

        ExplorationTransitionState.SetPendingSpawnId(TargetSpawnId);
        source?.GetTree()?.ChangeSceneToFile(TargetScenePath);
    }
}
