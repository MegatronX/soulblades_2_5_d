using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Shared party Overflow meter manager. Tracks generation, spend, and per-round rules.
/// </summary>
[GlobalClass]
public partial class OverflowSystem : Node
{
    [Signal]
    public delegate void OverflowChangedEventHandler(long side, int currentValue, int maxValue, int delta, string reason);

    [Signal]
    public delegate void SpendRejectedEventHandler(Node actor, string reason);

    [Signal]
    public delegate void RoundResolvedEventHandler(int roundIndex);

    [Export]
    public OverflowConfig Config { get; set; }

    private sealed class PartyState
    {
        public int Current = 0;
        public int Cap = 1000;
        public int UtilitiesSpentThisRound = 0;
        public int FinishersSpentThisRound = 0;
        public bool SpentAnyThisRound = false;
        public Dictionary<string, int> RuleTriggerCounts { get; } = new();
        public Dictionary<string, int> RuleGainTotals { get; } = new();
    }

    private readonly Dictionary<OverflowPartySide, PartyState> _states = new();
    private readonly Dictionary<OverflowTriggerType, List<OverflowGenerationRule>> _rulesByTrigger = new();

    private BattleController _battleController;
    private TimedHitManager _timedHitManager;
    private bool _isBattleActive = false;

    private int _roundIndex = 1;
    private int _turnsTakenInRound = 0;
    private int _turnsPerRound = 1;

    public override void _Ready()
    {
        Config ??= new OverflowConfig();
        EnsureStates();
        RebuildRules();
    }

    public override void _ExitTree()
    {
        if (_timedHitManager != null)
        {
            _timedHitManager.TimedHitResolved -= OnTimedHitResolved;
        }
    }

    public void Initialize(BattleController battleController, TimedHitManager timedHitManager)
    {
        if (_timedHitManager != null)
        {
            _timedHitManager.TimedHitResolved -= OnTimedHitResolved;
        }

        _battleController = battleController;
        _timedHitManager = timedHitManager;
        if (_timedHitManager != null)
        {
            _timedHitManager.TimedHitResolved += OnTimedHitResolved;
        }

        RebuildRules();
    }

    public void ResetForBattle(IEnumerable<Node> combatants)
    {
        EnsureStates();
        Config ??= new OverflowConfig();
        RebuildRules();

        int cap = Mathf.Max(1, Config.ResolveCap());
        foreach (var side in _states.Keys)
        {
            var state = _states[side];
            state.Cap = cap;
            if (Config.ResetOnBattleStart)
            {
                state.Current = 0;
            }

            ResetRoundState(state);
            EmitOverflowChanged(side, 0, "BattleStart");
        }

        _roundIndex = 1;
        _turnsTakenInRound = 0;
        _turnsPerRound = ResolveRoundTurnTarget(combatants);
        _isBattleActive = true;
    }

    public void EndBattle()
    {
        _isBattleActive = false;
    }

    public int GetOverflow(Node actor)
    {
        return GetState(ResolveSide(actor)).Current;
    }

    public int GetOverflowCap(Node actor)
    {
        return GetState(ResolveSide(actor)).Cap;
    }

    public int GetOverflowForSide(OverflowPartySide side)
    {
        return GetState(side).Current;
    }

    public int GetOverflowCapForSide(OverflowPartySide side)
    {
        return GetState(side).Cap;
    }

    public bool CanAffordAction(Node actor, ActionData action, out string reason)
    {
        reason = string.Empty;
        var cost = GetOverflowCost(action);
        if (cost == null || cost.Cost <= 0) return true;

        return CanSpend(actor, cost.Cost, cost.SpendType, cost.IgnorePerRoundSpendLimits, out reason);
    }

    public bool TrySpendForAction(Node actor, ActionData action, out string reason)
    {
        reason = string.Empty;
        var cost = GetOverflowCost(action);
        if (cost == null || cost.Cost <= 0) return true;

        var spendReason = string.IsNullOrEmpty(cost.SpendReason)
            ? action?.CommandName ?? "OverflowAction"
            : cost.SpendReason;

        return TrySpend(actor, cost.Cost, cost.SpendType, spendReason, cost.IgnorePerRoundSpendLimits, out reason);
    }

    public bool TrySpend(Node actor, int amount, OverflowSpendType spendType, string reason, bool ignorePerRoundSpendLimits, out string rejectionReason)
    {
        rejectionReason = string.Empty;
        if (amount <= 0) return true;

        if (!CanSpend(actor, amount, spendType, ignorePerRoundSpendLimits, out rejectionReason))
        {
            EmitSignal(SignalName.SpendRejected, actor, rejectionReason);
            return false;
        }

        var side = ResolveSide(actor);
        var state = GetState(side);
        int before = state.Current;
        state.Current = Mathf.Max(0, state.Current - amount);
        int delta = state.Current - before;

        state.SpentAnyThisRound = true;
        if (!ignorePerRoundSpendLimits)
        {
            if (spendType == OverflowSpendType.Finisher)
            {
                state.FinishersSpentThisRound++;
            }
            else
            {
                state.UtilitiesSpentThisRound++;
            }
        }

        EmitOverflowChanged(side, delta, reason);
        return true;
    }

    public int AddOverflow(Node source, int amount, string reason = "Manual")
    {
        if (source == null || amount <= 0) return 0;
        return AddOverflowInternal(ResolveSide(source), amount, reason);
    }

    public void ReportPerfectTimedGuard(Node source)
    {
        if (source == null) return;
        ReportEvent(new OverflowEventData
        {
            TriggerType = OverflowTriggerType.PerfectTimedGuard,
            Source = source,
            Amount = 1f,
            Reason = "PerfectTimedGuard"
        });
    }

    public void ReportOverheal(Node source, Node target, int overheal)
    {
        if (source == null || overheal <= 0) return;
        ReportEvent(new OverflowEventData
        {
            TriggerType = OverflowTriggerType.Overheal,
            Source = source,
            Target = target,
            Amount = overheal,
            Reason = "Overheal"
        });
    }

    public void ReportMpRestored(Node source, Node target, int mpRestored)
    {
        if (source == null || mpRestored <= 0) return;
        ReportEvent(new OverflowEventData
        {
            TriggerType = OverflowTriggerType.MpRestored,
            Source = source,
            Target = target,
            Amount = mpRestored,
            Reason = "MpRestore"
        });
    }

    public void ReportUniqueDebuffApplied(Node source, Node target, StatusEffect effect)
    {
        if (source == null || effect == null) return;
        if (effect.Polarity != StatusEffectPolarity.Negative) return;
        ReportEvent(new OverflowEventData
        {
            TriggerType = OverflowTriggerType.UniqueDebuffApplied,
            Source = source,
            Target = target,
            Amount = 1f,
            IsUnique = true,
            Reason = "UniqueDebuff"
        });
    }

    public void ReportEvent(OverflowEventData overflowEvent)
    {
        if (!_isBattleActive) return;
        if (overflowEvent == null || overflowEvent.Source == null) return;
        if (!_rulesByTrigger.TryGetValue(overflowEvent.TriggerType, out var rules) || rules.Count == 0) return;

        var side = ResolveSide(overflowEvent.Source);
        var state = GetState(side);

        foreach (var rule in rules)
        {
            if (rule == null) continue;
            if (rule.TriggerType != overflowEvent.TriggerType) continue;
            if (!HasRequiredAbility(overflowEvent.Source, rule)) continue;

            string ruleKey = rule.ResolveKey();
            int triggerCount = state.RuleTriggerCounts.GetValueOrDefault(ruleKey, 0);
            if (rule.PerRoundTriggerLimit >= 0 && triggerCount >= rule.PerRoundTriggerLimit)
            {
                continue;
            }

            int gain = rule.ResolveGain(overflowEvent.Amount);
            if (gain <= 0) continue;

            int gainedSoFar = state.RuleGainTotals.GetValueOrDefault(ruleKey, 0);
            if (rule.PerRoundOverflowGainCap >= 0)
            {
                int remaining = rule.PerRoundOverflowGainCap - gainedSoFar;
                if (remaining <= 0) continue;
                gain = Mathf.Min(gain, remaining);
            }

            if (gain <= 0) continue;

            int applied = AddOverflowInternal(side, gain, ResolveGainReason(rule, overflowEvent));
            if (applied <= 0) continue;

            state.RuleTriggerCounts[ruleKey] = triggerCount + 1;
            state.RuleGainTotals[ruleKey] = gainedSoFar + applied;
        }
    }

    public void NotifyTurnCommitted(Node actor)
    {
        if (!_isBattleActive) return;

        _turnsTakenInRound++;
        if (_turnsTakenInRound < _turnsPerRound)
        {
            return;
        }

        ResolveRoundEnd();
        _roundIndex++;
        _turnsTakenInRound = 0;
        _turnsPerRound = Mathf.Max(1, _battleController?.GetLivingCombatantCount() ?? _turnsPerRound);
    }

    private bool CanSpend(Node actor, int amount, OverflowSpendType spendType, bool ignorePerRoundSpendLimits, out string reason)
    {
        reason = string.Empty;
        if (amount <= 0) return true;

        var state = GetState(ResolveSide(actor));
        if (state.Current < amount)
        {
            reason = $"Not enough Overflow ({state.Current}/{amount}).";
            return false;
        }

        if (ignorePerRoundSpendLimits) return true;

        if (spendType == OverflowSpendType.Finisher)
        {
            if (Config.MaxFinishersPerRound >= 0 && state.FinishersSpentThisRound >= Config.MaxFinishersPerRound)
            {
                reason = "Finisher limit reached this round.";
                return false;
            }
        }
        else
        {
            if (Config.MaxUtilitiesPerRound >= 0 && state.UtilitiesSpentThisRound >= Config.MaxUtilitiesPerRound)
            {
                reason = "Utility limit reached this round.";
                return false;
            }
        }

        return true;
    }

    private int AddOverflowInternal(OverflowPartySide side, int amount, string reason)
    {
        if (amount <= 0) return 0;

        var state = GetState(side);
        int before = state.Current;
        state.Current = Mathf.Clamp(state.Current + amount, 0, state.Cap);
        int delta = state.Current - before;
        if (delta != 0)
        {
            EmitOverflowChanged(side, delta, reason);
        }
        return delta;
    }

    private void ResolveRoundEnd()
    {
        foreach (var side in _states.Keys)
        {
            var state = _states[side];

            if (!state.SpentAnyThisRound && state.Current > 0 && Config.EndOfRoundDecayPercent > 0f)
            {
                int decay = Mathf.FloorToInt(state.Current * Config.EndOfRoundDecayPercent);
                if (decay > 0)
                {
                    int before = state.Current;
                    state.Current = Mathf.Max(0, state.Current - decay);
                    EmitOverflowChanged(side, state.Current - before, "RoundDecay");
                }
            }

            ResetRoundState(state);
        }

        EmitSignal(SignalName.RoundResolved, _roundIndex);
    }

    private void ResetRoundState(PartyState state)
    {
        if (state == null) return;
        state.UtilitiesSpentThisRound = 0;
        state.FinishersSpentThisRound = 0;
        state.SpentAnyThisRound = false;
        state.RuleTriggerCounts.Clear();
        state.RuleGainTotals.Clear();
    }

    private int ResolveRoundTurnTarget(IEnumerable<Node> combatants)
    {
        if (combatants != null)
        {
            int count = 0;
            foreach (var node in combatants)
            {
                if (!GodotObject.IsInstanceValid(node)) continue;
                count++;
            }
            if (count > 0) return count;
        }

        return Mathf.Max(1, _battleController?.GetLivingCombatantCount() ?? 1);
    }

    private void RebuildRules()
    {
        _rulesByTrigger.Clear();
        Config ??= new OverflowConfig();

        if (Config.IncludeBaselineTimedRules)
        {
            AddRule(new OverflowGenerationRule
            {
                RuleId = "overflow.builtin.perfect_hit",
                TriggerType = OverflowTriggerType.PerfectTimedHit,
                FlatAmount = Config.PerfectTimedHitGain,
                PerRoundTriggerLimit = Config.PerfectTimedHitTriggerLimitPerRound,
                GainReason = "PerfectTimedHit"
            });

            AddRule(new OverflowGenerationRule
            {
                RuleId = "overflow.builtin.perfect_guard",
                TriggerType = OverflowTriggerType.PerfectTimedGuard,
                FlatAmount = Config.PerfectTimedGuardGain,
                PerRoundTriggerLimit = Config.PerfectTimedGuardTriggerLimitPerRound,
                GainReason = "PerfectTimedGuard"
            });
        }

        if (Config.GenerationRules == null) return;
        foreach (var rule in Config.GenerationRules)
        {
            AddRule(rule);
        }
    }

    private void AddRule(OverflowGenerationRule rule)
    {
        if (rule == null || rule.TriggerType == OverflowTriggerType.None) return;

        if (!_rulesByTrigger.TryGetValue(rule.TriggerType, out var list))
        {
            list = new List<OverflowGenerationRule>();
            _rulesByTrigger[rule.TriggerType] = list;
        }

        list.Add(rule);
    }

    private bool HasRequiredAbility(Node owner, OverflowGenerationRule rule)
    {
        if (rule == null || rule.RequiredEquippedAbility == null) return true;
        if (owner == null) return false;

        var abilityManager = owner.GetNodeOrNull<AbilityManager>(AbilityManager.NodeName);
        if (abilityManager == null) return false;
        return abilityManager.GetEquippedAbilities().Contains(rule.RequiredEquippedAbility);
    }

    private static string ResolveGainReason(OverflowGenerationRule rule, OverflowEventData overflowEvent)
    {
        if (rule != null && !string.IsNullOrEmpty(rule.GainReason))
        {
            return rule.GainReason;
        }

        if (overflowEvent != null && !string.IsNullOrEmpty(overflowEvent.Reason))
        {
            return overflowEvent.Reason;
        }

        return "OverflowGain";
    }

    private void EnsureStates()
    {
        if (!_states.ContainsKey(OverflowPartySide.Player))
        {
            _states[OverflowPartySide.Player] = new PartyState();
        }

        if (!_states.ContainsKey(OverflowPartySide.Enemy))
        {
            _states[OverflowPartySide.Enemy] = new PartyState();
        }
    }

    private PartyState GetState(OverflowPartySide side)
    {
        EnsureStates();
        return _states[side];
    }

    private OverflowPartySide ResolveSide(Node actor)
    {
        if (_battleController != null && actor != null)
        {
            return _battleController.IsPlayerSide(actor) ? OverflowPartySide.Player : OverflowPartySide.Enemy;
        }

        bool playerSide = actor?.IsInGroup(GameGroups.PlayerCharacters) ?? true;
        return playerSide ? OverflowPartySide.Player : OverflowPartySide.Enemy;
    }

    private void EmitOverflowChanged(OverflowPartySide side, int delta, string reason)
    {
        var state = GetState(side);
        EmitSignal(SignalName.OverflowChanged, (long)side, state.Current, state.Cap, delta, reason ?? string.Empty);
    }

    private void OnTimedHitResolved(TimedHitRating rating, ActionContext context, TimedHitSettings settings)
    {
        if (rating != TimedHitRating.Perfect) return;
        if (context?.Initiator == null) return;

        ReportEvent(new OverflowEventData
        {
            TriggerType = OverflowTriggerType.PerfectTimedHit,
            Source = context.Initiator,
            Target = context.CurrentTarget,
            Amount = 1f,
            Reason = "PerfectTimedHit"
        });
    }

    public static OverflowCostComponentData GetOverflowCost(ActionData action)
    {
        if (action?.Components == null) return null;
        foreach (var component in action.Components)
        {
            if (component is OverflowCostComponentData overflowCost && overflowCost.Cost > 0)
            {
                return overflowCost;
            }
        }

        return null;
    }
}
