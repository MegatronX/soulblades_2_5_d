using Godot;

/// <summary>
/// Sets a map runtime flag value.
/// </summary>
[GlobalClass]
public partial class SetMapFlagInteractionEffect : InteractionEffect
{
    [Export]
    public string FlagKey { get; private set; } = string.Empty;

    [Export]
    public bool Value { get; private set; } = true;

    public override void Execute(ExplorationInteractionContext context, Node source)
    {
        if (string.IsNullOrWhiteSpace(FlagKey)) return;
        context?.MapController?.SetMapFlag(FlagKey, Value);
    }
}
