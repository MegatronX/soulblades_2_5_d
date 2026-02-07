using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A test harness for visualizing and debugging the TurnOrderPreviewUI animations.
/// This script creates a mock battle environment and provides UI hooks to trigger
/// various turn order changes.
/// </summary>
public partial class TurnOrderPreviewUITest : Node
{
    [Export]
    private TurnOrderPreviewUI _turnOrderPreviewUI;

    [ExportGroup("Buttons")]
    [Export] private Button _commitTurnButton;
    [Export] private Button _previewSlowActionButton;
    [Export] private Button _previewHasteButton;
    [Export] private Button _cancelPreviewButton;
    [Export] private Button _summonUnitButton;
    [Export] private Button _defeatUnitButton;

    [ExportGroup("Summoning")]
    [Export] private PackedScene[] _summonableCharacters;

    private TurnManager _turnManager;
    private GlobalEventBus _eventBus;
    private ActionDirector _actionDirector; // Add a reference to the ActionDirector
    private List<Node> _mockCombatants = new();
    private readonly RandomNumberGenerator _rng = new();
    private int _summonCounter = 1;

    public override void _Ready()
    {
        _turnManager = new TurnManager();
        _eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        _actionDirector = new ActionDirector(); // Instantiate the ActionDirector

        // Connect the button signals programmatically.
        _commitTurnButton.Pressed += OnCommitTurnPressed;
        _previewSlowActionButton.Pressed += OnPreviewSlowActionPressed;
        _previewHasteButton.Pressed += OnPreviewHasteTargetPressed;
        _cancelPreviewButton.Pressed += OnCancelPreviewPressed;
        _summonUnitButton.Pressed += OnSummonUnitPressed;
        _defeatUnitButton.Pressed += OnUnitDefeatedPressed;

        // Create mock combatants with different speeds
        _mockCombatants.Add(CreateMockCombatant("Hero", 12));
        _mockCombatants.Add(CreateMockCombatant("Mage", 9));
        _mockCombatants.Add(CreateMockCombatant("Rogue", 15));
        _mockCombatants.Add(CreateMockCombatant("Goblin", 8));

        // Add them to the turn manager
        foreach (var combatant in _mockCombatants)
        {
            _turnManager.AddCombatant(combatant);
        }

        // Initialize the UI
        _turnOrderPreviewUI.Initialize(_turnManager);
    }

    private Node CreateMockCombatant(string name, int speed)
    {
        var node = new BaseCharacter { Name = name };
        
        // Create a temporary BaseStats resource in memory for this mock character.
        var mockBaseStats = new BaseStats { Speed = speed };

        // Create the StatsComponent, assign the resource, and manually initialize it.
        var stats = new StatsComponent();
        node.AddChild(stats);
        stats.SetBaseStatsResource(mockBaseStats); // Assign the resource
        stats.Name = StatsComponent.NodeName; // Ensure GetNode can find it
        return node;
    }

    public void OnCommitTurnPressed()
    {
        var turn = _turnManager.GetNextTurn();
        if (turn == null) return;

        // Pass the ActionDirector context when committing a turn.
        // This allows the TurnManager to correctly trigger OnTurnStart/OnTurnEnd.
        _turnManager.CommitTurn(turn, 0, _actionDirector);

        _eventBus.EmitSignal(GlobalEventBus.SignalName.TurnCommitted);
    }

    public void OnPreviewSlowActionPressed()
    {
        var turn = _turnManager.GetNextTurn();
        if (turn == null) return;

        // For this test, let's say a "slow action" also applies a slow debuff to the actor.
        var target = turn.Combatant;
        var targetStats = target.GetNode<StatsComponent>(StatsComponent.NodeName);
        var currentSpeed = targetStats.GetStatValue(StatType.Speed);
        var speedModifier = -5; // Negative modifier for slow
        var newSpeed = currentSpeed + speedModifier;
        GD.Print($"Previewing Slow Action: Targeting self ('{target.Name}'). Current Speed: {currentSpeed}, New Speed: {newSpeed}");

        var preview = new TurnManager.ActionPreview
        {
            ActionTickCost = 500f, // A slow, heavy attack
            StatusEffectSim = new() { { target, new() { { StatType.Speed, new(speedModifier, 1.0f) } } } }
        };
        _eventBus.EmitSignal(GlobalEventBus.SignalName.ActionPreviewRequested, preview, turn.Combatant);
    }

    public void OnPreviewHasteTargetPressed()
    {
        var turn = _turnManager.GetNextTurn();
        var order = _turnManager.GenerateTurnOrder(5);
        if (turn == null || order.Count < 2) return;

        // Target the 3rd character in the turn order for a haste effect
        var target = order.ElementAtOrDefault(2)?.Combatant;
        if (target == null) return;

        // Get current speed for debug printing
        var targetStats = target.GetNode<StatsComponent>(StatsComponent.NodeName);
        var currentSpeed = targetStats.GetStatValue(StatType.Speed);
        var speedModifier = 20;
        var newSpeed = currentSpeed + speedModifier;
        GD.Print($"Previewing Haste: Targeting '{target.Name}'. Current Speed: {currentSpeed}, New Speed: {newSpeed}");

        var preview = new TurnManager.ActionPreview
        {
            ActionTickCost = 100f, // The haste spell itself takes some time
            StatusEffectSim = new Dictionary<Node, Dictionary<StatType, System.Tuple<int, float>>>
            {
                { target, new() { { StatType.Speed, new(speedModifier, 1.0f) } } } // Simulate +20 speed
            }
        };
        _eventBus.EmitSignal(GlobalEventBus.SignalName.ActionPreviewRequested, preview, turn.Combatant);
    }

    public void OnCancelPreviewPressed()
    {
        _eventBus.EmitSignal(GlobalEventBus.SignalName.ActionPreviewCancelled);
    }

    public void OnSummonUnitPressed()
    {
        if (_summonableCharacters == null || _summonableCharacters.Length == 0)
        {
            GD.Print("No summonable character scenes configured for the test. Using mock summon.");
            var mockUnit = CreateMockCombatant($"Summon {_summonCounter++}", 10);
            _mockCombatants.Add(mockUnit);
            _turnManager.AddCombatant(mockUnit, TurnManager.TickThreshold);
            // The UI is event-driven. Emit a signal to tell it to update.
            _eventBus.EmitSignal(GlobalEventBus.SignalName.CombatantsChanged);
            return;
        }

        // Pick a random scene from the exported array and instantiate it.
        var sceneToSummon = _summonableCharacters[_rng.RandiRange(0, _summonableCharacters.Length - 1)];
        var newUnit = sceneToSummon.Instantiate();
        AddChild(newUnit); // Add to the scene tree

        _mockCombatants.Add(newUnit);
        _turnManager.AddCombatant(newUnit, TurnManager.TickThreshold); // Summoned unit starts ready
        // The UI is event-driven. Emit a signal to tell it to update.
        _eventBus.EmitSignal(GlobalEventBus.SignalName.CombatantsChanged);
    }

    public void OnUnitDefeatedPressed()
    {
        var order = _turnManager.GenerateTurnOrder(5);
        if (order.Count < 2) return;

        // Defeat the second unit in the turn order
        var targetTurnData = order[1];
        
        // This is a simplified removal. In a real game, you'd have a proper
        // RemoveCombatant method on TurnManager.
        var combatantToRemove = _turnManager.GetCombatants().FirstOrDefault(c => c.Combatant == targetTurnData.Combatant);
        if (combatantToRemove != null)
        {
            _turnManager.RemoveCombatant(combatantToRemove);
            // The UI is event-driven. Emit a signal to tell it to update.
            _eventBus.EmitSignal(GlobalEventBus.SignalName.CombatantsChanged);
        }
    }
}