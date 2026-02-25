using System.Collections.Generic;

/// <summary>
/// Implemented by status/effect resources that can describe deterministic stat
/// deltas for turn-order preview simulation.
/// </summary>
public interface ITurnPreviewStatDeltaProvider
{
    IEnumerable<TurnPreviewStatDelta> GetTurnPreviewStatDeltas();
}
