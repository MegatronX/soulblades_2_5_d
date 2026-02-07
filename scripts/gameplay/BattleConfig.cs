using Godot;

[GlobalClass]
public partial class BattleConfig : Resource
{
    [Export]
    public Godot.Collections.Array<Godot.Collections.Array<PackedScene>> EnemyParties { get; set; }

    [Export]
    public Godot.Collections.Array<PackedScene> AllyParty { get; set; }

    [Export]
    public BattleFormation Formation { get; set; } = BattleFormation.Normal;

    [Export]
    public BattleEnvironmentProfile EnvironmentProfile { get; set; }

    [Export]
    public BattleMusicData BattleMusic { get; set; }

    [Export]
    public BattleMusicData PostBattleMusic { get; set; }

    [Export]
    public string BattleScenePath { get; set; }

    [Export]
    public string ReturnScenePath { get; set; }

    [Export]
    public bool AllowRetry { get; set; } = true;

    [Export]
    public bool IsScriptedLoss { get; set; } = false;

    public bool HasSeed { get; set; }
    public ulong Seed { get; set; }
}
