using Godot;

/// <summary>
/// Captures a deep snapshot of a node subtree using PackedScene.
/// This avoids manual per-field snapshotting.
/// </summary>
public sealed class NodeSnapshot
{
    public string OriginalName { get; }
    public int MultiplayerAuthority { get; }
    public PackedScene Packed { get; }

    public NodeSnapshot(Node source)
    {
        OriginalName = source.Name;
        MultiplayerAuthority = source.GetMultiplayerAuthority();

        var duplicate = source.Duplicate((int)(Node.DuplicateFlags.UseInstantiation | Node.DuplicateFlags.Scripts | Node.DuplicateFlags.Groups));
        var packed = new PackedScene();
        packed.Pack(duplicate);
        Packed = packed;

        duplicate.QueueFree();
    }

    public Node Instantiate()
    {
        var instance = Packed.Instantiate();
        instance.Name = OriginalName;
        if (MultiplayerAuthority != 0)
        {
            instance.SetMultiplayerAuthority(MultiplayerAuthority);
        }
        return instance;
    }
}
