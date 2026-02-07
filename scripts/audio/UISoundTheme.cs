using Godot;

/// <summary>
/// A resource defining a set of UI sounds. Can be used to override defaults in UISoundManager.
/// </summary>
[GlobalClass]
public partial class UISoundTheme : Resource
{
    [Export] public AudioStream NavigationSound { get; set; }
    [Export] public AudioStream ConfirmSound { get; set; }
    [Export] public AudioStream CancelSound { get; set; }
    [Export] public AudioStream PageFlipSound { get; set; }
    [Export] public AudioStream InvalidSound { get; set; }
}