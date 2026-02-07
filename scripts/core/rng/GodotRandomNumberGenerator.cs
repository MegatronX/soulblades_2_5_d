using Godot;

/// <summary>
/// An implementation of IRandomNumberGenerator that wraps Godot's built-in
/// RandomNumberGenerator class. This allows for seeded, deterministic randomness.
/// </summary>
public class GodotRandomNumberGenerator : IRandomNumberGenerator
{
    private readonly RandomNumberGenerator _rng = new();

    public GodotRandomNumberGenerator()
    {
        _rng.Randomize(); // Default behavior is to be non-deterministic.
    }

    public void SetSeed(ulong seed)
    {
        _rng.Seed = seed;
    }

    public int RandRangeInt(int min, int max) => _rng.RandiRange(min, max);

    public float RandRangeFloat(float min, float max) => _rng.RandfRange(min, max);
}