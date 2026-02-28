using Godot;
using System.Linq;

/// <summary>
/// Area trigger that switches a map-layer participant to a target layer.
/// Useful for stairs, ramps, bridges, and overpass/underpass transitions.
/// </summary>
[GlobalClass]
public partial class MapLayerSwitchVolume : Area3D
{
    [Export]
    public int TargetLayer { get; private set; } = 0;

    [Export]
    public bool RestrictToPlayerCharacters { get; private set; } = true;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body == null) return;
        if (RestrictToPlayerCharacters && !body.IsInGroup(GameGroups.PlayerCharacters)) return;

        var participant = ResolveParticipant(body);
        participant?.SetLayer(TargetLayer);
    }

    private static MapLayerParticipant ResolveParticipant(Node node)
    {
        if (node == null) return null;

        var direct = node.GetNodeOrNull<MapLayerParticipant>(MapLayerParticipant.DefaultName);
        if (direct != null) return direct;

        foreach (var child in node.GetChildren().OfType<MapLayerParticipant>())
        {
            return child;
        }

        return node.GetParentOrNull<Node>()?.GetNodeOrNull<MapLayerParticipant>(MapLayerParticipant.DefaultName);
    }
}
