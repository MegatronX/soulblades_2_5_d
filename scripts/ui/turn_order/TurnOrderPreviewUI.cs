using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Manages the UI for displaying and previewing the turn order with high-quality animations.
/// This class is designed to be robust, handling additions, removals, and re-ordering of combatants smoothly.
/// </summary>
public partial class TurnOrderPreviewUI : Control
{
    /// <summary>
    /// Defines the layout direction for the turn order cards.
    /// </summary>
    public enum LayoutOrientation
    {
        Horizontal,
        Vertical
    }

    [ExportGroup("Configuration")]
    [Export] private PackedScene _cardScene;
    [Export] private LayoutOrientation _orientation = LayoutOrientation.Vertical;
    [Export] private float _cardSpacing = 110f;
    [Export] private bool _debugCompareOrders = false;
    [Export] private bool _includeBlockedTurns = true;

    private TurnManager _turnManager;
    private readonly List<TurnOrderCard> _cards = new();
    private List<TurnManager.TurnData> _currentTurnOrder;
    private bool _isAnimating = false;
    private List<TurnManager.TurnData> _pendingNewOrder;
    private List<TurnManager.TurnData> _pendingOldOrder;
    private readonly List<TurnOrderCard> _debugCards = new();
    private HashSet<Node> _highlightedCombatants = new();

    /// <summary>
    /// Initializes the UI with the TurnManager and performs the initial display.
    /// </summary>
    public void Initialize(TurnManager turnManager)
    {
        _turnManager = turnManager;

        // Connect to global events to react to game state changes.
        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        this.Subscribe(
            () => eventBus.ActionPreviewRequested += OnActionPreviewRequested,
            () => eventBus.ActionPreviewRequested -= OnActionPreviewRequested
        );
        this.Subscribe(
            () => eventBus.ActionPreviewCancelled += OnActionPreviewCancelled,
            () => eventBus.ActionPreviewCancelled -= OnActionPreviewCancelled
        );
        this.Subscribe(
            () => eventBus.TurnCommitted += OnTurnCommitted,
            () => eventBus.TurnCommitted -= OnTurnCommitted
        );
        this.Subscribe(
            () => eventBus.CombatantsChanged += OnCombatantsChanged,
            () => eventBus.CombatantsChanged -= OnCombatantsChanged
        );
        this.Subscribe(
            () => eventBus.TargetSelectionChanged += OnTargetSelectionChanged,
            () => eventBus.TargetSelectionChanged -= OnTargetSelectionChanged
        );

        // Initial population of the turn order.
        _currentTurnOrder = _turnManager.GenerateTurnOrder(10, _includeBlockedTurns);
        _ = AnimateToOrder(_currentTurnOrder, isInitialLoad: true);
    }

    /// <summary>
    /// Applies a visual highlight to the cards of specified combatants.
    /// </summary>
    /// <param name="charactersToHighlight">A list of combatant nodes to highlight.</param>
    public void HighlightCharacters(IEnumerable<Node> charactersToHighlight)
    {
        _highlightedCombatants = new HashSet<Node>(charactersToHighlight);
        foreach (var card in _cards)
        {
            if (card.Data != null)
            {
                card.SetHighlight(_highlightedCombatants.Contains(card.Data.Combatant));
            }
        }
    }

    // --- Event Handlers ---

    private void OnActionPreviewRequested(TurnManager.ActionPreview actionPreview, Node actingCombatantNode)
    {
        if (_turnManager == null) return;
        
        var actingTurnData = _turnManager.GetCombatants().FirstOrDefault(c => c.Combatant == actingCombatantNode);
        if (actingTurnData == null) return;

        var previewTurnOrder = _turnManager.PreviewAction(10, actingTurnData, actionPreview, _includeBlockedTurns);
        _ = AnimateToOrder(previewTurnOrder, _currentTurnOrder);
    }

    private void OnActionPreviewCancelled()
    {
        _ = AnimateToOrder(_currentTurnOrder);
    }

    private void OnTurnCommitted()
    {
        var newOrder = _turnManager.GenerateTurnOrder(10, _includeBlockedTurns);
        var oldOrder = _currentTurnOrder; // Keep a reference to the old order

        _currentTurnOrder = newOrder; // Now update the current state
        // Pass both old and new orders to the animation method
        _ = AnimateToOrder(_currentTurnOrder, oldOrder);
    }

    private void OnCombatantsChanged()
    {
        var newOrder = _turnManager.GenerateTurnOrder(10, _includeBlockedTurns);
        // A combatant change should trigger a general re-sort, not the special "commit" animation.
        _currentTurnOrder = newOrder;
        _ = AnimateToOrder(_currentTurnOrder);
    }

    private void OnTargetSelectionChanged(Godot.Collections.Array<Node> targets)
    {
        HighlightCharacters(targets);
    }

    // --- Core Animation Logic ---

    private async Task AnimateToOrder(List<TurnManager.TurnData> newOrder, List<TurnManager.TurnData> oldOrder = null, bool isInitialLoad = false)
    {
        if (_isAnimating && !isInitialLoad)
        {
            // Coalesce rapid updates (e.g. fast target cycling) and render the latest
            // requested order after the current tween finishes.
            _pendingNewOrder = newOrder;
            _pendingOldOrder = oldOrder;
            return;
        }

        // Optimization: Check if the new order is identical to the current display
        if (!isInitialLoad && _cards.Count == newOrder.Count)
        {
            bool identical = true;
            for (int i = 0; i < _cards.Count; i++)
            {
                // Special case: The first card represents the currently acting character.
                // The 'TickValue' in _cards comes from GenerateTurnOrder, which simulates time passing until the character is ready (Counter >= 1000).
                // The 'TickValue' in newOrder comes from PreviewAction, which uses the raw current state (Counter might be < 1000).
                // This discrepancy causes a mismatch. We can safely ignore TickValue for the first item if the Combatant matches.
                bool isFirstItemAndSameCombatant = (i == 0 && _cards[i].Data.Combatant == newOrder[i].Combatant);

                if (_cards[i].Data.Combatant != newOrder[i].Combatant || 
                    (!isFirstItemAndSameCombatant && !Mathf.IsEqualApprox(_cards[i].Data.TickValue, newOrder[i].TickValue)))
                {
                    identical = false;
                    break;
                }
            }

            if (identical)
            {
                if (_debugCompareOrders) UpdateDebugCards(oldOrder);
                return;
            }
        }

        _isAnimating = true;
        if (_debugCompareOrders) UpdateDebugCards(oldOrder);

        bool isCommitAnimation = IsShiftedList(oldOrder, newOrder);
        var moveTween = CreateTween().SetParallel(true);

        if (isCommitAnimation)
        {
            // --- Path A: Special "Commit" Animation ---

            // 1. Find the top card, animate it out, and remove it from our list.
            var cardToPop = _cards.First();
            AnimateCardPop(cardToPop);
            _cards.Remove(cardToPop);

            // 2. Add a new card at the end to maintain the count.
            var newCard = _cardScene.Instantiate<TurnOrderCard>();
            newCard.Modulate = Colors.Transparent;
            newCard.Position = GetTargetPosition(_cards.Count); // Position it at the bottom.
            AddChild(newCard);
            _cards.Add(newCard);

            // 3. Animate all cards to their new positions.
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                card.SetData(newOrder[i]);
                card.SetHighlight(_highlightedCombatants.Contains(newOrder[i].Combatant));
                Vector2 targetPosition = GetTargetPosition(i);
                moveTween.TweenProperty(card, "position", targetPosition, 0.4f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
                moveTween.TweenProperty(card, "modulate", Colors.White, 0.3f);
            }
        }
        else
        {
            // --- Path B: General "Re-shuffle" Animation (for previews, summons, etc.) ---
            var finalCardOrder = new List<TurnOrderCard>();
            var cardsToRemove = new List<TurnOrderCard>();
            var availableCards = new List<TurnOrderCard>(_cards);

            // 1. Match cards from the old order to the new order.
            for (int i = 0; i < newOrder.Count; i++)
            {
                var newTurn = newOrder[i];
                // Relaxed check for the first item to handle TickValue discrepancies during previews
                var matchingCard = availableCards.FirstOrDefault(c => 
                    c.Data?.Combatant == newTurn.Combatant && 
                    (i == 0 || c.Data?.TickValue == newTurn.TickValue));

                if (matchingCard != null)
                {
                    finalCardOrder.Add(matchingCard);
                    availableCards.Remove(matchingCard);
                }
                else
                {
                    finalCardOrder.Add(null); // Placeholder for a new card.
                }
            }
            cardsToRemove.AddRange(availableCards);

            // 2. Fill in gaps and execute animations.
            for (int i = 0; i < finalCardOrder.Count; i++)
            {
                if (finalCardOrder[i] == null)
                {
                    // Repurpose an old card or create a new one.
                    finalCardOrder[i] = cardsToRemove.Any() ? cardsToRemove.First() : _cardScene.Instantiate<TurnOrderCard>();
                    if (cardsToRemove.Any()) cardsToRemove.RemoveAt(0);
                    else AddChild(finalCardOrder[i]);
                    finalCardOrder[i].Modulate = Colors.Transparent;
                }

                var card = finalCardOrder[i];
                card.SetData(newOrder[i]);
                card.SetHighlight(_highlightedCombatants.Contains(newOrder[i].Combatant));
                Vector2 targetPosition = GetTargetPosition(i);
                if (isInitialLoad) card.Position = targetPosition + (_orientation == LayoutOrientation.Vertical ? new Vector2(50, 0) : new Vector2(0, 50));
                moveTween.TweenProperty(card, "position", targetPosition, 0.4f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
                moveTween.TweenProperty(card, "modulate", Colors.White, 0.3f);
            }

            // 3. Animate out any truly leftover cards.
            foreach (var card in cardsToRemove)
            {
                AnimateCardFadeOut(card);
            }
            
            // 4. Update internal state for this path.
            _cards.Clear();
            _cards.AddRange(finalCardOrder);
        }

        if (isInitialLoad)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        await ToSignal(moveTween, Tween.SignalName.Finished);
        _isAnimating = false;

        if (_pendingNewOrder != null)
        {
            var queuedNewOrder = _pendingNewOrder;
            var queuedOldOrder = _pendingOldOrder;
            _pendingNewOrder = null;
            _pendingOldOrder = null;
            await AnimateToOrder(queuedNewOrder, queuedOldOrder);
        }
    }

    private void UpdateDebugCards(List<TurnManager.TurnData> order)
    {
        foreach (var card in _debugCards)
        {
            if (GodotObject.IsInstanceValid(card)) card.QueueFree();
        }
        _debugCards.Clear();

        if (order == null) return;

        for (int i = 0; i < order.Count; i++)
        {
            var card = _cardScene.Instantiate<TurnOrderCard>();
            AddChild(card);
            
            // Offset the debug cards to the side
            Vector2 offset = _orientation == LayoutOrientation.Vertical 
                ? new Vector2(130, 0) // Shift right
                : new Vector2(0, 70); // Shift down
                
            card.Position = GetTargetPosition(i) + offset;
            card.SetData(order[i]);
            card.Modulate = new Color(0.8f, 0.8f, 0.8f, 0.8f); // Dim slightly
            _debugCards.Add(card);
        }
    }

    // --- Animation Helpers ---

    private void AnimateCardPop(TurnOrderCard card)
    {   
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(card, "scale", card.Scale * 1.2f, 0.2f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "modulate:a", 0f, 0.25f).SetDelay(0.05);
        // Move the card to the top of the visual stack so it animates over others.
        card.MoveToFront(); // This is important for the visual effect.
        // This card is now "dead" and should be removed from the scene once faded.
        tween.Finished += card.QueueFree;
    }

    private void AnimateCardFadeOut(TurnOrderCard card)
    {
        var tween = CreateTween();
        tween.TweenProperty(card, "modulate:a", 0f, 0.3f);
        tween.Finished += card.QueueFree;
    }

    // --- Utility Methods ---

    private Vector2 GetTargetPosition(int index)
    {
        return _orientation == LayoutOrientation.Vertical
            ? new Vector2(0, index * _cardSpacing)
            : new Vector2(index * _cardSpacing, 0);
    }

    /// <summary>
    /// Checks if the new list is the same as the old list, just shifted up by one.
    /// This is the condition for our special "commit" animation.
    /// </summary>
    private bool IsShiftedList(List<TurnManager.TurnData> oldOrder, List<TurnManager.TurnData> newOrder)
    {
        // A commit animation requires a valid old order to compare against.
        if (oldOrder == null || oldOrder.Count < 2 || newOrder == null || newOrder.Count < 1)
        {
            return false;
        }

        // Check if the tail of the old list matches the head of the new list.
        // We only need to check if the second element of the old list is now the first element of the new list.
        return oldOrder[1].Combatant == newOrder[0].Combatant;
    }
}

public static class TweenExtensions
{
    public static Task ToTask(this Tween tween)
    {
        var tcs = new TaskCompletionSource<bool>();
        tween.Finished += () => tcs.SetResult(true);
        return tcs.Task;
    }
}
