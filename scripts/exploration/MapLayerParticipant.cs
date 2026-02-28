using Godot;
using System.Collections.Generic;

/// <summary>
/// Applies map-layer depth/sorting changes to a participant's sprite visuals.
/// </summary>
[GlobalClass]
public partial class MapLayerParticipant : Node
{
    public const string DefaultName = "MapLayerParticipant";

    [Signal]
    public delegate void MapLayerChangedEventHandler(int previousLayer, int newLayer);

    [Export]
    public int InitialLayer { get; private set; } = 0;

    [Export]
    public int RenderPriorityPerLayer { get; private set; } = 12;

    [Export]
    public bool AutoCollectSpriteNodes { get; private set; } = true;

    [Export]
    public Godot.Collections.Array<NodePath> SpriteNodePaths { get; private set; } = new();

    public int CurrentLayer { get; private set; }

    private readonly List<SpriteBase3D> _sprites = new();
    private readonly List<int> _basePriorities = new();

    public override void _Ready()
    {
        CurrentLayer = InitialLayer;
        CollectSprites();
        ApplyLayerVisuals();
    }

    public void SetLayer(int layer)
    {
        if (layer == CurrentLayer) return;

        int previous = CurrentLayer;
        CurrentLayer = layer;
        ApplyLayerVisuals();
        EmitSignal(SignalName.MapLayerChanged, previous, CurrentLayer);
    }

    public void Refresh()
    {
        CollectSprites();
        ApplyLayerVisuals();
    }

    private void CollectSprites()
    {
        _sprites.Clear();
        _basePriorities.Clear();

        if (AutoCollectSpriteNodes)
        {
            CollectSpritesRecursive(GetParentOrNull<Node>());
        }

        foreach (var path in SpriteNodePaths)
        {
            if (path == null || path.IsEmpty) continue;
            var sprite = GetNodeOrNull<SpriteBase3D>(path);
            if (sprite == null) continue;
            if (_sprites.Contains(sprite)) continue;

            _sprites.Add(sprite);
            _basePriorities.Add(sprite.RenderPriority);
        }
    }

    private void CollectSpritesRecursive(Node node)
    {
        if (node == null) return;

        if (node is SpriteBase3D sprite)
        {
            _sprites.Add(sprite);
            _basePriorities.Add(sprite.RenderPriority);
        }

        foreach (Node child in node.GetChildren())
        {
            CollectSpritesRecursive(child);
        }
    }

    private void ApplyLayerVisuals()
    {
        int count = Mathf.Min(_sprites.Count, _basePriorities.Count);
        for (int i = 0; i < count; i++)
        {
            var sprite = _sprites[i];
            if (sprite == null || !GodotObject.IsInstanceValid(sprite)) continue;
            sprite.RenderPriority = _basePriorities[i] + (CurrentLayer * RenderPriorityPerLayer);
        }
    }
}
