using Godot;

/// <summary>
/// A pluggable component for DynamicAIStrategy that scores an action based on specific criteria.
/// </summary>
[GlobalClass]
public abstract partial class AIEvaluator : Resource
{
    [Export] public float Weight { get; set; } = 1.0f;
    [Export] public float BarkThreshold { get; set; } = 100.0f;
    [Export(PropertyHint.MultilineText)]
    public string BarkMessage { get; set; }

    /// <summary>
    /// Returns a score for the given action and target.
    /// Higher score means the AI is more likely to pick this action.
    /// </summary>
    public abstract float Evaluate(ActionData action, Node user, Node target, AIController controller);
}
