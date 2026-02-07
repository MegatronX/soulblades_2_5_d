using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A component that drives a character via AI logic.
/// Takes precedence over PlayerController if present and not suppressed.
/// </summary>
[GlobalClass]
public partial class AIController : Node
{
    public const string DefaultName = "AIController";

    // Static list for debuggers to find all active AI
    public static readonly List<AIController> ActiveControllers = new();

    /// <summary>
    /// If true, the AI is suppressed (e.g. by a player taking control via an ability),
    /// allowing the PlayerController to function if present.
    /// </summary>
    [Export]
    public bool IsSuppressed { get; set; } = false;

    [Export]
    public AIStrategy Strategy { get; set; }

    // Generic memory storage for strategies (e.g. "CurrentPhase", "ThreatTable")
    public Dictionary<string, Variant> Memory { get; private set; } = new();

    // Map of Opponent Instance ID -> Threat Value
    private Dictionary<ulong, float> _threatTable = new();

    // Map of Opponent Instance ID -> Set of ineffective Action Categories/Tags (e.g. "Slow", "Fire")
    private Dictionary<ulong, HashSet<string>> _immunityMemory = new();

    private BattleController _battleController;
    private ActionManager _actionManager;

    public override void _EnterTree()
    {
        ActiveControllers.Add(this);
    }

    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            SetProcess(false);
            return;
        }

        _battleController = GetTree().Root.FindChild("BattleController", true, false) as BattleController;
        if (_battleController != null)
        {
            _battleController.TurnStarted += OnTurnStarted;
        }
        
        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        this.Subscribe(
            () => eventBus.ActionExecuted += OnActionExecuted,
            () => eventBus.ActionExecuted -= OnActionExecuted
        );

        _actionManager = GetParent().GetNodeOrNull<ActionManager>(ActionManager.DefaultName);
    }

    public override void _ExitTree()
    {
        ActiveControllers.Remove(this);
        if (_battleController != null)
        {
            _battleController.TurnStarted -= OnTurnStarted;
        }
    }

    private async void OnTurnStarted(TurnManager.TurnData turnData)
    {
        if (IsSuppressed || turnData.Combatant != GetParent()) return;
        if (Strategy == null)
        {
            GD.PrintErr($"AIController on {GetParent().Name} has no strategy assigned!");
            // Skip turn to prevent softlock
            // In a real scenario, maybe default to a basic attack or wait.
            return;
        }

        // Add a small "thinking" delay for pacing
        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

        var decision = Strategy.GetDecision(this, GetParent(), _battleController);

        if (decision != null && decision.IsValid)
        {
            await _battleController.CommitAction(turnData, decision.Action, decision.Targets);
        }
        else
        {
            GD.Print($"AI {GetParent().Name} could not make a valid decision. Skipping turn.");
            // Fallback: Wait/Skip
            // _battleController.CommitAction(turnData, WaitAction, ...);
        }
    }

    public ActionManager GetActionManager() => _actionManager;

    // Helper to access memory safely
    public T GetMemory<[MustBeVariant] T>(string key, T defaultValue = default)
    {
        return Memory.TryGetValue(key, out var value) ? value.As<T>() : defaultValue;
    }

    // --- Debug Accessors ---
    public IReadOnlyDictionary<ulong, float> Debug_GetThreatTable() => _threatTable;
    public IReadOnlyDictionary<ulong, HashSet<string>> Debug_GetImmunityMemory() => _immunityMemory;

    public float GetThreat(Node target)
    {
        if (target == null) return 0f;
        return _threatTable.GetValueOrDefault(target.GetInstanceId(), 0f);
    }

    public void AddThreat(Node source, float amount)
    {
        if (source == null) return;
        ulong id = source.GetInstanceId();
        if (!_threatTable.ContainsKey(id)) _threatTable[id] = 0f;
        _threatTable[id] += amount;
        
        // Decay over time? Clamp? For now just accumulate.
    }

    public void RecordImmunity(Node target, string tag)
    {
        if (target == null || string.IsNullOrEmpty(tag)) return;
        ulong id = target.GetInstanceId();
        if (!_immunityMemory.ContainsKey(id)) _immunityMemory[id] = new HashSet<string>();
        _immunityMemory[id].Add(tag);
    }

    public bool IsImmune(Node target, string tag)
    {
        if (target == null) return false;
        return _immunityMemory.TryGetValue(target.GetInstanceId(), out var tags) && tags.Contains(tag);
    }

    private void OnActionExecuted(ActionContext context)
    {
        if (IsSuppressed || _battleController == null) return;
        var myChar = GetParent();
        
        // Cache lists for checks
        var opponents = _battleController.GetOpponents(myChar).ToList();
        var myAllies = _battleController.GetAllies(myChar).ToList();

        bool isOpponentAction = opponents.Contains(context.Initiator);
        bool isMySideAction = context.Initiator == myChar || myAllies.Contains(context.Initiator);

        if (!isOpponentAction && !isMySideAction) return;

        float threatGenerated = 0f;

        foreach (var kvp in context.Results)
        {
            var target = kvp.Key;
            var result = kvp.Value;
            
            // 1. Threat Logic (When opponents act against us)
            if (isOpponentAction)
            {
                bool isMySideTarget = target == myChar || myAllies.Contains(target);

                if (isMySideTarget)
                {
                    threatGenerated += result.FinalDamage; // Damage generates threat directly
                }
                else if (context.SourceAction.Category == "Heal")
                {
                    // If they healed themselves/allies, that generates threat!
                    threatGenerated += 20f; 
                }
            }

            // 2. Learning Logic (When we act against opponents)
            if (isMySideAction && opponents.Contains(target))
            {
                // Check for "Resist" or "Immune" in the result tags or log
                if (result.AnimationTags.Contains("Immune") || result.AnimationTags.Contains("Resist"))
                {
                    // Remember that this action category is bad against this target
                    RecordImmunity(target, context.SourceAction.Category);
                }
            }
        }
        
        if (threatGenerated > 0 && isOpponentAction)
        {
            AddThreat(context.Initiator, threatGenerated);
            Memory["LastAttackerId"] = context.Initiator.GetInstanceId();
        }
    }
}
