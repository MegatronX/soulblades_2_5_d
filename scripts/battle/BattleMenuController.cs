using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Controls the in-battle command menu, handling navigation, L/R page flipping, and sub-menus.
/// Supports nested sub-menus via a stack system.
/// </summary>
public partial class BattleMenuController : Control
{
    [Signal]
    public delegate void ActionCommittedEventHandler(BattleCommand action);

    [Export] private FoldableMenu _primaryMenu;
    [Export] private FoldableMenu _subMenu;
    [Export] private Control _pageIndicatorContainer;
    [Export] private Label _pageLabel;
    
    [Export] private Texture2D _pageIndicatorActive;
    [Export] private Texture2D _pageIndicatorInactive;
    [Export] private MenuTheme _defaultTheme;

    private ActionManager _currentActionManager;
    private IInputProvider _inputProvider;
    private BattleController _battleController;
    private int _currentPageIndex = 0;
    private bool _isAnimating = false;
    private bool _isTargeting = false;

    private List<FoldableMenu> _menuStack = new();
    private Dictionary<FoldableMenu, int> _menuSelectionHistory = new();
    private int _savedFocusIndex = -1;

    public override void _Ready()
    {
        Hide(); // Start hidden

        // Find the BattleController in the scene to listen for turn events
        _battleController = GetTree().Root.FindChild("BattleController", true, false) as BattleController;
        if (_battleController != null)
        {
            this.Subscribe(
                () => _battleController.TurnStarted += OnTurnStarted,
                () => _battleController.TurnStarted -= OnTurnStarted
            );
        }
        else
        {
            GD.PrintErr("BattleMenuController could not find BattleController!");
        }

        // Setup Primary Menu
        _primaryMenu.CommandSelected += (cmd, idx) => OnCommandSelected(_primaryMenu, cmd, idx);
        
        // We use _subMenu as a template for dynamic sub-menus. Hide it initially.
        _subMenu.Hide();
        _menuStack.Add(_primaryMenu);

        // Listen for targeting events
        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        this.Subscribe(
            () => eventBus.TargetingConfirmed += OnTargetingConfirmed,
            () => eventBus.TargetingConfirmed -= OnTargetingConfirmed
        );
        this.Subscribe(
            () => eventBus.TargetingCancelled += OnTargetingCancelled,
            () => eventBus.TargetingCancelled -= OnTargetingCancelled
        );
    }

    /// <summary>
    /// Called when a turn starts. Connect this to the BattleController signal.
    /// </summary>
    public void OnTurnStarted(TurnManager.TurnData turnData)
    {
        var character = turnData.Combatant;

        // 1. Check for AI Precedence
        // If an AIController is present and active (not suppressed), it takes control.
        var aiController = character.GetNodeOrNull<AIController>(AIController.DefaultName);
        if (aiController != null && !aiController.IsSuppressed)
        {
            Hide();
            return;
        }

        // Only show menu if it's the local player's character
        if (!character.IsMultiplayerAuthority()) 
        {
            Hide();
            return;
        }

        _currentActionManager = character.GetNodeOrNull<ActionManager>(ActionManager.DefaultName);
        if (_currentActionManager == null)
        {
            GD.PrintErr($"Active character {character.Name} missing ActionManager!");
            return;
        }

        // 2. Get InputProvider from PlayerController
        var playerController = character.GetNodeOrNull<PlayerController>(PlayerController.DefaultName);
        
        // If there is no PlayerController, we hide the menu as requested.
        // This handles cases where a character might have authority but no controller (e.g. unassigned or confused).
        if (playerController == null)
        {
            Hide();
            return;
        }
        
        _inputProvider = playerController.InputProvider;

        _currentPageIndex = 0;
        _isAnimating = false; // Reset animation state to prevent locking if previous turn ended mid-animation
        BuildPrimaryMenu();
        SetupPageIndicators();
        Show();
        Modulate = new Color(1, 1, 1, 1); // Ensure full opacity
        _isTargeting = false;
        _primaryMenu.FocusFirst();
    }

    public override void _Process(double delta)
    {
        if (!Visible || _currentActionManager == null || _inputProvider == null || _isAnimating || _isTargeting) return;

        // Confirm selection
        if (_inputProvider.IsActionJustPressed(GameInputAction.Confirm))
        {
            var activeMenu = _menuStack[_menuStack.Count - 1];
            activeMenu.TriggerFocusedOption();
        }

        // L/R Shoulder buttons to flip pages (Kingdom Hearts style)
        // Only allow flipping if we are in the root menu
        if (_menuStack.Count == 1)
        {
            if (_inputProvider.IsActionJustPressed(GameInputAction.AuxRight))
            {
                UISoundManager.Instance?.Play(UISoundType.PageFlip);
                ChangePage(1);
            }
            else if (_inputProvider.IsActionJustPressed(GameInputAction.AuxLeft))
            {
                UISoundManager.Instance?.Play(UISoundType.PageFlip);
                ChangePage(-1);
            }
        }
        
        // Back button to close sub-menu
        if (_menuStack.Count > 1 && _inputProvider.IsActionJustPressed(GameInputAction.Cancel))
        {
            UISoundManager.Instance?.Play(UISoundType.Cancel);
            CloseTopMenu();
        }
    }

    private void ChangePage(int direction)
    {
        int newIndex = _currentPageIndex + direction;
        // Wrap around logic
        if (newIndex >= _currentActionManager.RootPages.Count) newIndex = 0;
        if (newIndex < 0) newIndex = _currentActionManager.RootPages.Count - 1;

        if (newIndex != _currentPageIndex)
        {
            _currentPageIndex = newIndex;
            BuildPrimaryMenu();
            UpdatePageIndicators();
        }
    }

    private void BuildPrimaryMenu()
    {
        // Cleanup any dynamic menus from previous states
        foreach (var menu in _menuStack)
        {
            if (menu != _primaryMenu && menu != _subMenu)
            {
                menu.QueueFree();
            }
        }
        _menuStack.Clear();
        _menuStack.Add(_primaryMenu);
        _menuSelectionHistory.Clear();

        var commands = _currentActionManager.GetCommandsForPage(_currentPageIndex);
        var theme = _currentActionManager.GetPageTheme(_currentPageIndex) ?? _defaultTheme;
        
        _primaryMenu.BuildMenu(commands, theme);
        
        if (_pageLabel != null)
            _pageLabel.Text = $"Deck {_currentPageIndex + 1}";
    }

    private async void OnCommandSelected(FoldableMenu currentMenu, BattleCommand command, int index)
    {
        if (_isTargeting) return; // Prevent double-clicks or race conditions

        if (command is BattleCategory category)
        {
            if (_isAnimating) return;
            _isAnimating = true;

            // Open Sub-Menu
            _menuSelectionHistory[currentMenu] = index;

            // 1. Fold the current menu into a sidebar
            await currentMenu.FoldToSidebar(category.CommandName);

            // 2. Create a new sub-menu instance using _subMenu as a template
            var nextMenu = (FoldableMenu)_subMenu.Duplicate((int)DuplicateFlags.Scripts);
            AddChild(nextMenu);
            
            // Position it to the right of the folded menu
            nextMenu.GlobalPosition = new Vector2(currentMenu.GlobalPosition.X + currentMenu.Size.X + 10, currentMenu.GlobalPosition.Y);
            nextMenu.Show();

            // Use the category's specific theme, or fallback to page theme
            var theme = category.Theme ?? _currentActionManager.GetPageTheme(_currentPageIndex) ?? _defaultTheme;
            nextMenu.BuildMenu(new List<BattleCommand>(category.SubCommands), theme);
            
            // Connect signal recursively
            nextMenu.CommandSelected += (c, i) => OnCommandSelected(nextMenu, c, i);
            
            _menuStack.Add(nextMenu);
            nextMenu.FocusFirst();
            
            _isAnimating = false;
        }
        else
        {
            // It's a leaf command (ActionData or BattleAction). Commit it.
            InitiateTargeting(command);
        }
    }

    private void InitiateTargeting(BattleCommand command)
    {
        _isTargeting = true;
        
        // Save the current focus so we can restore it if targeting is cancelled.
        var activeMenu = _menuStack[_menuStack.Count - 1];
        _savedFocusIndex = activeMenu.GetFocusedIndex();

        // Explicitly disable input on buttons to stop Godot's auto-navigation
        foreach (var menu in _menuStack)
        {
            menu.SetInputEnabled(false);
        }
        
        // Release focus from the UI so gamepad/keyboard inputs don't trigger navigation
        GetViewport().GuiReleaseFocus();

        // Fade menu to partial opacity to indicate focus has shifted to the field
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.5f, 0.2f);

        // Notify the system that targeting should start
        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        Node actor = _currentActionManager.GetParent();
        eventBus.EmitSignal(GlobalEventBus.SignalName.ActionSelectedForTargeting, command, actor);
    }

    private async void OnTargetingConfirmed(BattleCommand command, Godot.Collections.Array<Node> targets)
    {
        if (!_isTargeting) return;
        _isTargeting = false;
        
        // Re-enable input (though we are hiding immediately, so it matters less here)
        foreach (var menu in _menuStack)
        {
            menu.SetInputEnabled(true);
        }

        // 1. Emit local signal for UI feedback
        EmitSignal(SignalName.ActionCommitted, command);

        // 2. Send RPC to server
        if (_battleController != null)
        {
            // We pass the resource path so the server can load the exact same ActionData
            // TODO: Pass targets to the server once the RPC signature is updated
            var targetPaths = targets.Select(t => t.GetPath().ToString()).ToArray();

            if (Multiplayer.IsServer())
            {
                _battleController.Server_PlayerCommitAction(command.ResourcePath, targetPaths);
            }
            else
            {
                _battleController.RpcId(1, nameof(BattleController.Server_PlayerCommitAction), command.ResourcePath, targetPaths);
            }
        }

        // 3. Fade out completely and hide
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.2f);
        await ToSignal(tween, Tween.SignalName.Finished);
        Hide();
    }

    private void OnTargetingCancelled()
    {
        if (!_isTargeting) return;

        // Re-enable input to resume menu interaction.
        foreach (var menu in _menuStack)
        {
            menu.SetInputEnabled(true);
        }

        // Restore focus to the button that was selected before targeting started.
        if (_menuStack.Count > 0 && _savedFocusIndex >= 0)
        {
            _menuStack[_menuStack.Count - 1].FocusIndex(_savedFocusIndex);
        }

        // Fade menu back in
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.2f);

        // Defer the state change to the next frame to prevent the "Cancel" input 
        // from being processed by the menu logic in the same frame.
        CallDeferred(nameof(EndTargetingState));
    }

    private void EndTargetingState() => _isTargeting = false;

    private async void CloseTopMenu()
    {
        if (_menuStack.Count <= 1) return; // Cannot close primary menu

        if (_isAnimating) return;
        _isAnimating = true;

        var topMenu = _menuStack[_menuStack.Count - 1];
        _menuStack.RemoveAt(_menuStack.Count - 1);

        await topMenu.HideMenu();
        topMenu.QueueFree();

        var prevMenu = _menuStack[_menuStack.Count - 1];
        await prevMenu.Unfold();

        if (_menuSelectionHistory.TryGetValue(prevMenu, out int index))
        {
            prevMenu.FocusIndex(index);
        }
        else
        {
            prevMenu.FocusFirst();
        }
        _isAnimating = false;
    }

    private void SetupPageIndicators()
    {
        if (_pageIndicatorContainer == null) return;

        // Clear existing indicators
        foreach (Node child in _pageIndicatorContainer.GetChildren())
        {
            // Prevent deleting the page label if it resides in this container
            if (child == _pageLabel) continue;
            child.QueueFree();
        }

        int pageCount = _currentActionManager.RootPages.Count;
        // Don't show indicators if there's only 1 page
        if (pageCount <= 1) 
        {
            _pageIndicatorContainer.Hide();
            return;
        }

        _pageIndicatorContainer.Show();

        for (int i = 0; i < pageCount; i++)
        {
            var rect = new TextureRect();
            rect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            rect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            rect.CustomMinimumSize = new Vector2(16, 16); // Adjust size as needed
            _pageIndicatorContainer.AddChild(rect);
        }
        
        UpdatePageIndicators();
    }

    private void UpdatePageIndicators()
    {
        if (_pageIndicatorContainer == null || !_pageIndicatorContainer.Visible) return;

        var indicators = _pageIndicatorContainer.GetChildren();
        for (int i = 0; i < indicators.Count; i++)
        {
            if (indicators[i] is TextureRect rect)
            {
                rect.Texture = (i == _currentPageIndex) ? _pageIndicatorActive : _pageIndicatorInactive;
                // Optional: Scale up the active one slightly
                rect.Scale = (i == _currentPageIndex) ? new Vector2(1.2f, 1.2f) : Vector2.One;
            }
        }
    }
}
