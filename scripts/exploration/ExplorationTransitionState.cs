/// <summary>
/// Holds one-shot transition payloads between exploration maps.
/// </summary>
public static class ExplorationTransitionState
{
    private static string _pendingSpawnId;

    public static void SetPendingSpawnId(string spawnId)
    {
        _pendingSpawnId = spawnId;
    }

    public static string ConsumePendingSpawnId()
    {
        string value = _pendingSpawnId;
        _pendingSpawnId = null;
        return value;
    }

    public static void Clear()
    {
        _pendingSpawnId = null;
    }
}
