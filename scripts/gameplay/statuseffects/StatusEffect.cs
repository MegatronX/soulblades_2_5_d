using Godot;
using System.Collections.Generic;

/// <summary>
/// Base class for all Status Effect resources. A status effect is a temporary
/// condition applied to a character that can modify their stats, modify actions,
/// or trigger effects at different points in the battle flow.
/// </summary>
[GlobalClass]
public partial class StatusEffect : Resource, IActionModifier, IPersistentEffect, IPrioritizedModifier, ITurnPreviewStatDeltaProvider
{
    [Export]
    public string EffectName { get; private set; }

    [Export(PropertyHint.MultilineText)]
    public string Description { get; private set; }

    [Export]
    public Texture Icon { get; private set; }

    [Export]
    public int MinDurationTurns { get; private set; } = 1;

    [Export]
    public int MaxDurationTurns { get; private set; } = 1;

    [ExportGroup("Effect Interactions")]
    [Export]
    public Godot.Collections.Array<StatusEffect> ReplacementEffects { get; private set; } = new();

    [Export]
    public Godot.Collections.Array<StatusEffect> CancelEffects { get; private set; } = new();

    [ExportGroup("Effect Logic")]
    [Export]
    public Godot.Collections.Array<EffectLogic> OnApplyEffects { get; private set; } = new();

    [Export]
    public Godot.Collections.Array<EffectLogic> OnRemoveEffects { get; private set; } = new();

    [Export]
    public Godot.Collections.Array<EffectLogic> OnTurnStartEffects { get; private set; } = new();

    [Export]
    public Godot.Collections.Array<EffectLogic> OnTurnEndEffects { get; private set; } = new();

    [ExportGroup("Visual Presentation")]
    /// <summary>
    /// An animation scene (e.g., particles, sound) to play when this effect triggers a modification.
    /// </summary>
    [Export(PropertyHint.ResourceType, "PackedScene")]
    public PackedScene TriggerAnimation { get; private set; }

    [Export]
    public Vector3 TriggerAnimationOffset { get; private set; } = Vector3.Zero;

    [Export]
    public bool AttachTriggerAnimationToOwner { get; private set; } = true;

    /// <summary>
    /// A shader material to apply to the character's sprite while this effect is active.
    /// Used for effects like Haste's red aura.
    /// </summary>
    [Export]
    public ShaderMaterial PersistentShader { get; private set; }

    [ExportGroup("Visual State Overrides")]
    [Export]
    public bool ForceInjuredIdleAnimation { get; private set; } = false;
    
    [Export]
    public float ScaleMultiplier { get; private set; } = 1.0f;

    [Export]
    public bool ApplyTintWhileActive { get; private set; } = false;

    [Export]
    public Color ActiveTintColor { get; private set; } = Colors.White;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float ActiveTintStrength { get; private set; } = 0.35f;

    [Export]
    public bool EnableMirrorImages { get; private set; } = false;

    [Export(PropertyHint.Range, "1,8,1")]
    public int MirrorImageCount { get; private set; } = 3;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float MirrorImageAlpha { get; private set; } = 0.2f;

    [Export(PropertyHint.Range, "0,0.5,0.001")]
    public float MirrorImageSpread { get; private set; } = 0.06f;

    [Export(PropertyHint.Range, "0,8,0.01")]
    public float MirrorImageDriftSpeed { get; private set; } = 1.8f;

    [Export]
    public StatusEffectPolarity Polarity { get; set; } = StatusEffectPolarity.Neutral;

    [ExportGroup("Priority")]
    [Export]
    public int Priority { get; private set; } = 0;


    [ExportGroup("Turn Control")]
    [Export]
    public bool IsTurnSkipping { get; private set; } = false;

    /// <summary>
    /// Called when the status effect is first applied to a character.
    /// Use this to apply initial stat changes.
    /// </summary>
    public virtual void OnApply(Node owner, ActionDirector actionDirector)
    {
        RunEffectLogic(OnApplyEffects, owner, effect => effect.OnApply(owner));
    }

    /// <summary>
    /// Called when the status effect is removed (either by duration expiring or by being dispelled).
    /// Use this to clean up any applied stat changes.
    /// </summary>
    public virtual void OnRemove(Node owner, ActionDirector actionDirector)
    {
        RunEffectLogic(OnRemoveEffects, owner, effect => effect.OnRemove(owner));
    }

    /// <summary>
    /// Called at the end of the owner's turn.
    /// Use for effects like Regen or Poison.
    /// </summary>
    public virtual void OnTurnEnd(Node owner, ActionDirector actionDirector)
    {
        RunEffectLogic(OnTurnEndEffects, owner, effect => effect.OnTurnEnd(owner));
    }

    /// <summary>
    /// Called at the start of the owner's turn.
    /// Use for effects like Paralysis checks.
    /// </summary>
    public virtual void OnTurnStart(Node owner, ActionDirector actionDirector)
    {
        RunEffectLogic(OnTurnStartEffects, owner, effect => effect.OnTurnStart(owner));
    }

    /// <summary>
    /// Called on the initiator when they start an action.
    /// </summary>
    public virtual void OnActionInitiated(ActionContext context, Node owner)
    {
    }

    /// <summary>
    /// Called when an ally of the owner initiates an action.
    /// </summary>
    public virtual void OnAllyActionInitiated(ActionContext context, Node initiator, Node owner)
    {
    }

    /// <summary>
    /// Called during the global broadcast phase (before targeting is finalized).
    /// Use for effects like Storm Drain that can redirect actions.
    /// </summary>
    public virtual void OnActionBroadcast(ActionContext context, Node owner)
    {
    }

    /// <summary>
    /// Called during the per-target phase (after targeting is finalized).
    /// Use for effects like Reflect that can redirect or nullify actions.
    /// </summary>
    public virtual void OnActionTargeted(ActionContext context, Node owner)
    {
    }

    /// <summary>
    /// Called after action resolution against this owner.
    /// </summary>
    public virtual void OnActionPostExecution(ActionContext context, Node owner, ActionResult result)
    {
    }

    protected void RequestTriggerAnimation(Node owner)
    {
        if (TriggerAnimation == null || owner == null) return;
        if (!GodotObject.IsInstanceValid(owner)) return;

        var eventBus = owner.GetNodeOrNull<GlobalEventBus>(GlobalEventBus.Path);
        if (eventBus == null) return;

        eventBus.EmitSignal(GlobalEventBus.SignalName.EffectVfxRequested, TriggerAnimation, owner, TriggerAnimationOffset, AttachTriggerAnimationToOwner);
    }

    private static void RunEffectLogic(Godot.Collections.Array<EffectLogic> effects, Node owner, System.Action<EffectLogic> callback)
    {
        if (effects == null || effects.Count == 0 || owner == null) return;

        foreach (var effect in effects)
        {
            if (effect == null) continue;
            callback?.Invoke(effect);
        }
    }

    public virtual IEnumerable<TurnPreviewStatDelta> GetTurnPreviewStatDeltas()
    {
        if (OnApplyEffects == null || OnApplyEffects.Count == 0) yield break;

        foreach (var effect in OnApplyEffects)
        {
            if (effect is not ITurnPreviewStatDeltaProvider provider) continue;
            foreach (var delta in provider.GetTurnPreviewStatDeltas())
            {
                if (delta.IsNoOp) continue;
                yield return delta;
            }
        }
    }

    public bool TryGetActiveTint(out Color tintColor, out float strength)
    {
        tintColor = ActiveTintColor;
        strength = Mathf.Clamp(ActiveTintStrength, 0f, 1f);
        return ApplyTintWhileActive && strength > 0f;
    }

    public bool TryGetMirrorImageConfig(out int count, out float alpha, out float spread, out float driftSpeed)
    {
        count = Mathf.Clamp(MirrorImageCount, 1, 8);
        alpha = Mathf.Clamp(MirrorImageAlpha, 0f, 1f);
        spread = Mathf.Max(0f, MirrorImageSpread);
        driftSpeed = Mathf.Max(0f, MirrorImageDriftSpeed);
        return EnableMirrorImages && count > 0 && alpha > 0f;
    }
}
