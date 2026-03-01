using Godot;
using System.Collections.Generic;

/// <summary>
/// Shared list/random helpers for deterministic in-place randomization.
/// </summary>
public static class RandomListUtils
{
    public static void ShuffleInPlace<T>(IList<T> list, RandomNumberGenerator rng)
    {
        if (list == null || list.Count <= 1 || rng == null) return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.RandiRange(0, i);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
