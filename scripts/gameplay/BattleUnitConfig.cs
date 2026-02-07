// /Users/jonathanriley/soulblades_2_5_d/scripts/gameplay/BattleUnitConfig.cs
using Godot;

[GlobalClass]
public partial class BattleUnitConfig : Resource
{
    [Export]
    public bool PersistAfterDeath { get; set; } = false;
}
