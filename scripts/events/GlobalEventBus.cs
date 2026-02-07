using Godot;

/// <summary>
/// A global event bus for game-wide signals. This acts as a "bridge"
/// to allow various game systems to communicate without being directly aware of each other.
/// This should be configured as an Autoload singleton in Project Settings.
/// </summary>
public partial class GlobalEventBus : Node
{
    public const string Path = "/root/GlobalEventBus";

    /// <summary>
    /// Emitted when a player's UI wants to preview the outcome of a potential action.
    /// The TurnOrderPreviewUI will listen for this.
    /// </summary>
    [Signal]
    public delegate void ActionPreviewRequestedEventHandler(TurnManager.ActionPreview actionPreview, Node activeCombatant);

    /// <summary>
    /// Emitted when the UI should stop showing an action preview.
    /// </summary>
    [Signal]
    public delegate void ActionPreviewCancelledEventHandler();

    /// <summary>
    /// Emitted after a turn has been successfully committed and the game state has advanced.
    /// The TurnOrderPreviewUI will listen for this to update its display.
    /// </summary>
    [Signal]
    public delegate void TurnCommittedEventHandler();

    /// <summary>
    /// Emitted when the roster of combatants changes (e.g., a summon or defeat).
    /// The TurnOrderPreviewUI will listen for this to trigger a general refresh.
    /// </summary>
    [Signal]
    public delegate void CombatantsChangedEventHandler();

    /// <summary>
    /// Emitted when an action is selected in the menu, initiating the targeting phase.
    /// </summary>
    [Signal]
    public delegate void ActionSelectedForTargetingEventHandler(BattleCommand action, Node actor);

    /// <summary>
    /// Emitted when targeting is confirmed. Contains the action and the selected targets.
    /// </summary>
    [Signal]
    public delegate void TargetingConfirmedEventHandler(BattleCommand action, Godot.Collections.Array<Node> targets);

    /// <summary>
    /// Emitted when targeting is cancelled, returning control to the menu.
    /// </summary>
    [Signal]
    public delegate void TargetingCancelledEventHandler();

    /// <summary>
    /// Emitted when the set of currently selected targets changes during the targeting phase.
    /// </summary>
    [Signal]
    public delegate void TargetSelectionChangedEventHandler(Godot.Collections.Array<Node> targets);

    /// <summary>
    /// Emitted to request a 2D sound effect (non-positional).
    /// </summary>
    [Signal]
    public delegate void PlaySFXRequestedEventHandler(AudioStream stream, float volumeDb, float pitchScale);

    /// <summary>
    /// Emitted to request a 3D sound effect at a specific world position.
    /// </summary>
    [Signal]
    public delegate void PlaySFX3DRequestedEventHandler(AudioStream stream, Vector3 position, float volumeDb, float pitchScale);

    /// <summary>
    /// Emitted when an action has been fully executed (animation and mechanics complete).
    /// Useful for AI learning, logging, or achievements.
    /// </summary>
    [Signal]
    public delegate void ActionExecutedEventHandler(ActionContext context);

    /// <summary>
    /// Emitted when an AI wants to "bark" or shout a message to the chat/UI.
    /// </summary>
    [Signal]
    public delegate void AIShoutedEventHandler(string message);
}