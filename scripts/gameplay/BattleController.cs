using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Orchestrates a turn-based battle, managing combatants, the turn order, and the overall battle state.
/// This controller should only exist and run its logic on the server.
/// </summary>
public partial class BattleController : Node
{
    public enum BattleState
    {
        NotStarted,
        InProgress,
        Victory,
        Defeat
    }

    public BattleState CurrentState { get; private set; } = BattleState.NotStarted;

    [Export]
    private Node _playerTeamContainer;
    [Export]
    private Node _enemyTeamContainer;
    [Export]
    private Node _allyTeamContainer;

    [Export(PropertyHint.File, "*.tscn")]
    private PackedScene _turnOrderPreviewUIScene;

    private TurnManager _turnManager;
    private IRandomNumberGenerator _rng;
    private BattleContext _context;
    private BattleRoster _roster;
    private BattleTurnFlow _turnFlow;
    private BattleNetworkGateway _networkGateway;
    private GlobalEventBus _eventBus;
    private BattleRewardTracker _rewardTracker = new BattleRewardTracker();
    private BattleRewardsApplier _rewardsApplier = new BattleRewardsApplier();
    private BattleResultsOverlay _resultsOverlay;
    private Node _lastKillingBlowActor;
    private bool _isResolvingBattleEnd;

    [Export]
    private TurnOrderPreviewUI _turnOrderPreviewUI;

    [Export(PropertyHint.File, "*.tscn")]
    private PackedScene _battleResultsOverlayScene;

    [Export]
    private ActionDirector _actionDirector;
    public ActionDirector ActionDirector => _actionDirector;

    [Export]
    private ChargeSystem _chargeSystem;
    public ChargeSystem ChargeSystem => _chargeSystem;

    [Export]
    private BattlePlacementSettings _placementSettings;

    [Export]
    public BattleCamera BattleCamera { get; set; }

    [Export]
    private int _minCounterStart = 0;

    private int _normalMaxCounterStart = 200;

    [Export]
    private int _advantageMaxCounterStart = 400;


    [Export]
    private int _advantageMinCounterStart = 200;

    [Export]
    private int _disadvantageMaxCounterStart = 200;


    // A signal to announce whose turn it is. The UI and other systems will listen to this.
    // This is useful for the ActionMenu, character highlights, etc.
    [Signal]
    public delegate void TurnStartedEventHandler(TurnManager.TurnData activeCombatant);

    [Signal]
    public delegate void CombatantDefeatedEventHandler(Node combatant);

    [Signal]
    public delegate void BattleReadyEventHandler();

    [Signal]
    public delegate void BattleEndedEventHandler(BattleState result);

    public override void _Ready()
    {
        // This node should only be active on the server.
        if (!Multiplayer.IsServer())
        {
            SetProcess(false);
            SetPhysicsProcess(false);
            return;
        }

        _turnManager = new TurnManager();

        // Instantiate the Turn Order UI and add it to the scene.
        if (_turnOrderPreviewUIScene != null)
        {
            _turnOrderPreviewUI = _turnOrderPreviewUIScene.Instantiate<TurnOrderPreviewUI>();
            AddChild(_turnOrderPreviewUI);
        }

        if (_chargeSystem == null)
        {
            _chargeSystem = GetNodeOrNull<ChargeSystem>("ChargeSystem");
        }

        if (BattleCamera == null)
        {
            // Fallback: Look for BattleCamera in the scene root (MainBattleScene)
            var sceneRoot = GetTree().CurrentScene;
            if (sceneRoot != null)
            {
                // Try by name
                BattleCamera = sceneRoot.GetNodeOrNull<BattleCamera>("BattleCamera");

                // Try by type in root children
                if (BattleCamera == null)
                {
                    foreach (var child in sceneRoot.GetChildren())
                    {
                        if (child is BattleCamera cam)
                        {
                            BattleCamera = cam;
                            break;
                        }
                    }
                }
            }

            if (BattleCamera == null)
            {
                GD.PrintErr("BattleCamera is not assigned in BattleController and could not be found in the scene root.");
            }
        }

        // Add AI Debug Overlay in debug builds
        if (OS.IsDebugBuild())
        {
            var debugOverlay = new AIDebugOverlay();
            AddChild(debugOverlay);
        }

        _eventBus = GetNodeOrNull<GlobalEventBus>(GlobalEventBus.Path);
        if (_battleResultsOverlayScene != null)
        {
            _resultsOverlay = _battleResultsOverlayScene.Instantiate<BattleResultsOverlay>();
            AddChild(_resultsOverlay);
        }
        else
        {
            _resultsOverlay = new BattleResultsOverlay();
            AddChild(_resultsOverlay);
            GD.PrintErr("BattleResultsOverlay scene not assigned. Using code-only fallback.");
        }

        // The BattleController is now responsible for setting up the battle
        // using the data prepared in the GameManager.
        CallDeferred(nameof(SetupBattleFromGameManager));
    }

    private void SetupBattleFromGameManager()
    {
        var gameManager = GetNode<GameManager>(GameManager.Path);
        if (_placementSettings == null) _placementSettings = new BattlePlacementSettings();
        gameManager.GetOrCreatePendingBattleConfig();

        var bootstrapper = new BattleBootstrapper(_playerTeamContainer, _enemyTeamContainer, _allyTeamContainer, _placementSettings);
        _context = bootstrapper.BuildContextFromGameManager(gameManager);

        StartBattle(_context);
    }

    /// <summary>
    /// Initializes and starts the battle.
    /// </summary>
    public void StartBattle(BattleContext context)
    {
        if (CurrentState != BattleState.NotStarted || !Multiplayer.IsServer()) return;

        _rng = new GodotRandomNumberGenerator();

        // Disable free movement for all combatants during battle.
        DisableMovement(context.PlayerParty);
        DisableMovement(context.EnemyCombatants);
        DisableMovement(context.AllyCombatants);

        if (context.Config.HasSeed)
        {
            _rng.SetSeed(context.Config.Seed);
        }
        
        var allCombatants = context.PlayerParty.Concat(context.EnemyCombatants).Concat(context.AllyCombatants).ToList();
        _actionDirector.Initialize(allCombatants);
        _actionDirector.SetRNG(_rng);

        if (_chargeSystem != null)
        {
            _chargeSystem.Initialize(_actionDirector.TimedHitManager);
        }

        _roster = new BattleRoster(_playerTeamContainer, _enemyTeamContainer, _allyTeamContainer, _turnManager, _actionDirector);
        _roster.CombatantDefeated += OnCombatantDefeated;

        _turnFlow = new BattleTurnFlow(_turnManager, _actionDirector, BattleCamera, _eventBus);
        _turnFlow.TurnStarted += (turn) => EmitSignal(SignalName.TurnStarted, turn);
        _networkGateway = new BattleNetworkGateway(this, () => _turnFlow.ActiveTurn);

        _roster.RegisterCombatants(
            context.PlayerParty,
            () => context.Config.Formation == BattleFormation.PlayerAdvantage
                ? _rng.RandRangeFloat(_advantageMinCounterStart, _advantageMaxCounterStart)
                : _rng.RandRangeFloat(_minCounterStart, _normalMaxCounterStart)
        );

        _roster.RegisterCombatants(
            context.EnemyCombatants,
            () => context.Config.Formation == BattleFormation.EnemyAdvantage
                ? _rng.RandRangeFloat(_advantageMinCounterStart, _advantageMaxCounterStart)
                : _rng.RandRangeFloat(_minCounterStart, _normalMaxCounterStart)
        );

        _roster.RegisterCombatants(
            context.AllyCombatants,
            () => _rng.RandRangeFloat(_minCounterStart, _normalMaxCounterStart)
        );

        // Apply initial status effects based on formation
        if (context.Config.Formation == BattleFormation.EnemyAdvantage)
        {
            // Apply "Back Turned" status to player party
            // foreach (var player in context.PlayerParty) { ... }
        }
        else if (context.Config.Formation == BattleFormation.PlayerAdvantage)
        {
            // Apply "Back Turned" status to enemy party
            // foreach (var enemy in context.EnemyCombatants) { ... }
        }

        // Now that the TurnManager is populated, initialize the UI.
        // The UI will pull the initial turn order.
        if (_turnOrderPreviewUI != null)
        {
            _turnOrderPreviewUI.Initialize(_turnManager);
        }

        CurrentState = BattleState.InProgress;
        EmitSignal(SignalName.BattleReady);
        _eventBus?.EmitSignal(GlobalEventBus.SignalName.BattleReady);
        TriggerAbilityBattleStart();
        ProcessNextTurn();
    }

    /// <summary>
    /// Called by the active combatant when they have chosen an action to perform.
    /// </summary>
    public async Task CommitAction(TurnManager.TurnData actor, ActionData action, List<Node> targets, ItemData sourceItem = null)
    {
        if (!Multiplayer.IsServer() || CurrentState != BattleState.InProgress) return;

        // 1. Resolve the action's effects (damage, healing, status effects, etc.)
        GD.Print($"'{actor.Combatant.Name}' performs action '{action.CommandName}'.");

        // Fallback: If no targets provided (e.g. AI placeholder), use default logic.
        if (targets == null || targets.Count == 0)
        {
            targets = new List<Node>();
            // Simple default: if player, target first enemy.
            if (IsPlayerSide(actor.Combatant))
            {
                if (_enemyTeamContainer.GetChildCount() > 0)
                    targets.Add(_enemyTeamContainer.GetChild(0));
            }
            else
            {
                if (_playerTeamContainer.GetChildCount() > 0)
                    targets.Add(_playerTeamContainer.GetChild(0));
            }
        }

        await _turnFlow.CommitAction(actor, action, targets, sourceItem);
    }

    private void OnCombatantDefeated(Node combatant)
    {
        GD.Print($"Combatant {combatant.Name} has been defeated!");

        // 1. Visuals
        // Get context from ActionDirector to see what killed them
        var killingContext = _actionDirector.CurrentContext;
        if (killingContext?.Initiator != null)
        {
            _lastKillingBlowActor = killingContext.Initiator;
        }

        _rewardTracker.RegisterDefeat(combatant, _roster != null ? _roster.IsPlayerSide : null);
        
        // Try to find BattleAnimator in scene if not directly linked (fallback)
        var animator = GetNodeOrNull<BattleAnimator>("BattleAnimator") ?? 
                       GetTree().Root.FindChild("BattleAnimator", true, false) as BattleAnimator;
        
        animator?.PlayDeathEffect(combatant, killingContext);

        // 2. Logic + turn management
        _roster.HandleCombatantDefeated(combatant);

        // 4. Triggers
        EmitSignal(SignalName.CombatantDefeated, combatant);

        // 5. Win/Loss Check
        CheckWinConditions();
    }

    /// <summary>
    /// Revives a defeated combatant, restoring them to the fight.
    /// Only works on Players or Persistent Enemies (e.g. Bosses).
    /// </summary>
    public void ReviveCombatant(Node combatant, int healAmount)
    {
        if (CurrentState != BattleState.InProgress) return;
        _roster.ReviveCombatant(combatant, healAmount);
    }

    private void CheckWinConditions()
    {
        bool playersAlive = false;
        bool enemiesAlive = false;

        // Check Players & Allies
        foreach (Node p in _playerTeamContainer.GetChildren())
        {
            if (_roster.IsCombatantAlive(p)) playersAlive = true;
        }
        foreach (Node a in _allyTeamContainer.GetChildren())
        {
            if (_roster.IsCombatantAlive(a)) playersAlive = true;
        }

        // Check Enemies
        foreach (Node e in _enemyTeamContainer.GetChildren())
        {
            if (_roster.IsCombatantAlive(e)) enemiesAlive = true;
        }

        if (!enemiesAlive)
        {
            EndBattle(BattleState.Victory);
        }
        else if (!playersAlive)
        {
            EndBattle(BattleState.Defeat);
        }
    }

    private void EndBattle(BattleState result)
    {
        if (CurrentState == BattleState.Victory || CurrentState == BattleState.Defeat) return;

        CurrentState = result;
        GD.Print($"Battle Ended: {result}");
        EmitSignal(SignalName.BattleEnded, (int)result);

        _ = HandleBattleEndAsync(result);
    }

    private async Task HandleBattleEndAsync(BattleState result)
    {
        if (_isResolvingBattleEnd) return;
        _isResolvingBattleEnd = true;

        var config = _context?.Config;
        bool allowRetry = config?.AllowRetry ?? true;
        bool isScriptedLoss = config?.IsScriptedLoss ?? false;

        if (result == BattleState.Victory)
        {
            await PlayVictoryCinematicAsync();

            var rewards = _rewardTracker.ResolveRewards(_rng);
            if (_resultsOverlay != null)
            {
                var gameManager = GetNodeOrNull<GameManager>(GameManager.Path);
                var inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
                _resultsOverlay.ShowVictory(rewards, () => _rewardsApplier.Apply(rewards, _context.PlayerParty, inventory, gameManager, _context?.Config, _lastKillingBlowActor), _context.PlayerParty);
                await _resultsOverlay.WaitForContinueAsync();
                await _resultsOverlay.FadeOutAsync();
            }

            ReturnToCaller(result, rewards, false);
        }
        else if (result == BattleState.Defeat)
        {
            if (isScriptedLoss)
            {
                ReturnToCaller(result, null, true);
                return;
            }

            if (_resultsOverlay != null)
            {
                _resultsOverlay.ShowDefeat(allowRetry);
                var choice = await _resultsOverlay.WaitForDefeatChoiceAsync();
                await _resultsOverlay.FadeOutAsync();

                if (choice == BattleResultsOverlay.DefeatChoice.Retry && allowRetry)
                {
                    RetryBattle();
                    return;
                }
            }

            _eventBus?.EmitSignal(GlobalEventBus.SignalName.BattleQuitRequested);
        }
    }

    private async Task PlayVictoryCinematicAsync()
    {
        if (BattleCamera == null) return;

        var focus = _lastKillingBlowActor as Node3D;
        if (focus == null && _context?.PlayerParty != null)
        {
            focus = _context.PlayerParty.OfType<Node3D>().FirstOrDefault();
        }

        if (focus != null)
        {
            BattleCamera.FocusOnTarget(focus);
        }

        BattleCamera.TriggerSweep(12.0f, 1.2f);
        await ToSignal(GetTree().CreateTimer(1.2f), SceneTreeTimer.SignalName.Timeout);
        BattleCamera.ResetToDefault();
    }

    private void ReturnToCaller(BattleState result, BattleRewards rewards, bool wasScriptedLoss)
    {
        var gameManager = GetNodeOrNull<GameManager>(GameManager.Path);
        gameManager?.ReturnFromBattle(result, rewards, wasScriptedLoss);
    }

    private void RetryBattle()
    {
        var gameManager = GetNodeOrNull<GameManager>(GameManager.Path);
        gameManager?.RestartBattleFromSnapshot();
    }

    

    /// <summary>
    /// Summons a new character into the battle.
    /// </summary>
    public void SummonCombatant(PackedScene characterScene, bool isPlayerSide)
    {
        _roster.SummonCombatant(characterScene, isPlayerSide);
    }

    private void ProcessNextTurn()
    {
        if (!_turnFlow.ProcessNextTurn())
        {
            CurrentState = BattleState.Defeat; // Or some error state
        }
    }

    private void TriggerAbilityBattleStart()
    {
        foreach (var member in EnumerateAllCombatants())
        {
            var abilityManager = member.GetNodeOrNull<AbilityManager>(AbilityManager.NodeName);
            if (abilityManager == null) continue;

            var perMemberContext = new AbilityEffectContext(member, AbilityTrigger.BattleStart)
            {
                BattleContext = _context,
                ActionDirector = _actionDirector
            };
            abilityManager.ApplyTrigger(AbilityTrigger.BattleStart, perMemberContext);
        }
    }

    private IEnumerable<Node> EnumerateAllCombatants()
    {
        foreach (Node node in _playerTeamContainer.GetChildren()) yield return node;
        foreach (Node node in _allyTeamContainer.GetChildren()) yield return node;
        foreach (Node node in _enemyTeamContainer.GetChildren()) yield return node;
    }

    /// <summary>
    /// RPC called by the client when they select an action from the menu.
    /// </summary>
    /// <param name="actionPath">The resource path of the selected ActionData/BattleCommand.</param>
    /// <param name="itemPath">Optional resource path of the selected ItemData (if the action came from an item).</param>
    /// <param name="targetPaths">An array of NodePaths for the selected targets.</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Server_PlayerCommitAction(string actionPath, string itemPath, string[] targetPaths)
    {
        if (!Multiplayer.IsServer()) return;
        if (!_networkGateway.TryBuildCommitRequest(actionPath, itemPath, targetPaths, out var currentTurn, out var actionResource, out var itemResource, out var targets))
        {
            return;
        }

        if (targets.Count == 0)
        {
            if (_roster.IsPlayerSide(currentTurn.Combatant))
            {
                if (_enemyTeamContainer.GetChildCount() > 0)
                    targets.Add(_enemyTeamContainer.GetChild(0));
            }
            else
            {
                if (_playerTeamContainer.GetChildCount() > 0)
                    targets.Add(_playerTeamContainer.GetChild(0));
            }
        }

        _ = CommitAction(currentTurn, actionResource, targets, itemResource);
    }

    private void DisableMovement(IEnumerable<Node> combatants)
    {
        foreach (var node in combatants)
        {
            var controller = node.GetNodeOrNull<PlayerController>(PlayerController.DefaultName);
            if (controller != null)
            {
                controller.IsMovementEnabled = false;
            }
        }
    }

    // --- AI Helper Methods ---

    public IEnumerable<Node> GetOpponents(Node character)
    {
        return _roster != null ? _roster.GetOpponents(character) : Enumerable.Empty<Node>();
    }

    public IEnumerable<Node> GetAllies(Node character)
    {
        return _roster != null ? _roster.GetAllies(character) : Enumerable.Empty<Node>();
    }

    public bool IsPlayerSide(Node character)
    {
        return _roster != null && _roster.IsPlayerSide(character);
    }
}
