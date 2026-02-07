using Godot;
using System;
using System.Collections.Generic;

public enum TimedHitRating
{
    Miss,
    Good,
    Great,
    Perfect
}

/// <summary>
/// Manages the logic for timed hits, including input detection and rating calculation.
/// </summary>
public partial class TimedHitManager : Node
{
    [Signal]
    public delegate void TimedHitResolvedEventHandler(TimedHitRating rating, ActionContext context, TimedHitSettings settings);

    [Export] private float _difficultyMultiplier = 1.0f; // Higher = tighter windows

    // Base window sizes in seconds (half-width, so +/- this amount)
    private const float BasePerfectWindow = 0.05f;
    private const float BaseGreatWindow = 0.1f;
    private const float BaseGoodWindow = 0.2f;

    private class ActiveWindow
    {
        public TimedHitSettings Settings;
        public ActionContext Context;
        public float TimeRemaining;
    }
    private List<ActiveWindow> _activeWindows = new();

    public override void _Ready()
    {
        SetProcess(false);
    }

    public void StartWindow(TimedHitSettings settings, ActionContext context, float timeToHit)
    {
        var window = new ActiveWindow
        {
            Settings = settings,
            Context = context,
            TimeRemaining = timeToHit
        };
        _activeWindows.Add(window);
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (_activeWindows.Count == 0)
        {
            SetProcess(false);
            return;
        }

        // 1. Check for timeouts (Miss)
        // Iterate backwards to allow safe removal
        for (int i = _activeWindows.Count - 1; i >= 0; i--)
        {
            var window = _activeWindows[i];
            window.TimeRemaining -= (float)delta;

            if (window.TimeRemaining < -BaseGoodWindow)
            {
                ResolveHit(window, TimedHitRating.Miss);
                _activeWindows.RemoveAt(i);
            }
        }
        
        // 2. Check for input
        if (Input.IsActionJustPressed(GameInputs.GetActionName(GameInputAction.Confirm))) 
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        // Find the window closest to its target time
        ActiveWindow bestCandidate = null;
        float bestDiff = float.MaxValue;

        foreach (var window in _activeWindows)
        {
            float diff = Mathf.Abs(window.TimeRemaining);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestCandidate = window;
            }
        }

        if (bestCandidate != null)
        {
            TimedHitRating rating = CalculateRating(bestDiff);
            ResolveHit(bestCandidate, rating);
            _activeWindows.Remove(bestCandidate);
        }
    }

    private TimedHitRating CalculateRating(float diff)
    {
        // Apply difficulty scaling (tighter windows for higher difficulty)
        float scale = 1.0f / _difficultyMultiplier;

        if (diff <= BasePerfectWindow * scale) return TimedHitRating.Perfect;
        if (diff <= BaseGreatWindow * scale) return TimedHitRating.Great;
        if (diff <= BaseGoodWindow * scale) return TimedHitRating.Good;
        
        return TimedHitRating.Miss;
    }

    private void ResolveHit(ActiveWindow window, TimedHitRating rating)
    {
        GD.Print($"Timed Hit Result: {rating}");

        EmitSignal(SignalName.TimedHitResolved, (int)rating, window.Context, window.Settings);
        
        // Store rating in context for UI
        window.Context.LastTimedHitRating = rating;

        if (rating != TimedHitRating.Miss)
        {
            window.Context.RuntimeEvents.Add("TimedHitSuccess");

            // Calculate bonus based on rating
            // Perfect = 100% of the settings bonus, Great = 80%, Good = 50%
            float ratingFactor = rating == TimedHitRating.Perfect ? 1.0f : (rating == TimedHitRating.Great ? 0.8f : 0.5f);
            
            // Add to the cumulative multiplier (e.g. +0.25f * 1.0)
            float bonus = (window.Settings.DamageMultiplier - 1.0f) * ratingFactor;
            window.Context.TimedHitMultiplier += bonus;
        }
    }

    public void ClearAll()
    {
        _activeWindows.Clear();
        SetProcess(false);
    }
}