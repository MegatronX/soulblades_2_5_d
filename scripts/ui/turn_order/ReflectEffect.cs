using Godot;

/// <summary>
/// A status effect that reflects magical actions back to the initiator.
/// </summary>
[GlobalClass]
public partial class ReflectEffect : StatusEffect
{
    public override void OnActionTargeted(ActionContext context, Node owner)
    {
        // Is the owner the current target?
        if (context.CurrentTarget != owner) return;

        // Is the action more than 50% magical? (i.e., PhysicalRatio is less than 0.5)
        // An action with a 0.5 ratio is an even split, so we check for < 0.5.
        if (context.SourceAction.PhysicalRatio >= 0.5f) return;

        // All conditions met. Change the target to the original initiator.
        context.CurrentTarget = context.Initiator;

        // Log the modification for the animation system.
        context.ModificationLog.Add("Reflected");
    }
}
