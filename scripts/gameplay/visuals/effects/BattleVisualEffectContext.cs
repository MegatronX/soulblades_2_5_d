using Godot;

public sealed class BattleVisualEffectContext
{
    public BattleVisualEventType EventType { get; init; } = BattleVisualEventType.None;
    public Node Owner { get; init; }
    public Node RelatedNode { get; init; }

    public StatusEffect StatusEffect { get; init; }
    public StatusEffectManager StatusManager { get; init; }
    public StatusEffectManager.StatusEffectInstance StatusInstance { get; init; }

    public Ability Ability { get; init; }
    public AbilityEffect AbilityEffect { get; init; }
    public AbilityEffectContext AbilityContext { get; init; }

    public ActionDirector ActionDirector { get; init; }
    public ActionContext ActionContext { get; init; }
    public ActionResult ActionResult { get; init; }

    public CharacterVisualStateController VisualController { get; init; }
    public double DeltaSeconds { get; init; } = 0d;

    public int SourcePriority { get; init; } = 0;
    public int EffectPriority { get; init; } = 0;
    public int SourceOrder { get; init; } = 0;

    public int EffectivePriority => (SourcePriority * 1024) + EffectPriority;

    public GlobalEventBus GetEventBus()
    {
        if (Owner == null) return null;
        return Owner.GetNodeOrNull<GlobalEventBus>(GlobalEventBus.Path);
    }

    public void EmitOneShotVfx(PackedScene vfxScene, Vector3 offset, bool parentToOwner = true, Node anchorOverride = null)
    {
        if (vfxScene == null) return;

        Node anchor = anchorOverride;
        if (anchor == null || !GodotObject.IsInstanceValid(anchor))
        {
            anchor = Owner;
        }

        if (anchor == null || !GodotObject.IsInstanceValid(anchor)) return;

        var eventBus = GetEventBus();
        if (eventBus == null) return;

        eventBus.EmitSignal(GlobalEventBus.SignalName.EffectVfxRequested, vfxScene, anchor, offset, parentToOwner);
    }
}
