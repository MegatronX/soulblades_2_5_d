using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class BattlePartyVisualizer : Control
{
    [Export] private NodePath _playerTeamContainerPath;
    [Export] private NodePath _rowContainerPath;
    [Export] private PackedScene _rowScene;
    [Export] private int _maxRows = 4;
    [Export] private int _rowSpacingDefault = 10;
    [Export] private int _rowSpacingFour = 6;
    [Export(PropertyHint.Range, "0.5,1.0,0.01")]
    private float _fourRowScale = 0.9f;

    private Node _playerTeamContainer;
    private BoxContainer _rowContainer;

    [Export]
    private BattleController _battleController;

    [Export]
    private ChargeSystem _chargeSystem;

    [Export]
    private OverflowSystem _overflowSystem;

    private readonly Dictionary<Node, BattlePartyStatusRow> _rowsByMember = new();
    private bool _partyBindingsInitialized;

    public override void _Ready()
    {
        _battleController = _battleController ?? GetTree().Root.FindChild("BattleController", true, false) as BattleController;
        _chargeSystem = _chargeSystem ?? _battleController?.ChargeSystem;
        _overflowSystem = _overflowSystem ?? _battleController?.OverflowSystem;

        _rowContainer = GetNodeOrNull<BoxContainer>(_rowContainerPath);

        if (_rowContainer == null)
        {
            GD.PrintErr("BattlePartyVisualizer: RowContainer path not set.");
            return;
        }

        SetMouseFilterRecursive(this, MouseFilterEnum.Ignore);

        if (_battleController != null)
        {
            this.Subscribe(
                () => _battleController.CombatantDefeated += OnCombatantDefeated,
                () => _battleController.CombatantDefeated -= OnCombatantDefeated
            );
            this.Subscribe(
                () => _battleController.TurnStarted += OnTurnStarted,
                () => _battleController.TurnStarted -= OnTurnStarted
            );

            if (_battleController.CurrentState == BattleController.BattleState.InProgress)
            {
                InitializePartyBindings();
            }
        }

        if (_overflowSystem != null)
        {
            this.Subscribe(
                () => _overflowSystem.OverflowChanged += OnOverflowChanged,
                () => _overflowSystem.OverflowChanged -= OnOverflowChanged
            );
        }

        var eventBus = GetNodeOrNull<GlobalEventBus>(GlobalEventBus.Path);
        if (eventBus != null)
        {
            this.Subscribe(
                () => eventBus.BattleReady += OnBattleReady,
                () => eventBus.BattleReady -= OnBattleReady
            );
        }
    }

    private void OnBattleReady()
    {
        InitializePartyBindings();
        UpdatePlayerOverflowGauge(immediate: true);
    }

    private void InitializePartyBindings()
    {
        if (_partyBindingsInitialized) return;

        _playerTeamContainer = GetNodeOrNull<Node>(_playerTeamContainerPath)
            ?? GetTree().Root.FindChild("PlayerTeamContainer", true, false);

        if (_playerTeamContainer == null)
        {
            GD.PrintErr("BattlePartyVisualizer: PlayerTeamContainer not found.");
            return;
        }

        this.Subscribe(
            () => _playerTeamContainer.ChildEnteredTree += OnPartyMemberAdded,
            () => _playerTeamContainer.ChildEnteredTree -= OnPartyMemberAdded
        );
        this.Subscribe(
            () => _playerTeamContainer.ChildExitingTree += OnPartyMemberRemoved,
            () => _playerTeamContainer.ChildExitingTree -= OnPartyMemberRemoved
        );

        BuildRows();
        _partyBindingsInitialized = true;
    }

    public void BuildRows()
    {
        ClearRows();

        if (_playerTeamContainer == null) return;

        var members = _playerTeamContainer.GetChildren().OfType<Node>()
            .Where(n => n.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName) != null)
            .Take(_maxRows)
            .ToList();

        foreach (var member in members)
        {
            AddRowForMember(member);
        }

        AdjustRowSizing();
        UpdatePlayerOverflowGauge(immediate: true);
        SetMouseFilterRecursive(this, MouseFilterEnum.Ignore);
    }

    private BattlePartyStatusRow CreateRow()
    {
        if (_rowScene == null)
        {
            GD.PrintErr("BattlePartyVisualizer: RowScene not set.");
            return null;
        }

        var node = _rowScene.Instantiate();
        if (node is not BattlePartyStatusRow row)
        {
            GD.PrintErr("BattlePartyVisualizer: RowScene root must be BattlePartyStatusRow.");
            node.QueueFree();
            return null;
        }

        _rowContainer.AddChild(row);
        return row;
    }

    private void ClearRows()
    {
        foreach (var row in _rowsByMember.Values)
        {
            row?.Unbind();
            row?.QueueFree();
        }
        _rowsByMember.Clear();

        if (_rowContainer != null)
        {
            foreach (var child in _rowContainer.GetChildren())
            {
                child.QueueFree();
            }
        }
    }

    private void OnCombatantDefeated(Node combatant)
    {
        if (combatant == null) return;
        if (_rowsByMember.TryGetValue(combatant, out var row))
        {
            row.SetDefeated(true);
        }
        AdjustRowSizing();
    }

    private void OnPartyMemberAdded(Node node)
    {
        if (node == null) return;
        if (node.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName) == null) return;
        AddRowForMember(node);
        AdjustRowSizing();
    }

    private void OnPartyMemberRemoved(Node node)
    {
        if (node == null) return;
        if (_rowsByMember.TryGetValue(node, out var row))
        {
            row.Unbind();
            row.QueueFree();
            _rowsByMember.Remove(node);
        }
        AdjustRowSizing();
    }

    private void OnTurnStarted(TurnManager.TurnData turnData)
    {
        var active = turnData?.Combatant;
        foreach (var kvp in _rowsByMember)
        {
            kvp.Value?.SetActive(kvp.Key == active);
        }
    }

    private void AdjustRowSizing()
    {
        if (_rowContainer == null) return;

        int count = _rowsByMember.Count;
        float scale = count >= 4 ? _fourRowScale : 1.0f;
        int spacing = count >= 4 ? _rowSpacingFour : _rowSpacingDefault;
        _rowContainer.AddThemeConstantOverride("separation", spacing);

        foreach (var row in _rowsByMember.Values)
        {
            if (row == null) continue;
            row.Scale = new Vector2(scale, scale);
        }
    }

    private void AddRowForMember(Node member)
    {
        if (member == null) return;
        if (_rowsByMember.ContainsKey(member)) return;
        if (_rowsByMember.Count >= _maxRows) return;

        var row = CreateRow();
        if (row == null) return;

        row.Bind(member, _chargeSystem);
        _rowsByMember[member] = row;
        UpdatePlayerOverflowGauge(immediate: true);
    }

    private void OnOverflowChanged(long side, int currentValue, int maxValue, int delta, string reason)
    {
        if ((OverflowPartySide)side != OverflowPartySide.Player) return;

        foreach (var row in _rowsByMember.Values)
        {
            row?.SetLimitGauge(currentValue, maxValue);
        }
    }

    private void UpdatePlayerOverflowGauge(bool immediate = false)
    {
        if (_overflowSystem == null) return;

        int current = _overflowSystem.GetOverflowForSide(OverflowPartySide.Player);
        int max = _overflowSystem.GetOverflowCapForSide(OverflowPartySide.Player);
        foreach (var row in _rowsByMember.Values)
        {
            row?.SetLimitGauge(current, max, immediate);
        }
    }

    private static void SetMouseFilterRecursive(Node node, Control.MouseFilterEnum filter)
    {
        if (node is Control control)
        {
            control.MouseFilter = filter;
        }

        foreach (var child in node.GetChildren())
        {
            SetMouseFilterRecursive(child, filter);
        }
    }
}
