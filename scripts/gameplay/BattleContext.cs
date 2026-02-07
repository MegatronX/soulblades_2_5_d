using Godot;
using System.Collections.Generic;

public sealed class BattleContext
{
    public BattleContext(BattleConfig config)
    {
        Config = config ?? new BattleConfig();
    }

    public BattleConfig Config { get; }
    public List<Node> PlayerParty { get; } = new();
    public List<Node> EnemyCombatants { get; } = new();
    public List<Node> AllyCombatants { get; } = new();
}
