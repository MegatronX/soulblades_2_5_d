using Godot;

/// <summary>
/// A data-driven resource for an ability that reflects magical actions back to the initiator.
/// </summary>
[GlobalClass]
public partial class ReflectAbilityData : AbilityData
{
    /// <summary>
    /// The message to add to the log when this ability triggers.
    /// The animation system will look for this tag.
    /// </summary>
    [Export]
    public string AnimationTriggerTag { get; private set; } = "Reflected";

    public void OnActionTargeted(ActionContext context, Node owner)
    {
        // 1. Check Trigger Conditions
        
        // First, is the owner the current target of the action?
        if (context.CurrentTarget != owner)
        {
            return;
        }

        // Second, is the action magical? We consider any action that is not 100% physical
        // to be magical for the purpose of reflection.
        if (context.SourceAction.PhysicalRatio >= 1.0f)
        {
            return;
        }

        // 2. Modify the ActionContext
        // All conditions met. Change the target to the original initiator.
        context.CurrentTarget = context.Initiator;

        // 3. Log the Modification for the Animation System
        // This is the crucial step for triggering the animation.
        context.ModificationLog.Add(AnimationTriggerTag);
    }
}