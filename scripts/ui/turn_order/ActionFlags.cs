using System;

/// <summary>
/// A bit-mask enum for efficiently flagging actions with various properties.
/// </summary>
[Flags]
public enum ActionFlags
{
    None = 0,
    IsItem = 1 << 0,
    CannotBeDodged = 1 << 1, // Bypasses evasion checks.
    FixedDamage = 1 << 2,    // Damage is not affected by stats, only by resistances.
    Piercing = 1 << 3,       // Damage ignores target's defense stats.
    AlwaysHits = 1 << 4,     // Bypasses accuracy checks.
    CannotBeRedirected = 1 << 5, // Ignores redirect/reflect effects.
}
