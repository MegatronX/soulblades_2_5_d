using Godot;
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Debug overlay that displays full stat breakdowns for all active battle combatants.
/// Toggle with F3.
/// </summary>
public partial class BattleStatsDebugOverlay : CanvasLayer
{
    [Export]
    public Key ToggleKey { get; set; } = Key.F3;

    [Export]
    public int OverlayLayer { get; set; } = 1000;

    [Export]
    public int ContentFontSize { get; set; } = 18;

    private BattleController _battleController;
    private Control _root;
    private Label _contentLabel;

    public void Initialize(BattleController battleController)
    {
        _battleController = battleController;
    }

    public override void _Ready()
    {
        Layer = OverlayLayer;

        _root = new Control();
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_root);

        var panel = new PanelContainer();
        panel.AnchorLeft = 1;
        panel.AnchorRight = 1;
        panel.AnchorTop = 0;
        panel.AnchorBottom = 1;
        panel.OffsetLeft = -620;
        panel.OffsetRight = -10;
        panel.OffsetTop = 10;
        panel.OffsetBottom = -10;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0.55f)
        };
        panel.AddThemeStyleboxOverride("panel", style);
        _root.AddChild(panel);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        panel.AddChild(scroll);

        _contentLabel = new Label();
        _contentLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _contentLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _contentLabel.LabelSettings = new LabelSettings
        {
            FontColor = Colors.White,
            OutlineSize = 2,
            OutlineColor = Colors.Black,
            FontSize = Mathf.Max(8, ContentFontSize)
        };
        scroll.AddChild(_contentLabel);

        _root.Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent) return;
        if (!keyEvent.Pressed || keyEvent.Echo) return;
        if (keyEvent.Keycode != ToggleKey) return;

        _root.Visible = !_root.Visible;
        if (_root.Visible)
        {
            RefreshText();
        }
    }

    public override void _Process(double delta)
    {
        if (!_root.Visible) return;
        RefreshText();
    }

    private void RefreshText()
    {
        if (_battleController == null || !GodotObject.IsInstanceValid(_battleController))
        {
            _contentLabel.Text = "Battle stats overlay unavailable: no active BattleController.";
            return;
        }

        var combatants = new List<Node>();
        foreach (var combatant in _battleController.GetCombatantsForDebugOverlay())
        {
            if (combatant == null || !GodotObject.IsInstanceValid(combatant)) continue;
            combatants.Add(combatant);
        }

        var text = new StringBuilder();
        text.AppendLine($"--- BATTLE STATS DEBUG ({ToggleKey}) ---");
        text.AppendLine($"Combatants: {combatants.Count}");
        text.AppendLine();

        if (combatants.Count == 0)
        {
            text.AppendLine("(No active combatants)");
            _contentLabel.Text = text.ToString();
            return;
        }

        for (int i = 0; i < combatants.Count; i++)
        {
            AppendCombatantBlock(text, combatants[i]);
            if (i < combatants.Count - 1)
            {
                text.AppendLine("--------------------------------------------------");
            }
        }

        _contentLabel.Text = text.ToString();
    }

    private void AppendCombatantBlock(StringBuilder text, Node combatant)
    {
        string side = ResolveSideLabel(combatant);
        text.AppendLine($"{combatant.Name} [{side}]");

        var stats = combatant.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null)
        {
            text.AppendLine("  No StatsComponent.");
            return;
        }

        int maxHp = stats.GetStatValue(StatType.HP);
        int maxMp = stats.GetStatValue(StatType.MP);
        int charge = _battleController.ChargeSystem != null ? _battleController.ChargeSystem.GetCharges(combatant) : 0;

        string overflowText = "n/a";
        if (_battleController.OverflowSystem != null)
        {
            OverflowPartySide overflowSide = _battleController.IsPlayerSide(combatant)
                ? OverflowPartySide.Player
                : OverflowPartySide.Enemy;
            int overflow = _battleController.OverflowSystem.GetOverflowForSide(overflowSide);
            int overflowCap = _battleController.OverflowSystem.GetOverflowCapForSide(overflowSide);
            overflowText = $"{overflow}/{overflowCap}";
        }

        text.AppendLine($"  HP: {stats.CurrentHP}/{maxHp} | MP: {stats.CurrentMP}/{maxMp} | Charge: {charge} | Overflow: {overflowText}");

        foreach (StatType statType in Enum.GetValues<StatType>())
        {
            int baseValue = stats.GetBaseStatValue(statType);
            int finalValue = stats.GetStatValue(statType);
            int delta = finalValue - baseValue;
            string deltaText = delta == 0 ? string.Empty : delta > 0 ? $" (+{delta})" : $" ({delta})";
            text.AppendLine($"  {statType,-12}: {finalValue}{deltaText} [base {baseValue}]");
        }

        var statusManager = combatant.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (statusManager == null)
        {
            text.AppendLine("  Statuses: (no StatusEffectManager)");
            return;
        }

        var activeEffects = statusManager.GetActiveEffects();
        if (activeEffects == null || activeEffects.Count == 0)
        {
            text.AppendLine("  Statuses: none");
            return;
        }

        text.AppendLine("  Statuses:");
        foreach (var instance in activeEffects)
        {
            if (instance?.EffectData == null) continue;
            text.AppendLine($"    - {instance.EffectData.EffectName} (turns={instance.RemainingTurns}, stacks={instance.Stacks})");
        }
    }

    private string ResolveSideLabel(Node combatant)
    {
        if (_battleController == null || !GodotObject.IsInstanceValid(_battleController))
        {
            return "Unknown";
        }

        if (!_battleController.IsPlayerSide(combatant))
        {
            return "Enemy";
        }

        string parentName = combatant.GetParent()?.Name.ToString() ?? string.Empty;
        if (parentName.Contains("ally", StringComparison.OrdinalIgnoreCase))
        {
            return "Ally";
        }

        return "Player";
    }
}
