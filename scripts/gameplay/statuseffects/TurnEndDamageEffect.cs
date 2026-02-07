using Godot;

/// <summary>
/// A data-driven status effect that deals damage or heals based on a percentage
/// of Max HP at the end of the owner's turn. Used for Regen and Poison.
/// </summary>
[GlobalClass]
public partial class TurnEndDamageEffect : StatusEffect
{
    [Export(PropertyHint.Range, "-100,100,0.5")]
    public float MaxHpPercent { get; private set; } = -7.5f; // Negative for damage, positive for healing.

    public override void OnTurnEnd(Node owner, ActionDirector actionDirector)
    {
        // In a real implementation, you would get the character's HealthComponent
        // and StatsComponent to apply the damage/healing.
        // var stats = owner.GetNode<StatsComponent>("StatsComponent");
        // var health = owner.GetNode<HealthComponent>("HealthComponent");
        // var amount = stats.GetStatValue(StatType.MaxHP) * (MaxHpPercent / 100.0f);
        // health.ApplyChange(amount);

        GD.Print($"{owner.Name} is affected by {EffectName} for {MaxHpPercent}% Max HP.");
    }
}