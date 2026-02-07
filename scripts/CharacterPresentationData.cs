using Godot;

/// <summary>
/// A Resource that holds all presentation-related data for a character,
/// such as their name, portraits, and UI images.
/// </summary>
[GlobalClass]
public partial class CharacterPresentationData : Resource
{
    [Export]
    public string DisplayName { get; private set; }

    [Export(PropertyHint.ResourceType, "Texture2D")]
    public Texture2D TurnQueueIcon { get; private set; }

    [Export(PropertyHint.ResourceType, "Texture2D")]
    public Texture2D PortraitImage { get; private set; }
}