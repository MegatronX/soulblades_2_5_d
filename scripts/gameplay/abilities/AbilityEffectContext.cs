using Godot;

public sealed class AbilityEffectContext
{
    public AbilityEffectContext(Node owner, AbilityTrigger trigger)
    {
        Owner = owner;
        Trigger = trigger;
    }

    public Node Owner { get; }
    public AbilityTrigger Trigger { get; }
    public Ability Ability { get; set; }

    public BattleContext BattleContext { get; set; }
    public ActionDirector ActionDirector { get; set; }
    public OverflowSystem OverflowSystem { get; set; }
    public ActionContext ActionContext { get; set; }
    public ActionResult ActionResult { get; set; }
    public ItemData Item { get; set; }

    // Generic numeric payload for modifiers (exp gain, costs, etc.)
    public int Amount { get; set; }
    public float Multiplier { get; set; } = 1.0f;

    public bool Cancel { get; set; }

    /// <summary>
    /// Set by an AbilityEffect when it actually applies its behavior.
    /// Used to determine whether to play triggered VFX.
    /// </summary>
    public bool WasTriggered { get; set; } = false;
}
