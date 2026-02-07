/// <summary>
/// Defines a contract for a random number generator. This allows for swapping
/// different RNG implementations, such as a seeded generator for replays
/// or a mock generator for testing.
/// </summary>
public interface IRandomNumberGenerator
{
    /// <summary>
    /// Sets the seed for the random number generator to produce a deterministic sequence.
    /// </summary>
    void SetSeed(ulong seed);
    int RandRangeInt(int min, int max);
    float RandRangeFloat(float min, float max);
}