using Godot;

/// <summary>
/// A status effect that reduces all incoming damage to 0.
/// </summary>
[GlobalClass]
public partial class InvincibleEffect : StatusEffect
{
    public void OnActionTargeted(ActionContext context, Node owner)
    {
        // Is the owner the final target?
        if (context.CurrentTarget != owner) return;

        // Does the action have a damage component?
        var damageComponent = context.GetComponent<DamageComponent>();
        if (damageComponent == null) return;

        // If the damage is already zero or less, do nothing.
        if (damageComponent.Power <= 0) return;

        // Reduce the damage to 0.
        damageComponent.Power = 0;

        // Log the event for the animator to show an "Invincible" or "0" popup.
        context.ModificationLog.Add("Invincible");
    }
}