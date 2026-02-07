using Godot;

/// <summary>
/// A trigger condition that checks if an action's damage component matches a specific element.
/// </summary>
[GlobalClass]
public partial class ElementalCondition : TriggerCondition
{
    [Export]
    public ElementType ElementToMatch { get; private set; }

    public override bool IsMet(ActionContext context)
    {
        var damageComponent = context.GetComponent<DamageComponent>();
        if (damageComponent == null)
        {
            // If the action has no damage component, it can't match an element.
            return false;
        }

        // Check if the damage component contains the matching element with a weight > 0.
        return damageComponent.ElementalWeights.TryGetValue(ElementToMatch, out float weight) && weight > 0;
    }
}