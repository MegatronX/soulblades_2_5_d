using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Snapshot for a collection of nodes.
/// </summary>
public sealed class NodeCollectionSnapshot
{
    private readonly List<NodeSnapshot> _snapshots;

    public NodeCollectionSnapshot(IEnumerable<Node> nodes)
    {
        _snapshots = nodes?.Select(n => new NodeSnapshot(n)).ToList() ?? new List<NodeSnapshot>();
    }

    public List<Node> InstantiateAll()
    {
        return _snapshots.Select(s => s.Instantiate()).ToList();
    }
}
