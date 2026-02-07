using Godot;

/// <summary>
/// A data-driven resource for abilities that trigger an effect when the owner is hit
/// by a specific type of attack. This replaces the need for unique scripts like StormSurgeAbility.
/// </summary>
[GlobalClass]
public partial class ReactiveAbilityData : AbilityData
{
    [Export]
    public ElementType TriggerElement { get; private set; }

    [Export(PropertyHint.ResourceType, "StatusEffectData")]
    public StatusEffectData EffectToApply { get; private set; }

    public void OnActionTargeted(ActionContext context, Node owner)
    {
        // 1. Check Trigger Conditions
        
        // Is the owner the final target of the action?
        if (context.CurrentTarget != owner)
        {
            return;
        }

        // Does the incoming action have a damage component with the correct element?
        var damageComponent = context.GetComponent<DamageComponent>();
        if (damageComponent == null || !damageComponent.ElementalWeights.TryGetValue(TriggerElement, out float weight) || weight <= 0)
        {
            return;
        }

        // 2. Apply the Effect
        if (EffectToApply != null)
        {
            // All conditions met. Apply the specified status effect to the owner.
            // In a real game, this would call a StatusEffectManager.
            // owner.GetNode<StatusEffectManager>().ApplyEffect(EffectToApply);
            GD.Print($"{owner.Name}'s reactive ability '{this.ResourceName}' triggered! Applying '{EffectToApply.ResourceName}'.");
        }
    }
}