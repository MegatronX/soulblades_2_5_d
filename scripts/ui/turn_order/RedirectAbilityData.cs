using Godot;
using System.Linq;

/// <summary>
/// A data-driven resource for abilities that redirect attacks of a specific element
/// to the owner. This replaces the need for unique scripts like StormDrainAbility.
/// </summary>
[GlobalClass]
public partial class RedirectAbilityData : AbilityData
{
    /// <summary>
    /// A list of conditions that must ALL be met for the redirection to trigger.
    /// </summary>
    [Export]
    public Godot.Collections.Array<TriggerCondition> Conditions { get; private set; } = new();

    public void OnActionBroadcast(ActionContext context, Node owner)
    {
        // If there are no conditions, the ability can't trigger.
        if (Conditions.Count == 0)
        {
            return;
        }

        // Check if all conditions are met by the incoming action.
        // The `All` method returns true if the collection is empty, so we check count first.
        bool allConditionsMet = Conditions.All(c => c.IsMet(context));

        if (allConditionsMet)
        {
            // Don't redirect if the owner is already the sole target.
            if (context.InitialTargets.Count == 1 && context.InitialTargets[0] == owner)
            {
                return;
            }

            // All conditions passed! Hijack the action.
            context.InitialTargets.Clear();
            context.InitialTargets.Add(owner);
            context.ModificationLog.Add($"Drawn in by {owner.Name}'s {this.ResourceName}!");
        }
    }
}