using Godot;
using System.Collections.Generic;

/// <summary>
/// Test harness for BattleResultsOverlay to tune layout without running a real battle.
/// Assign a BattleResultsOverlay scene in the inspector.
/// </summary>
public partial class BattleResultsOverlayTest : Node
{
    [Export] private PackedScene _overlayScene;

    [ExportGroup("Party")]
    [Export] private int _partyCount = 3;
    [Export] private Godot.Collections.Array<string> _partyNames = new();
    [Export] private Godot.Collections.Array<Texture2D> _partyPortraits = new();
    [Export] private Godot.Collections.Array<PackedScene> _partyCharacterScenes = new();

    [ExportGroup("Rewards")]
    [Export] private int _expReward = 140;
    [Export] private int _apReward = 25;
    [Export] private int _moneyReward = 200;

    [ExportGroup("Mode")]
    [Export] private bool _showDefeat = false;
    [Export] private bool _allowRetry = true;

    private readonly List<Node> _partyMembers = new();

    public override async void _Ready()
    {
        if (_overlayScene == null)
        {
            GD.PrintErr("BattleResultsOverlayTest: Overlay scene not assigned.");
            return;
        }

        var overlay = _overlayScene.Instantiate<BattleResultsOverlay>();
        AddChild(overlay);

        BuildParty();

        // Allow deferred initialization on LevelingComponent and StatsComponent.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (_showDefeat)
        {
            overlay.ShowDefeat(_allowRetry);
            return;
        }

        var rewards = CreateRewards();
        overlay.ShowVictory(rewards, ApplyRewardsToParty, _partyMembers);
    }

    private void BuildParty()
    {
        _partyMembers.Clear();

        for (int i = 0; i < _partyCount; i++)
        {
            Node member = TryCreateFromScene(i);
            if (member == null)
            {
                string name = i < _partyNames.Count && !string.IsNullOrEmpty(_partyNames[i])
                    ? _partyNames[i]
                    : $"Hero {i + 1}";
                Texture2D portrait = i < _partyPortraits.Count ? _partyPortraits[i] : null;
                member = CreateMockCharacter(name, portrait);
            }
            AddChild(member);
            _partyMembers.Add(member);
        }
    }

    private Node TryCreateFromScene(int index)
    {
        if (index >= _partyCharacterScenes.Count) return null;
        var scene = _partyCharacterScenes[index];
        if (scene == null) return null;

        var instance = scene.Instantiate();
        if (instance is Node node)
        {
            return node;
        }

        GD.PrintErr("BattleResultsOverlayTest: Party character scene did not instantiate a Node.");
        instance.QueueFree();
        return null;
    }

    private Node CreateMockCharacter(string name, Texture2D portrait)
    {
        var character = new BaseCharacter { Name = name };

        if (portrait != null)
        {
            var presentation = new CharacterPresentationData();
            presentation.Set("DisplayName", name);
            presentation.Set("PortraitImage", portrait);
            character.Set("PresentationData", presentation);
        }

        var stats = new StatsComponent { Name = StatsComponent.NodeName };
        stats.SetBaseStatsResource(CreateBaseStats());
        character.AddChild(stats);

        var actionManager = new ActionManager { Name = ActionManager.DefaultName };
        character.AddChild(actionManager);

        var leveling = new LevelingComponent
        {
            Name = LevelingComponent.NodeName,
            Progression = CreateProgression(),
            StartingLevel = 1,
            StartingExperience = 0
        };
        leveling.StatGrowthRules.Add(CreateStrengthGrowth());
        leveling.LevelStatIncrements.Add(CreateHpIncrementEntry());
        leveling.LevelRewards.Add(CreateActionRewardEntry());
        character.AddChild(leveling);

        return character;
    }

    private static BaseStats CreateBaseStats()
    {
        return new BaseStats
        {
            HP = 120,
            MP = 40,
            Strength = 10,
            Defense = 8,
            Magic = 6,
            MagicDefense = 5,
            Speed = 9,
            Evasion = 3,
            MgEvasion = 3,
            Accuracy = 8,
            MgAccuracy = 6,
            Luck = 5,
            AP = 20
        };
    }

    private static LevelProgression CreateProgression()
    {
        return new LevelProgression
        {
            MaxLevel = 20,
            ExpMode = ExpRequirementMode.TableTotal,
            ExpTable = new Godot.Collections.Array<int> { 0, 20, 50, 90, 140, 200, 270, 350, 440, 540, 650 }
        };
    }

    private static StatGrowthRule CreateStrengthGrowth()
    {
        return new StatGrowthRule
        {
            Stat = StatType.Strength,
            Mode = StatGrowthMode.AddPerLevel,
            AddPerLevel = 1.0f
        };
    }

    private static LevelStatIncrementEntry CreateHpIncrementEntry()
    {
        var entry = new LevelStatIncrementEntry { Level = 3 };
        entry.Increments.Add(new StatIncrement { Stat = StatType.HP, Amount = 15 });
        return entry;
    }

    private static LevelRewardEntry CreateActionRewardEntry()
    {
        var entry = new LevelRewardEntry { Level = 2 };
        var action = new ActionData { CommandName = "Fire" };
        entry.Actions.Add(action);
        return entry;
    }

    private BattleRewards CreateRewards()
    {
        var rewards = new BattleRewards
        {
            TotalExperience = _expReward,
            TotalApExperience = _apReward,
            TotalMoney = _moneyReward
        };

        var potion = new ItemData { ItemName = "Potion" };
        var ether = new ItemData { ItemName = "Ether" };
        rewards.AddItem(potion, 2);
        rewards.AddItem(ether, 1);

        return rewards;
    }

    private void ApplyRewardsToParty()
    {
        foreach (var member in _partyMembers)
        {
            if (member == null) continue;
            var leveling = member.GetNodeOrNull<LevelingComponent>(LevelingComponent.NodeName);
            leveling?.AddExperience(_expReward);

            var abilityManager = member.GetNodeOrNull<AbilityManager>(AbilityManager.NodeName);
            abilityManager?.AddApExperience(_apReward);
        }
    }
}
