using Godot;

/// <summary>
/// Manages the HUD for the active combatant, displaying status information like Charges.
/// </summary>
[GlobalClass]
public partial class BattleHUD : Control
{
    [Export] private Label _chargeLabel;
    [Export] private Control _statusContainer; // Parent control to hide/show the entire status widget

    [ExportGroup("Animation")]
    [Export] public Color PulseColorGain { get; set; } = Colors.Cyan;
    [Export] public Color PulseColorSpend { get; set; } = Colors.Orange;
    [Export] public float PulseDuration { get; set; } = 0.3f;
    [Export] public float PulseScale { get; set; } = 1.5f;

    private BattleController _battleController;
    private GlobalEventBus _eventBus;
    private Node _currentCombatant;
    private int _previousCharges = -1;

    public override void _Ready()
    {
        // Locate the BattleController
        _battleController = GetTree().Root.FindChild("BattleController", true, false) as BattleController;
        if (_battleController != null)
        {
            this.Subscribe(
                () => _battleController.TurnStarted += OnTurnStarted,
                () => _battleController.TurnStarted -= OnTurnStarted
            );
            
            // Subscribe to TimedHitManager to update charges in real-time during action execution
            if (_battleController.ActionDirector != null && _battleController.ActionDirector.TimedHitManager != null)
            {
                var timedHitManager = _battleController.ActionDirector.TimedHitManager;
                this.Subscribe(
                    () => timedHitManager.TimedHitResolved += OnTimedHitResolved,
                    () => timedHitManager.TimedHitResolved -= OnTimedHitResolved
                );
            }
        }

        // Subscribe to ActionExecuted to update charges after they are spent
        _eventBus = GetNodeOrNull<GlobalEventBus>(GlobalEventBus.Path);
        if (_eventBus != null)
        {
            this.Subscribe(
                () => _eventBus.ActionExecuted += OnActionExecuted,
                () => _eventBus.ActionExecuted -= OnActionExecuted
            );
        }
        
        if (_statusContainer != null) _statusContainer.Hide();
    }

    private void OnTurnStarted(TurnManager.TurnData turnData)
    {
        _currentCombatant = turnData.Combatant;
        _previousCharges = -1; // Reset tracking for new character
        UpdateUI();
    }

    private void OnTimedHitResolved(TimedHitRating rating, ActionContext context, TimedHitSettings settings)
    {
        // Defer update to ensure ChargeSystem has processed the event first
        CallDeferred(nameof(UpdateUI));
    }

    private void OnActionExecuted(ActionContext context)
    {
        // Update UI in case charges were spent during the action
        CallDeferred(nameof(UpdateUI));
    }

    private void UpdateUI()
    {
        if (_currentCombatant == null || _battleController == null) return;

        // Only show HUD for player characters
        if (_battleController.IsPlayerSide(_currentCombatant))
        {
            if (_statusContainer != null) _statusContainer.Show();
            
            if (_chargeLabel != null && _battleController.ChargeSystem != null)
            {
                int charges = _battleController.ChargeSystem.GetCharges(_currentCombatant);
                
                // Detect change and animate
                if (_previousCharges != -1 && charges != _previousCharges)
                {
                    AnimateChargeChange(charges > _previousCharges);
                }

                _chargeLabel.Text = $"Charges: {charges}";
                _previousCharges = charges;
            }
        }
        else
        {
            if (_statusContainer != null) _statusContainer.Hide();
        }
    }

    private void AnimateChargeChange(bool gained)
    {
        if (_chargeLabel == null) return;

        // Ensure pivot is centered for scaling effect
        _chargeLabel.PivotOffset = _chargeLabel.Size / 2.0f;

        var tween = CreateTween();
        Color targetColor = gained ? PulseColorGain : PulseColorSpend;

        // Scale Up + Color
        tween.TweenProperty(_chargeLabel, "scale", Vector2.One * PulseScale, PulseDuration / 2.0f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(_chargeLabel, "modulate", targetColor, PulseDuration / 2.0f);

        // Scale Down + Reset Color
        tween.TweenProperty(_chargeLabel, "scale", Vector2.One, PulseDuration / 2.0f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
        tween.Parallel().TweenProperty(_chargeLabel, "modulate", Colors.White, PulseDuration / 2.0f);
    }
}
