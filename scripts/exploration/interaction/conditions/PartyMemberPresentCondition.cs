using Godot;

/// <summary>
/// Requires a party member to be present by display name, node name, or scene file stem.
/// </summary>
[GlobalClass]
public partial class PartyMemberPresentCondition : InteractionCondition
{
    [Export]
    public string RequiredMemberName { get; private set; } = string.Empty;

    [Export]
    public bool AllowPartialMatch { get; private set; } = true;

    [Export]
    public bool InvertResult { get; private set; } = false;

    public override bool IsSatisfied(ExplorationInteractionContext context, out string reason)
    {
        if (string.IsNullOrWhiteSpace(RequiredMemberName))
        {
            reason = string.Empty;
            return true;
        }

        bool present = context?.MapController?.IsPartyMemberPresent(RequiredMemberName, AllowPartialMatch) ?? false;
        if (InvertResult)
        {
            present = !present;
        }

        reason = present ? string.Empty : $"Requires party member: {RequiredMemberName}";
        return present;
    }
}
