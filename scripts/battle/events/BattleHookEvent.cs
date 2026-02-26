using Godot;

/// <summary>
/// Generic runtime hook payload emitted by battle systems.
/// Consumers (presentation, telemetry, etc.) subscribe without coupling gameplay systems.
/// </summary>
public sealed class BattleHookEvent
{
    public BattleHookEventType EventType { get; init; } = BattleHookEventType.None;

    public Node Owner { get; init; }
    public Node RelatedNode { get; init; }

    public ActionDirector ActionDirector { get; init; }
    public ActionContext ActionContext { get; init; }
    public ActionResult ActionResult { get; init; }

    public object Modifier { get; init; }

    public StatusEffect StatusEffect { get; init; }
    public StatusEffectManager StatusManager { get; init; }
    public StatusEffectManager.StatusEffectInstance StatusInstance { get; init; }

    public Ability Ability { get; init; }
    public AbilityEffect AbilityEffect { get; init; }
    public AbilityEffectContext AbilityContext { get; init; }

    public BattlefieldEffect BattlefieldEffect { get; init; }
    public BattlefieldEffectManager BattlefieldEffectManager { get; init; }
}
