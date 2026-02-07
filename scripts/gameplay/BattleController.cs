using Godot;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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

    [Export]
    private TurnOrderPreviewUI _turnOrderPreviewUI;

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

        // The BattleController is now responsible for setting up the battle
        // using the data prepared in the GameManager.
        CallDeferred(nameof(SetupBattleFromGameManager));
    }

    private void SetupBattleFromGameManager()
    {
        var gameManager = GetNode<GameManager>(GameManager.Path);

        // Spawn the enemy party from the PackedScenes stored in the GameManager.
        var allEnemyCombatants = new List<Node>();
        if (gameManager.PendingEnemyParties != null)
        {
            foreach (var enemyParty in gameManager.PendingEnemyParties)
            {
                foreach (var enemyScene in enemyParty)
                {
                    var enemyInstance = enemyScene.Instantiate();
                    _enemyTeamContainer.AddChild(enemyInstance);
                    allEnemyCombatants.Add(enemyInstance);
                }
            }
        }

        // Spawn the ally party.
        var allyCombatants = new List<Node>();
        if (gameManager.PendingAllyParty != null)
        {
            foreach (var allyScene in gameManager.PendingAllyParty)
            {
                var allyInstance = allyScene.Instantiate();
                _allyTeamContainer.AddChild(allyInstance);
                allyCombatants.Add(allyInstance);
            }
        }

        // Take the persistent player party from the GameManager and place them in the battle scene.
        foreach (var player in gameManager.PlayerParty)
        {
            player.GetParent()?.RemoveChild(player);
            _playerTeamContainer.AddChild(player);
        }
        var playerParty = gameManager.PlayerParty;

        // Use the assigned settings, or create a default one if missing to prevent crashes.
        if (_placementSettings == null) _placementSettings = new BattlePlacementSettings();
        _placementSettings.ApplyFormationPositions(playerParty, allEnemyCombatants, allyCombatants, gameManager.PendingFormation);

        // Apply Environment Profile if one was passed
        if (gameManager.PendingEnvironmentProfile != null)
        {
            // Try to find the generator in the scene root
            var sceneRoot = GetTree().CurrentScene;
            BattleArenaGenerator arenaGenerator = null;
            
            foreach (var child in sceneRoot.GetChildren())
            {
                if (child is BattleArenaGenerator gen)
                {
                    arenaGenerator = gen;
                    break;
                }
            }

            if (arenaGenerator != null)
            {
                arenaGenerator.ApplyProfile(gameManager.PendingEnvironmentProfile);
            }
        }

        StartBattle(playerParty, allEnemyCombatants, allyCombatants, gameManager.PendingFormation, gameManager.PendingBattleSeed);
    }

    /// <summary>
    /// Initializes and starts the battle.
    /// </summary>
    public void StartBattle(IEnumerable<Node> playerCombatants, IEnumerable<Node> enemyCombatants, IEnumerable<Node> allyCombatants, BattleFormation formation, ulong? seed = null)
    {
        if (CurrentState != BattleState.NotStarted || !Multiplayer.IsServer()) return;

        _rng = new GodotRandomNumberGenerator();

        // Disable free movement for all combatants during battle.
        DisableMovement(playerCombatants);
        DisableMovement(enemyCombatants);
        DisableMovement(allyCombatants);

        if (seed.HasValue)
        {
            _rng.SetSeed(seed.Value);
        }
        
        var allCombatants = playerCombatants.Concat(enemyCombatants).Concat(allyCombatants).ToList();
        _actionDirector.Initialize(allCombatants);
        _actionDirector.SetRNG(_rng);

        if (_chargeSystem != null)
        {
            _chargeSystem.Initialize(_actionDirector.TimedHitManager);
        }

        foreach (var combatant in playerCombatants)
        {
            EnsureBattleUnit(combatant);
            // If player has advantage, they start with a higher counter.
            float initialCounter = formation == BattleFormation.PlayerAdvantage ? 
            _rng.RandRangeFloat(_advantageMinCounterStart, _advantageMaxCounterStart) : _rng.RandRangeFloat(_minCounterStart, _normalMaxCounterStart);
            _turnManager.AddCombatant(combatant, initialCounter);
            ConnectCombatantSignals(combatant);
        }

        foreach (var combatant in enemyCombatants)
        {
            EnsureBattleUnit(combatant);
            // If enemy has advantage, they start with a higher counter.
            float initialCounter = formation == BattleFormation.EnemyAdvantage ? 
            _rng.RandRangeFloat(_advantageMinCounterStart, _advantageMaxCounterStart) : _rng.RandRangeFloat(_minCounterStart, _normalMaxCounterStart);
            _turnManager.AddCombatant(combatant, initialCounter);
            ConnectCombatantSignals(combatant);
        }

        foreach (var combatant in allyCombatants)
        {
            EnsureBattleUnit(combatant);
            float initialCounter = _rng.RandRangeFloat(_minCounterStart, _normalMaxCounterStart);
            _turnManager.AddCombatant(combatant, initialCounter);
            ConnectCombatantSignals(combatant);
        }

        // Apply initial status effects based on formation
        if (formation == BattleFormation.EnemyAdvantage)
        {
            // Apply "Back Turned" status to player party
            // foreach (var player in playerCombatants) { ... }
        }
        else if (formation == BattleFormation.PlayerAdvantage)
        {
            // Apply "Back Turned" status to enemy party
            // foreach (var enemy in enemyCombatants) { ... }
        }

        // Now that the TurnManager is populated, initialize the UI.
        // The UI will pull the initial turn order.
        if (_turnOrderPreviewUI != null)
        {
            _turnOrderPreviewUI.Initialize(_turnManager);
        }

        CurrentState = BattleState.InProgress;
        ProcessNextTurn();
    }

    private void EnsureBattleUnit(Node combatant)
    {
        if (combatant.GetNodeOrNull<BattleUnit>(BattleUnit.NodeName) == null)
        {
            var unit = new BattleUnit();
            unit.Name = BattleUnit.NodeName;
            combatant.AddChild(unit);
        }
    }

    private void ConnectCombatantSignals(Node combatant)
    {
        var stats = combatant.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats != null)
        {
            stats.HealthDepleted += () => OnCombatantHealthDepleted(combatant);
        }
    }

    /// <summary>
    /// Called by the active combatant when they have chosen an action to perform.
    /// </summary>
    public async void CommitAction(TurnManager.TurnData actor, ActionData action, List<Node> targets)
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

        var context = new ActionContext(action, actor.Combatant, targets);

        // Process via ActionDirector
        await _actionDirector.ProcessAction(context);

        // 2. Commit the turn to the TurnManager with the action's cost.
        _turnManager.CommitTurn(actor, action.TickCost, _actionDirector);

        // 3. Announce that the turn has been committed. The UI will react to this.
        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        eventBus.EmitSignal(GlobalEventBus.SignalName.TurnCommitted);

        // 4. Proceed to the next turn.
        ProcessNextTurn();
    }

    private void OnCombatantHealthDepleted(Node combatant)
    {
        GD.Print($"Combatant {combatant.Name} has been defeated!");

        // 1. Visuals
        // Get context from ActionDirector to see what killed them
        var killingContext = _actionDirector.CurrentContext;
        
        // Try to find BattleAnimator in scene if not directly linked (fallback)
        var animator = GetNodeOrNull<BattleAnimator>("BattleAnimator") ?? 
                       GetTree().Root.FindChild("BattleAnimator", true, false) as BattleAnimator;
        
        animator?.PlayDeathEffect(combatant, killingContext);

        // 2. Logic
        
        // Mark as untargetable/dead logic here if needed, though StatsComponent.IsDead handles the state.
        // We might want to disable collision or remove from targeting lists.
        if (combatant is CollisionObject3D col)
        {
            col.CollisionLayer = 0; // Disable collision
        }

        // 3. Turn Management
        var battleUnit = combatant.GetNodeOrNull<BattleUnit>(BattleUnit.NodeName);
        bool persist = battleUnit?.PersistAfterDeath ?? false;

        if (!persist)
        {
            var turnData = _turnManager.GetCombatants().FirstOrDefault(t => t.Combatant == combatant);
            if (turnData != null)
            {
                _turnManager.RemoveCombatant(turnData);
            }
            _actionDirector.RemoveCombatant(combatant);
            combatant.QueueFree();
        }

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

        var stats = combatant.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null || stats.CurrentHP > 0) return;

        // Policy Check: Only revive Players or Persistent Enemies.
        // Non-persistent enemies are considered permanently removed from the battle.
        bool isPlayer = IsPlayerSide(combatant);
        var battleUnit = combatant.GetNodeOrNull<BattleUnit>(BattleUnit.NodeName);
        bool persist = battleUnit?.PersistAfterDeath ?? false;

        if (!isPlayer && !persist)
        {
            GD.Print($"Revival failed: {combatant.Name} is permanently defeated.");
            return;
        }

        GD.Print($"Reviving {combatant.Name}...");

        // 1. Restore HP
        stats.ModifyCurrentHP(healAmount);

        // 2. Restore Collision/Targetability
        if (combatant is CollisionObject3D col) col.CollisionLayer = 1;

        // 3. Restore to Turn Manager if missing
        // (Players who don't persist are removed on death, so we must add them back)
        var turnData = _turnManager.GetCombatants().FirstOrDefault(t => t.Combatant == combatant);
        if (turnData == null) _turnManager.AddCombatant(combatant, 0);
    }

    private void CheckWinConditions()
    {
        bool playersAlive = false;
        bool enemiesAlive = false;

        // Check Players & Allies
        foreach (Node p in _playerTeamContainer.GetChildren())
        {
            if (IsCombatantAlive(p)) playersAlive = true;
        }
        foreach (Node a in _allyTeamContainer.GetChildren())
        {
            if (IsCombatantAlive(a)) playersAlive = true;
        }

        // Check Enemies
        foreach (Node e in _enemyTeamContainer.GetChildren())
        {
            if (IsCombatantAlive(e)) enemiesAlive = true;
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

    private bool IsCombatantAlive(Node combatant)
    {
        var battleUnit = combatant.GetNodeOrNull<BattleUnit>(BattleUnit.NodeName);
        if (battleUnit != null) return !battleUnit.IsDead;

        // Fallback to direct stats check if BattleUnit is missing
        var stats = combatant.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        return stats != null && stats.CurrentHP > 0;
    }

    private void EndBattle(BattleState result)
    {
        if (CurrentState == BattleState.Victory || CurrentState == BattleState.Defeat) return;

        CurrentState = result;
        GD.Print($"Battle Ended: {result}");
        EmitSignal(SignalName.BattleEnded, (int)result);

        // TODO: Show Results Screen, Distribute XP, etc.
    }

    /// <summary>
    /// Summons a new character into the battle.
    /// </summary>
    public void SummonCombatant(PackedScene characterScene, bool isPlayerSide)
    {
        if (characterScene == null) return;

        var instance = characterScene.Instantiate();
        Node container = isPlayerSide ? _allyTeamContainer : _enemyTeamContainer;
        
        container.AddChild(instance);
        
        // Initialize logic
        EnsureBattleUnit(instance);
        _actionDirector.RegisterCombatant(instance); // Assuming ActionDirector has a method to add runtime combatants
        ConnectCombatantSignals(instance);

        // Add to Turn Manager (start with 0 counter or inherit?)
        _turnManager.AddCombatant(instance, 0);

        // Visual entry
        if (instance is Node3D node3d)
        {
            // Optional: Play summon VFX
        }
        
        GD.Print($"Summoned {instance.Name} to {(isPlayerSide ? "Player" : "Enemy")} side.");
    }

    private void ProcessNextTurn()
    {
        var nextTurn = _turnManager.GetNextTurn();
        if (nextTurn == null)
        {
            GD.PrintErr("Battle ended because no combatants could take a turn.");
            CurrentState = BattleState.Defeat; // Or some error state
            return;
        }

        // Check if the combatant is Stopped/Blocked
        if (nextTurn.IsBlocked)
        {
            GD.Print($"{nextTurn.Combatant.Name} is stopped! Skipping turn.");
            // Commit the turn immediately with 0 extra cost.
            // This triggers OnTurnEnd, which ticks down the Stop effect.
            _turnManager.CommitTurn(nextTurn, 0, _actionDirector);
            return; // CommitTurn calls ProcessNextTurn, so we return here to avoid recursion issues or double processing.
        }

        // Announce whose turn it is.
        EmitSignal(SignalName.TurnStarted, nextTurn);

        if (BattleCamera != null && nextTurn.Combatant is Node3D combatant3D)
        {
            BattleCamera.FocusOnTarget(combatant3D);
        }

        // If the active combatant is AI, trigger its logic.
        // If it's a human player, the server will now wait for an RPC from that player's client.
        // We will implement this player input logic next.
    }

        /// <summary>
    /// RPC called by the client when they select an action from the menu.
    /// </summary>
    /// <param name="actionPath">The resource path of the selected ActionData/BattleCommand.</param>
    /// <param name="targetPaths">An array of NodePaths for the selected targets.</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Server_PlayerCommitAction(string actionPath, string[] targetPaths)
    {
        if (!Multiplayer.IsServer()) return;

        // 1. Validate it is actually a player's turn
        var currentTurn = _turnManager.GetNextTurn();
        if (currentTurn == null) return;

        // Security Check: Ensure the RPC sender owns the active character
        var senderId = Multiplayer.GetRemoteSenderId();
        if (senderId == 0) senderId = 1; // Handle local call from host
        if (currentTurn.Combatant.GetMultiplayerAuthority() != senderId)
        {
            GD.PrintErr($"Player {senderId} tried to act, but it is {currentTurn.Combatant.Name}'s turn (Owner: {currentTurn.Combatant.GetMultiplayerAuthority()})");
            return;
        }

        // 2. Load the action resource
        var actionResource = GD.Load<ActionData>(actionPath);
        if (actionResource == null)
        {
            GD.PrintErr($"Server could not load action from path: {actionPath}");
            return;
        }

        // 3. Resolve Targets
        var targets = new List<Node>();
        if (targetPaths != null)
        {
            foreach (var path in targetPaths)
            {
                var node = GetNodeOrNull(path);
                if (node != null) targets.Add(node);
            }
        }

        // 4. Commit
        CommitAction(currentTurn, actionResource, targets);
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
        if (IsPlayerSide(character))
        {
            return _enemyTeamContainer.GetChildren().Cast<Node>();
        }
        // If enemy, opponents are players + allies
        return _playerTeamContainer.GetChildren().Cast<Node>().Concat(_allyTeamContainer.GetChildren().Cast<Node>());
    }

    public IEnumerable<Node> GetAllies(Node character)
    {
        if (IsPlayerSide(character))
        {
            return _playerTeamContainer.GetChildren().Cast<Node>().Concat(_allyTeamContainer.GetChildren().Cast<Node>());
        }
        // If enemy, allies are other enemies
        return _enemyTeamContainer.GetChildren().Cast<Node>();
    }

    public bool IsPlayerSide(Node character)
    {
        var parent = character.GetParent();
        return parent == _playerTeamContainer || parent == _allyTeamContainer;
    }
}