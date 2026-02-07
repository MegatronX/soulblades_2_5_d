// /Users/jonathanriley/soulblades_2_5_d/scripts/gameplay/BattleUnit.cs
using Godot;

[GlobalClass]
public partial class BattleUnit : Node
{
    public const string NodeName = "BattleUnit";

    [Export]
    public BattleUnitConfig Config { get; set; }

    private StatsComponent _stats;

    public override void _Ready()
    {
        // Expecting to be a child of the character root, alongside StatsComponent
        _stats = GetParent().GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
    }

    public bool IsDead => _stats != null && _stats.CurrentHP <= 0;

    // Players always persist. Others depend on config.
    public bool PersistAfterDeath => (Config?.PersistAfterDeath ?? false) || (GetParent()?.IsInGroup(GameGroups.PlayerCharacters) ?? false);
}
