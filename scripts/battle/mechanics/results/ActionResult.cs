using Godot;
using System.Collections.Generic;

/// <summary>
/// Represents the calculated outcome of an action on a specific target.
/// This separates "what will happen" from "what is happening".
/// </summary>
[GlobalClass]
public partial class ActionResult : RefCounted
{
    public bool IsHit { get; set; } = true;
    public bool IsCritical { get; set; } = false;
    public bool IsBlocked { get; set; } = false; // Timed block success
    public bool IsTimedHit { get; set; } = false; // Timed attack success
    
    public int RawDamage { get; set; }
    public int FinalDamage { get; set; }
    public int HealingAmount { get; set; }
    
    public List<StatusEffectData> AddedStatusEffects { get; } = new();
    public List<string> AnimationTags { get; } = new(); // e.g. "Weakness", "Resist"

    // Snapshot of stats at the time of calculation, useful for UI/Animation
    public Dictionary<ElementType, float> DamageElements { get; set; } = new();

    /// <summary>
    /// The magnitude of the damage or healing (absolute value).
    /// </summary>
    public int TotalDamage => Mathf.Abs(FinalDamage);

    /// <summary>
    /// Returns true if the action resulted in healing (negative damage).
    /// </summary>
    public bool IsHeal => FinalDamage < 0;
}