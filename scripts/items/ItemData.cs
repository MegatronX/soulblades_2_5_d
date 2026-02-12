using Godot;

/// <summary>
/// The base resource for all items in the game. An item's functionality
/// is defined by the ItemComponentData resources attached to it.
/// </summary>
[GlobalClass]
public partial class ItemData : Resource
{
    [Export]
    public string ItemName { get; set; }

    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; }

    [Export]
    public int Value { get; private set; }

    [Export]
    public bool IsKeyItem { get; private set; }

    [Export]
    public Texture2D Icon { get; private set; }

    [Export]
    public Godot.Collections.Array<ItemComponentData> Components { get; private set; } = new();

}
