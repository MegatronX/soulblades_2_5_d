using System;

[Flags]
public enum TargetingType
{
    None = 0,
    Self = 1 << 0,
    AnyAlly = 1 << 1,
    AnyEnemy = 1 << 2,
    AnySingleTarget = 1 << 3, // Implies AnyAlly | AnyEnemy
    
    OwnParty = 1 << 4,
    AnyAllyParty = 1 << 5,
    AnyEnemyParty = 1 << 6,
    AnySingleParty = 1 << 7, // Implies AnyAllyParty | AnyEnemyParty
    
    All = 1 << 8
}