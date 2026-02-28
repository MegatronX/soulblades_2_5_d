using Godot;

/// <summary>
/// Requires a map runtime flag to match a target value.
/// </summary>
[GlobalClass]
public partial class MapFlagCondition : InteractionCondition
{
    [Export]
    public string FlagKey { get; private set; } = string.Empty;

    [Export]
    public bool RequiredValue { get; private set; } = true;

    public override bool IsSatisfied(ExplorationInteractionContext context, out string reason)
    {
        if (string.IsNullOrWhiteSpace(FlagKey))
        {
            reason = string.Empty;
            return true;
        }

        bool value = context?.MapController?.GetMapFlag(FlagKey) ?? false;
        bool ok = value == RequiredValue;
        reason = ok ? string.Empty : $"Requires flag {FlagKey} = {RequiredValue}";
        return ok;
    }
}
