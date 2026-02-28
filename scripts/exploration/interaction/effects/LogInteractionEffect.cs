using Godot;

/// <summary>
/// Debug/log effect for interaction prototyping.
/// </summary>
[GlobalClass]
public partial class LogInteractionEffect : InteractionEffect
{
    [Export(PropertyHint.MultilineText)]
    public string Message { get; private set; } = "Interaction triggered.";

    public override void Execute(ExplorationInteractionContext context, Node source)
    {
        GD.Print($"[Interaction] {Message}");
    }
}
