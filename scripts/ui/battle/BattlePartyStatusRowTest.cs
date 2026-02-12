using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Quick harness to test BattlePartyStatusRow without running a full battle.
/// </summary>
public partial class BattlePartyStatusRowTest : Control
{
    [Export] private PackedScene _rowScene;
    [Export] private NodePath _rowHostPath;
    [Export] private NodePath _infoLabelPath;
    [Export] private NodePath _amountSpinPath;
    [Export] private NodePath _damageButtonPath;
    [Export] private NodePath _healButtonPath;
    [Export] private NodePath _mpUseButtonPath;
    [Export] private NodePath _mpGainButtonPath;
    [Export] private NodePath _chargeGainButtonPath;
    [Export] private NodePath _chargeSpendButtonPath;
    [Export] private NodePath _statusNameEditPath;
    [Export] private NodePath _statusAddButtonPath;
    [Export] private NodePath _statusRemoveButtonPath;

    [ExportGroup("Playback")]
    [Export] private bool _autoPlay = true;
    [Export] private bool _loop = true;
    [Export] private float _stepDelaySeconds = 1.0f;

    [ExportGroup("Status Icons")]
    [Export] private Texture2D _positiveIcon;
    [Export] private Texture2D _negativeIcon;

    [ExportGroup("Status Library")]
    [Export] private Godot.Collections.Dictionary<string, StatusEffect> _statusEffectLibrary = new();

    private Control _rowHost;
    private Label _infoLabel;
    private SpinBox _amountSpin;
    private Button _damageButton;
    private Button _healButton;
    private Button _mpUseButton;
    private Button _mpGainButton;
    private Button _chargeGainButton;
    private Button _chargeSpendButton;
    private LineEdit _statusNameEdit;
    private Button _statusAddButton;
    private Button _statusRemoveButton;
    private BattlePartyStatusRow _row;

    private BaseCharacter _character;
    private StatsComponent _stats;
    private StatusEffectManager _statusManager;
    private ChargeSystem _chargeSystem;

    private StatusEffect _positiveEffect;
    private StatusEffect _negativeEffect;

    private readonly List<System.Action> _steps = new();
    private int _stepIndex;
    private bool _isRunning;

    public override async void _Ready()
    {
        _rowHost = GetNodeOrNull<Control>(_rowHostPath);
        _infoLabel = GetNodeOrNull<Label>(_infoLabelPath);
        _amountSpin = GetNodeOrNull<SpinBox>(_amountSpinPath);
        _damageButton = GetNodeOrNull<Button>(_damageButtonPath);
        _healButton = GetNodeOrNull<Button>(_healButtonPath);
        _mpUseButton = GetNodeOrNull<Button>(_mpUseButtonPath);
        _mpGainButton = GetNodeOrNull<Button>(_mpGainButtonPath);
        _chargeGainButton = GetNodeOrNull<Button>(_chargeGainButtonPath);
        _chargeSpendButton = GetNodeOrNull<Button>(_chargeSpendButtonPath);
        _statusNameEdit = GetNodeOrNull<LineEdit>(_statusNameEditPath);
        _statusAddButton = GetNodeOrNull<Button>(_statusAddButtonPath);
        _statusRemoveButton = GetNodeOrNull<Button>(_statusRemoveButtonPath);

        if (_rowScene == null)
        {
            _rowScene = GD.Load<PackedScene>("res://assets/scenes/battle/ui/BattlePartyStatusRow.tscn");
        }

        if (_rowScene == null || _rowHost == null)
        {
            GD.PrintErr("BattlePartyStatusRowTest: Missing row scene or RowHost path.");
            return;
        }

        _row = _rowScene.Instantiate<BattlePartyStatusRow>();
        _rowHost.AddChild(_row);

        BuildCharacter();

        // Let nodes initialize before binding.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        _row.Bind(_character, _chargeSystem);

        EnsureStatusLibraryDefaults();
        HookManualControls();

        BuildSteps();
        UpdateInfo("Ready. Press Space/Enter to step.");

        if (_autoPlay)
        {
            _ = RunSequenceAsync();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_autoPlay) return;

        if (@event.IsActionPressed("ui_accept") || (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Space))
        {
            _ = StepOnceAsync();
        }
    }

    private void BuildCharacter()
    {
        _character = new BaseCharacter { Name = "TestHero" };

        _stats = new StatsComponent { Name = StatsComponent.NodeName };
        _stats.SetBaseStatsResource(CreateBaseStats());
        _character.AddChild(_stats);

        _statusManager = new StatusEffectManager { Name = StatusEffectManager.NodeName };
        _character.AddChild(_statusManager);

        AddChild(_character);

        _chargeSystem = new ChargeSystem();
        AddChild(_chargeSystem);

        _positiveEffect = CreateStatusEffect("Haste", StatusEffectPolarity.Positive, _positiveIcon, new Color(0.4f, 1.0f, 0.6f));
        _negativeEffect = CreateStatusEffect("Poison", StatusEffectPolarity.Negative, _negativeIcon, new Color(0.9f, 0.4f, 0.4f));
    }

    private void BuildSteps()
    {
        _steps.Clear();
        _steps.Add(() => { UpdateInfo("Gain charges (+2)"); _chargeSystem.AddCharges(_character, 2); });
        _steps.Add(() => { UpdateInfo("Spend charges (-1)"); _chargeSystem.TrySpendCharges(_character, 1); });
        _steps.Add(() => { UpdateInfo("Take damage (-30 HP)"); _stats.ModifyCurrentHP(-30); });
        _steps.Add(() => { UpdateInfo("Heal (+20 HP)"); _stats.ModifyCurrentHP(20); });
        _steps.Add(() => { UpdateInfo("Use MP (-10 MP)"); _stats.ModifyCurrentMP(-10); });
        _steps.Add(() => { UpdateInfo("Gain status effect (Haste)"); _statusManager.ApplyEffect(_positiveEffect, null); });
        _steps.Add(() => { UpdateInfo("Lose status effect (Haste)"); RemoveEffect(_positiveEffect); });
        _steps.Add(() => { UpdateInfo("Gain status effect (Poison)"); _statusManager.ApplyEffect(_negativeEffect, null); });
        _steps.Add(() => { UpdateInfo("Lose status effect (Poison)"); RemoveEffect(_negativeEffect); });
    }

    private void HookManualControls()
    {
        if (_damageButton != null)
        {
            _damageButton.Pressed += () =>
            {
                int amount = GetAmount();
                UpdateInfo($"Damage -{amount} HP");
                _stats?.ModifyCurrentHP(-amount);
            };
        }

        if (_healButton != null)
        {
            _healButton.Pressed += () =>
            {
                int amount = GetAmount();
                UpdateInfo($"Heal +{amount} HP");
                _stats?.ModifyCurrentHP(amount);
            };
        }

        if (_mpUseButton != null)
        {
            _mpUseButton.Pressed += () =>
            {
                int amount = GetAmount();
                UpdateInfo($"MP -{amount}");
                _stats?.ModifyCurrentMP(-amount);
            };
        }

        if (_mpGainButton != null)
        {
            _mpGainButton.Pressed += () =>
            {
                int amount = GetAmount();
                UpdateInfo($"MP +{amount}");
                _stats?.ModifyCurrentMP(amount);
            };
        }

        if (_chargeGainButton != null)
        {
            _chargeGainButton.Pressed += () =>
            {
                int amount = GetAmount();
                UpdateInfo($"Charges +{amount}");
                _chargeSystem?.AddCharges(_character, amount);
            };
        }

        if (_chargeSpendButton != null)
        {
            _chargeSpendButton.Pressed += () =>
            {
                int amount = GetAmount();
                UpdateInfo($"Charges -{amount}");
                _chargeSystem?.TrySpendCharges(_character, amount);
            };
        }

        if (_statusAddButton != null)
        {
            _statusAddButton.Pressed += AddStatusFromInput;
        }

        if (_statusRemoveButton != null)
        {
            _statusRemoveButton.Pressed += RemoveStatusFromInput;
        }

        if (_statusNameEdit != null)
        {
            _statusNameEdit.TextSubmitted += _ => AddStatusFromInput();
        }
    }

    private void AddStatusFromInput()
    {
        var effect = FindStatusEffectFromInput();
        if (effect == null) return;

        UpdateInfo($"Add status: {effect.EffectName}");
        _statusManager?.ApplyEffect(effect, null);
    }

    private void RemoveStatusFromInput()
    {
        var effect = FindStatusEffectFromInput();
        if (effect == null) return;

        UpdateInfo($"Remove status: {effect.EffectName}");
        RemoveEffect(effect);
    }

    private StatusEffect FindStatusEffectFromInput()
    {
        if (_statusNameEdit == null)
        {
            UpdateInfo("Status name field missing.");
            return null;
        }

        string input = _statusNameEdit.Text?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            UpdateInfo("Enter a status effect name.");
            return null;
        }

        if (_statusEffectLibrary != null && _statusEffectLibrary.TryGetValue(input, out var effect))
        {
            return effect;
        }

        if (_statusEffectLibrary != null)
        {
            foreach (var key in _statusEffectLibrary.Keys)
            {
                if (string.Equals(key, input, System.StringComparison.OrdinalIgnoreCase))
                {
                    return _statusEffectLibrary[key];
                }
            }
        }

        UpdateInfo($"Status '{input}' not found in library.");
        return null;
    }

    private void EnsureStatusLibraryDefaults()
    {
        if (_statusEffectLibrary == null) return;

        if (!_statusEffectLibrary.ContainsKey("Haste"))
        {
            _statusEffectLibrary["Haste"] = _positiveEffect;
        }

        if (!_statusEffectLibrary.ContainsKey("Poison"))
        {
            _statusEffectLibrary["Poison"] = _negativeEffect;
        }
    }

    private async Task RunSequenceAsync()
    {
        if (_isRunning) return;
        _isRunning = true;

        do
        {
            for (_stepIndex = 0; _stepIndex < _steps.Count; _stepIndex++)
            {
                _steps[_stepIndex]?.Invoke();
                if (_stepDelaySeconds > 0f)
                {
                    await ToSignal(GetTree().CreateTimer(_stepDelaySeconds), SceneTreeTimer.SignalName.Timeout);
                }
            }
        } while (_loop);

        _isRunning = false;
    }

    private async Task StepOnceAsync()
    {
        if (_isRunning) return;
        _isRunning = true;

        if (_steps.Count == 0)
        {
            _isRunning = false;
            return;
        }

        if (_stepIndex >= _steps.Count) _stepIndex = 0;
        _steps[_stepIndex]?.Invoke();
        _stepIndex = (_stepIndex + 1) % _steps.Count;

        _isRunning = false;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void RemoveEffect(StatusEffect effect)
    {
        var instance = _statusManager.GetActiveEffects().FirstOrDefault(e => e.EffectData == effect);
        if (instance != null)
        {
            _statusManager.RemoveEffect(instance, null);
        }
    }

    private void UpdateInfo(string text)
    {
        if (_infoLabel != null)
        {
            _infoLabel.Text = text;
        }
    }

    private int GetAmount()
    {
        if (_amountSpin == null) return 10;
        return Mathf.Max(1, Mathf.RoundToInt((float)_amountSpin.Value));
    }

    private static BaseStats CreateBaseStats()
    {
        return new BaseStats
        {
            HP = 180,
            MP = 60,
            Strength = 10,
            Defense = 8,
            Magic = 6,
            MagicDefense = 5,
            Speed = 9,
            Evasion = 3,
            MgEvasion = 3,
            Accuracy = 8,
            MgAccuracy = 6,
            Luck = 5,
            AP = 20
        };
    }

    private static StatusEffect CreateStatusEffect(string name, StatusEffectPolarity polarity, Texture2D icon, Color fallbackColor)
    {
        var effect = new StatusEffect();
        effect.Set("EffectName", name);
        effect.Polarity = polarity;
        effect.Set("MinDurationTurns", 1);
        effect.Set("MaxDurationTurns", 3);

        if (icon == null)
        {
            icon = CreateSolidTexture(16, 16, fallbackColor);
        }
        effect.Set("Icon", icon);
        return effect;
    }

    private static Texture2D CreateSolidTexture(int width, int height, Color color)
    {
        var image = Image.Create(width, height, false, Image.Format.Rgba8);
        image.Fill(color);
        return ImageTexture.CreateFromImage(image);
    }
}
