using Godot;

/// <summary>
/// Defines a status effect and the chance to apply it on hit.
/// </summary>
[GlobalClass]
public partial class StatusEffectChanceEntry : Resource
{
    [Export]
    public StatusEffect Effect { get; set; }

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float ChancePercent { get; set; } = 100f;
}
