using Godot;
using System.Collections.Generic;

/// <summary>
/// Manages "Charge Counters" for characters in battle.
/// Listens for specific Timed Hit events to generate charges.
/// </summary>
public partial class ChargeSystem : Node
{
    [Export]
    public string ChargeActionName { get; set; } = "Focus";

    [Signal]
    public delegate void ChargesChangedEventHandler(Node character, int newValue, int delta);

    private Dictionary<Node, int> _charges = new();
    private TimedHitManager _timedHitManager;

    public void Initialize(TimedHitManager timedHitManager)
    {
        // Clean up previous subscription if any
        if (_timedHitManager != null)
        {
            _timedHitManager.TimedHitResolved -= OnTimedHitResolved;
        }

        _timedHitManager = timedHitManager;
        _charges.Clear();
        
        if (_timedHitManager != null)
        {
            _timedHitManager.TimedHitResolved += OnTimedHitResolved;
        }
    }

    public override void _ExitTree()
    {
        if (_timedHitManager != null)
        {
            _timedHitManager.TimedHitResolved -= OnTimedHitResolved;
        }
    }

    private void OnTimedHitResolved(TimedHitRating rating, ActionContext context, TimedHitSettings settings)
    {
        // Only proceed if the action name matches the specific charge generating action
        if (context.SourceAction == null) // || context.SourceAction.CommandName != ChargeActionName)
        {
            return;
        }

        int chargesToAdd = 0;
        if (rating == TimedHitRating.Great)
        {
            chargesToAdd = 1;
        }
        else if (rating == TimedHitRating.Perfect)
        {
            chargesToAdd = 2;
        }

        if (chargesToAdd > 0)
        {
            AddCharges(context.Initiator, chargesToAdd);
        }
    }

    public void AddCharges(Node character, int amount)
    {
        if (!_charges.ContainsKey(character))
        {
            _charges[character] = 0;
        }
        _charges[character] += amount;
        GD.Print($"[ChargeSystem] {character.Name} gained {amount} charges. Total: {_charges[character]}");
        EmitSignal(SignalName.ChargesChanged, character, _charges[character], amount);
    }

    public int GetCharges(Node character)
    {
        return _charges.TryGetValue(character, out int count) ? count : 0;
    }

    public bool TrySpendCharges(Node character, int amount)
    {
        if (GetCharges(character) >= amount)
        {
            _charges[character] -= amount;
            GD.Print($"[ChargeSystem] {character.Name} spent {amount} charges. Remaining: {_charges[character]}");
            EmitSignal(SignalName.ChargesChanged, character, _charges[character], -amount);
            return true;
        }
        return false;
    }
}
