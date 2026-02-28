using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Runtime map-state storage keyed by map id and object key.
/// Persists across scene changes for the current play session.
/// </summary>
public static class MapRuntimeStateStore
{
    private static readonly Dictionary<string, Dictionary<string, Variant>> _mapState
        = new(StringComparer.Ordinal);

    public static bool GetBool(string mapId, string key, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(mapId) || string.IsNullOrWhiteSpace(key))
        {
            return defaultValue;
        }

        if (!_mapState.TryGetValue(mapId, out var state))
        {
            return defaultValue;
        }

        if (!state.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        return value.VariantType == Variant.Type.Bool ? value.AsBool() : defaultValue;
    }

    public static void SetBool(string mapId, string key, bool value)
    {
        if (string.IsNullOrWhiteSpace(mapId) || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (!_mapState.TryGetValue(mapId, out var state))
        {
            state = new Dictionary<string, Variant>(StringComparer.Ordinal);
            _mapState[mapId] = state;
        }

        state[key] = value;
    }

    public static Variant GetValue(string mapId, string key, Variant defaultValue = default)
    {
        if (string.IsNullOrWhiteSpace(mapId) || string.IsNullOrWhiteSpace(key))
        {
            return defaultValue;
        }

        if (!_mapState.TryGetValue(mapId, out var state))
        {
            return defaultValue;
        }

        return state.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public static void SetValue(string mapId, string key, Variant value)
    {
        if (string.IsNullOrWhiteSpace(mapId) || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (!_mapState.TryGetValue(mapId, out var state))
        {
            state = new Dictionary<string, Variant>(StringComparer.Ordinal);
            _mapState[mapId] = state;
        }

        state[key] = value;
    }

    public static void ClearMap(string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId)) return;
        _mapState.Remove(mapId);
    }

    public static void ClearAll()
    {
        _mapState.Clear();
    }
}
