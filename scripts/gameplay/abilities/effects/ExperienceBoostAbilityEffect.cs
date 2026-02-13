using Godot;

[GlobalClass]
public partial class ExperienceBoostAbilityEffect : AbilityEffect
{
    [Export(PropertyHint.Range, "0.5,5.0,0.05")]
    public float Multiplier { get; private set; } = 1.2f;

    [Export]
    public int FlatBonus { get; private set; } = 0;

    public override void Apply(AbilityEffectContext context)
    {
        if (context == null) return;
        if (!Matches(context.Trigger)) return;

        bool triggered = false;

        if (FlatBonus != 0)
        {
            context.Amount = Mathf.Max(0, context.Amount + FlatBonus);
            triggered = true;
        }

        if (Multiplier > 0f && Multiplier != 1f)
        {
            context.Multiplier *= Multiplier;
            triggered = true;
        }

        if (triggered)
        {
            context.WasTriggered = true;
        }
    }
}
