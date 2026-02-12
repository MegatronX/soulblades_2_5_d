using Godot;

/// <summary>
/// A status effect that prevents a character from taking their next N turns.
/// </summary>
[GlobalClass]
public partial class StopEffect : StatusEffect
{
    [Export]
    public int MinTurnsToSkip { get; private set; } = 2;

    [Export]
    public int MaxTurnsToSkip { get; private set; } = 5;

    public override void OnApply(Node owner, ActionDirector actionDirector)
    {
        base.OnApply(owner, actionDirector);

        // In a real implementation, this would call a method on the TurnManager
        // or a component on the owner that the TurnManager reads.
        // For example:
        // var turnManager = actionDirector.GetTurnManager(); // Access via ActionDirector
        // int turnsToSkip = new RandomNumberGenerator().RandiRange(MinTurnsToSkip, MaxTurnsToSkip);
        // turnManager.AddTurnSkip(owner, turnsToSkip);

        GD.Print($"{owner.Name} is now Stopped!");
    }

    // OnRemove would be called by the TurnManager after all skip tokens are consumed.
}
