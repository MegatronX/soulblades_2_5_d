using Godot;

/// <summary>
/// Base class for all Status Effect resources. A status effect is a temporary
/// condition applied to a character that can modify their stats, modify actions,
/// or trigger effects at different points in the battle flow.
/// </summary>
[GlobalClass]
public partial class StatusEffect : Resource, IActionModifier, IPersistentEffect
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

    [ExportGroup("Visual Presentation")]
    /// <summary>
    /// An animation scene (e.g., particles, sound) to play when this effect triggers a modification.
    /// </summary>
    [Export(PropertyHint.ResourceType, "PackedScene")]
    public PackedScene TriggerAnimation { get; private set; }

    /// <summary>
    /// A shader material to apply to the character's sprite while this effect is active.
    /// Used for effects like Haste's red aura.
    /// </summary>
    [Export]
    public ShaderMaterial PersistentShader { get; private set; }

    [Export]
    public float ScaleMultiplier { get; private set; } = 1.0f;

    /// <summary>
    /// If true, this effect prevents the character from acting on their turn.
    /// While active, other non-skipping status effects (like Regen) will be paused.
    /// </summary>
    public virtual bool IsTurnSkipping => false;

    /// <summary>
    /// Called when the status effect is first applied to a character.
    /// Use this to apply initial stat changes.
    /// </summary>
    public virtual void OnApply(Node owner, ActionDirector actionDirector) { /* Base implementation is empty */ }

    /// <summary>
    /// Called when the status effect is removed (either by duration expiring or by being dispelled).
    /// Use this to clean up any applied stat changes.
    /// </summary>
    public virtual void OnRemove(Node owner, ActionDirector actionDirector) { /* Base implementation is empty */ }

    /// <summary>
    /// Called at the end of the owner's turn.
    /// Use for effects like Regen or Poison.
    /// </summary>
    public virtual void OnTurnEnd(Node owner, ActionDirector actionDirector) { /* Base implementation is empty */ }

    /// <summary>
    /// Called at the start of the owner's turn.
    /// Use for effects like Paralysis checks.
    /// </summary>
    public virtual void OnTurnStart(Node owner, ActionDirector actionDirector) { /* Base implementation is empty */ }
}