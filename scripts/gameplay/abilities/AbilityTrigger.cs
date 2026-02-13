using System;

[Flags]
public enum AbilityTrigger
{
    None = 0,
    BattleStart = 1 << 0,
    BattleEnd = 1 << 1,
    TurnStart = 1 << 2,
    TurnEnd = 1 << 3,
    ActionExecuting = 1 << 4,
    ActionExecuted = 1 << 5,
    Targeting = 1 << 6,
    ItemUse = 1 << 7,
    ItemConsume = 1 << 8,
    ExperienceGain = 1 << 9,
    ApGain = 1 << 10,
    EncounterRoll = 1 << 11,
    TimedHitResolved = 1 << 12,
    DamageCalculated = 1 << 13,
    DamageApplied = 1 << 14,
    CostCalculated = 1 << 15
}
