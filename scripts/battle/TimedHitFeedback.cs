using Godot;

/// <summary>
/// Human-readable timing bands for timed hit/block feedback.
/// This is presentation-only and does not alter gameplay multipliers.
/// </summary>
public enum TimedHitTimingBand
{
    Early = 0,
    GreatEarly = 1,
    Perfect = 2,
    GreatLate = 3,
    Late = 4
}

public static class TimedHitFeedback
{
    public static TimedHitTimingBand Classify(TimedHitRating rating, float signedOffsetSeconds)
    {
        if (rating == TimedHitRating.Perfect)
        {
            return TimedHitTimingBand.Perfect;
        }

        bool isEarly = signedOffsetSeconds > 0f;
        if (rating == TimedHitRating.Great)
        {
            return isEarly ? TimedHitTimingBand.GreatEarly : TimedHitTimingBand.GreatLate;
        }

        return isEarly ? TimedHitTimingBand.Early : TimedHitTimingBand.Late;
    }

    public static string GetLabel(TimedHitTimingBand band)
    {
        return band switch
        {
            TimedHitTimingBand.GreatEarly => "Great (Early)",
            TimedHitTimingBand.Perfect => "Perfect",
            TimedHitTimingBand.GreatLate => "Great (Late)",
            TimedHitTimingBand.Late => "Late",
            _ => "Early"
        };
    }

    public static Color GetColor(TimedHitTimingBand band)
    {
        return band switch
        {
            TimedHitTimingBand.GreatEarly => new Color(0.66f, 0.93f, 1.00f, 0.82f),
            TimedHitTimingBand.Perfect => new Color(1.00f, 0.95f, 0.62f, 0.90f),
            TimedHitTimingBand.GreatLate => new Color(0.66f, 0.93f, 1.00f, 0.82f),
            TimedHitTimingBand.Late => new Color(0.85f, 0.86f, 0.94f, 0.74f),
            _ => new Color(0.85f, 0.86f, 0.94f, 0.74f)
        };
    }
}
