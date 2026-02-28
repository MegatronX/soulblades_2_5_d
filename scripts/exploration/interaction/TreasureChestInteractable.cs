using Godot;

/// <summary>
/// One-time chest interactable with open/closed visual state and item reward.
/// </summary>
[GlobalClass]
public partial class TreasureChestInteractable : ExplorationInteractableBase
{
    [Export]
    public NodePath SpritePath { get; private set; }

    [Export]
    public Texture2D ClosedTexture { get; private set; }

    [Export]
    public Texture2D OpenTexture { get; private set; }

    [Export]
    public ItemData RewardItem { get; private set; }

    [Export(PropertyHint.Range, "1,999,1")]
    public int RewardQuantity { get; private set; } = 1;

    private Sprite3D _sprite;

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<Sprite3D>(SpritePath);
        base._Ready();
    }

    protected override void OnAfterEffects(ExplorationInteractionContext context)
    {
        if (RewardItem != null && RewardQuantity > 0)
        {
            context?.InventoryManager?.AddItem(RewardItem, RewardQuantity);
            GD.Print($"[Chest] Received {RewardQuantity}x {RewardItem.ItemName}");
        }
    }

    protected override void OnConsumedStateChanged(bool consumed)
    {
        if (_sprite == null) return;
        _sprite.Texture = consumed ? OpenTexture ?? _sprite.Texture : ClosedTexture ?? _sprite.Texture;
    }
}
