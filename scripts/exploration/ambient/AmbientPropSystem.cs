using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Spawns/despawns ambient prop scenes around a focus target.
/// </summary>
[GlobalClass]
public partial class AmbientPropSystem : Node3D
{
    [Export]
    public AmbientPropProfile Profile { get; private set; }

    [Export]
    public NodePath FollowTargetPath { get; private set; }

    [Export]
    public bool AutoFindPlayerCharacter { get; private set; } = true;

    private readonly List<Node3D> _activeProps = new();
    private readonly Dictionary<Node3D, AmbientPropSpawnEntry> _activePropEntries = new();
    private readonly RandomNumberGenerator _rng = new();
    private double _spawnTimer;
    private Node3D _followTarget;

    public override void _Ready()
    {
        _rng.Randomize();
        _followTarget = GetNodeOrNull<Node3D>(FollowTargetPath);
        SpawnInitial();
    }

    public override void _Process(double delta)
    {
        _followTarget ??= ResolveFollowTarget();
        PruneDestroyed();
        PruneByDistance();

        if (!HasSpawnContent()) return;
        if (_activeProps.Count >= Profile.MaxActiveProps) return;

        _spawnTimer -= delta;
        if (_spawnTimer > 0d) return;

        _spawnTimer = Mathf.Max(0.1f, Profile.SpawnIntervalSeconds);
        TrySpawnOne();
    }

    private void SpawnInitial()
    {
        if (!HasSpawnContent()) return;
        int spawnCount = Mathf.Clamp(Profile.InitialSpawnCount, 0, Profile.MaxActiveProps);
        for (int i = 0; i < spawnCount; i++)
        {
            TrySpawnOne();
        }
    }

    private void TrySpawnOne()
    {
        if (!HasSpawnContent()) return;
        if (_activeProps.Count >= Profile.MaxActiveProps) return;

        var packed = ResolveSpawnScene(out var spawnEntry);
        if (packed == null) return;

        var instance = packed.Instantiate();
        if (instance is not Node3D node3D)
        {
            instance?.QueueFree();
            return;
        }

        AddChild(node3D);
        node3D.GlobalPosition = ResolveSpawnPosition();
        _activeProps.Add(node3D);
        if (spawnEntry != null)
        {
            _activePropEntries[node3D] = spawnEntry;
        }
    }

    private Vector3 ResolveSpawnPosition()
    {
        Vector3 anchor = _followTarget?.GlobalPosition ?? GlobalPosition;
        float angle = _rng.RandfRange(0f, Mathf.Tau);
        float radius = _rng.RandfRange(0f, Mathf.Max(0.1f, Profile.SpawnRadius));
        float height = _rng.RandfRange(-Profile.VerticalJitter, Profile.VerticalJitter);
        return anchor + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
    }

    private void PruneByDistance()
    {
        if (_followTarget == null || Profile == null) return;
        float maxDistance = Mathf.Max(Profile.DespawnRadius, Profile.SpawnRadius + 1f);
        float maxDistanceSq = maxDistance * maxDistance;

        for (int i = _activeProps.Count - 1; i >= 0; i--)
        {
            var prop = _activeProps[i];
            if (prop == null || !GodotObject.IsInstanceValid(prop))
            {
                _activeProps.RemoveAt(i);
                _activePropEntries.Remove(prop);
                continue;
            }

            if (prop.GlobalPosition.DistanceSquaredTo(_followTarget.GlobalPosition) > maxDistanceSq)
            {
                prop.QueueFree();
                _activeProps.RemoveAt(i);
                _activePropEntries.Remove(prop);
            }
        }
    }

    private void PruneDestroyed()
    {
        for (int i = _activeProps.Count - 1; i >= 0; i--)
        {
            var prop = _activeProps[i];
            if (prop != null && GodotObject.IsInstanceValid(prop))
            {
                continue;
            }

            _activeProps.RemoveAt(i);
            _activePropEntries.Remove(prop);
        }
    }

    private Node3D ResolveFollowTarget()
    {
        if (_followTarget != null && GodotObject.IsInstanceValid(_followTarget))
        {
            return _followTarget;
        }

        if (!AutoFindPlayerCharacter) return null;
        return GetTree()?.GetNodesInGroup(GameGroups.PlayerCharacters).OfType<Node3D>().FirstOrDefault();
    }

    private bool HasSpawnContent()
    {
        if (Profile == null) return false;
        if (Profile.SpawnEntries != null && Profile.SpawnEntries.Count > 0) return true;
        return Profile.PropScenes != null && Profile.PropScenes.Count > 0;
    }

    private PackedScene ResolveSpawnScene(out AmbientPropSpawnEntry selectedEntry)
    {
        selectedEntry = null;
        if (Profile == null) return null;

        if (Profile.SpawnEntries != null && Profile.SpawnEntries.Count > 0)
        {
            var candidates = new List<AmbientPropSpawnEntry>();
            int totalWeight = 0;
            foreach (var entry in Profile.SpawnEntries)
            {
                if (entry?.PropScene == null) continue;
                if (entry.MaxActiveCount > 0 && CountActiveForEntry(entry) >= entry.MaxActiveCount) continue;

                int weight = Mathf.Max(1, entry.Weight);
                totalWeight += weight;
                candidates.Add(entry);
            }

            if (candidates.Count > 0 && totalWeight > 0)
            {
                int roll = _rng.RandiRange(0, totalWeight - 1);
                foreach (var entry in candidates)
                {
                    int weight = Mathf.Max(1, entry.Weight);
                    if (roll < weight)
                    {
                        selectedEntry = entry;
                        return entry.PropScene;
                    }
                    roll -= weight;
                }

                selectedEntry = candidates[0];
                return selectedEntry.PropScene;
            }
        }

        if (Profile.PropScenes == null || Profile.PropScenes.Count == 0) return null;
        return Profile.PropScenes[_rng.RandiRange(0, Profile.PropScenes.Count - 1)];
    }

    private int CountActiveForEntry(AmbientPropSpawnEntry entry)
    {
        if (entry == null || _activePropEntries.Count == 0) return 0;
        int count = 0;
        foreach (var kvp in _activePropEntries)
        {
            if (kvp.Value == entry && kvp.Key != null && GodotObject.IsInstanceValid(kvp.Key))
            {
                count++;
            }
        }
        return count;
    }
}
