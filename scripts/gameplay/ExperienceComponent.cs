using Godot;

/// <summary>
/// Tracks experience for a character. Optional component.
/// </summary>
[GlobalClass]
public partial class ExperienceComponent : Node
{
    public const string NodeName = "ExperienceComponent";

    [Signal]
    public delegate void ExperienceChangedEventHandler(int newValue);

    [Export(PropertyHint.Range, "0,100000000,1")]
    public int CurrentExperience { get; private set; } = 0;

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;
        CurrentExperience += amount;
        EmitSignal(SignalName.ExperienceChanged, CurrentExperience);
    }
}
