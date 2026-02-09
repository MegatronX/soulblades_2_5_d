using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class BattleResultsOverlay : CanvasLayer
{
    public enum DefeatChoice
    {
        Retry,
        Quit
    }

    private enum VictoryStage
    {
        Summary,
        Party
    }

    private class PartySnapshot
    {
        public Node Member;
        public string Name;
        public LevelingComponent Leveling;
        public LevelProgression Progression;
        public int StartLevel;
        public int StartExp;
        public int EndLevel;
        public int EndExp;
    }

    private class PartyRow
    {
        public PartySnapshot Snapshot;
        public Control Root;
        public Label NameLabel;
        public Label LevelLabel;
        public Label ExpLabel;
        public ProgressBar ExpBar;
        public BattleResultsPartyRow RowComponent;
        public Control Wrapper;
    }

    [Export] private NodePath _rootPath;
    [Export] private NodePath _backdropPath;
    [Export] private NodePath _titleLabelPath;
    [Export] private NodePath _summaryContainerPath;
    [Export] private NodePath _rewardsLabelPath;
    [Export] private NodePath _rewardsSummaryLabelPath;
    [Export] private NodePath _rewardsItemsLabelPath;
    [Export] private NodePath _partyContainerPath;
    [Export] private NodePath _partyListContainerPath;
    [Export] private NodePath _eventsLabelPath;
    [Export] private NodePath _continueButtonPath;
    [Export] private NodePath _retryButtonPath;
    [Export] private NodePath _quitButtonPath;
    [Export] private NodePath _panelPath;
    [Export] private NodePath _buttonContainerPath;
    [Export] private NodePath _rewardsSummaryPanelPath;
    [Export] private NodePath _rewardsItemsPanelPath;
    [Export] private PackedScene _partyRowScene;

    [ExportGroup("Animation")]
    [Export] private float _introFadeSeconds = 0.3f;
    [Export] private float _introSlideSeconds = 0.25f;
    [Export] private float _introSlidePixels = 18.0f;
    [Export] private float _introStaggerSeconds = 0.08f;
    [Export] private float _rewardCountSeconds = 0.6f;
    [Export] private float _panelPulseSeconds = 0.25f;
    [Export] private float _rowRevealSeconds = 0.42f;
    [Export] private float _rowRevealStaggerSeconds = 0.06f;
    [Export] private float _rowSlidePixels = 40f;
    [Export] private float _rowSlideOvershootPixels = 6f;
    [Export] private float _rowSlideSettleSeconds = 0.18f;
    [Export] private bool _rowSlideFromOffscreen = true;
    [Export] private float _rowSlideOffscreenPadding = 80f;
    [Export] private bool _rowSlideElastic = false;
    [Export] private float _expPerSecond = 140f;
    [Export] private float _expSpeedScaleUnit = 100f;
    [Export] private float _expSpeedScaleMin = 0.6f;
    [Export] private float _expSpeedScaleMax = 3.0f;
    [Export] private float _expMinSegmentSeconds = 0.2f;
    [Export] private float _expMaxSegmentSeconds = 1.0f;
    [Export] private float _levelUpPauseSeconds = 0.08f;
    [Export] private float _expBannerDelaySeconds = 0.08f;

    private Control _root;
    private ColorRect _backdrop;
    private Label _titleLabel;
    private VBoxContainer _summaryContainer;
    private RichTextLabel _rewardsLabel;
    private RichTextLabel _rewardsSummaryLabel;
    private RichTextLabel _rewardsItemsLabel;
    private VBoxContainer _partyContainer;
    private VBoxContainer _partyListContainer;
    private RichTextLabel _eventsLabel;
    private Button _continueButton;
    private Button _retryButton;
    private Button _quitButton;
    private Control _panel;
    private Control _buttonContainer;
    private Control _rewardsSummaryPanel;
    private Control _rewardsItemsPanel;
    private Vector2 _panelBasePosition;

    private TaskCompletionSource<bool> _continueTcs;
    private TaskCompletionSource<DefeatChoice> _defeatTcs;
    private readonly List<System.Action> _unbindLevelEvents = new();

    private VictoryStage _victoryStage;
    private readonly List<PartySnapshot> _partySnapshots = new();
    private readonly List<PartyRow> _partyRows = new();
    private readonly Dictionary<Node, PartyRow> _partyRowsByMember = new();
    private readonly Dictionary<Node, Queue<string>> _pendingRowBanners = new();
    private bool _canShowBanners;
    private bool _isAnimatingExp;
    private bool _skipExpAnimation;
    private bool _rewardsApplied;
    private System.Action _pendingApplyRewards;
    private IEnumerable<Node> _pendingPartyMembers;

    public override void _Ready()
    {
        Layer = 200;
        CacheNodes();
        HookButtons();
        HideOverlay();
    }

    public void ShowVictory(BattleRewards rewards, System.Action applyRewards, IEnumerable<Node> partyMembers)
    {
        ShowOverlay();
        _pendingApplyRewards = applyRewards;
        _pendingPartyMembers = partyMembers;
        _rewardsApplied = false;
        _pendingRowBanners.Clear();
        _canShowBanners = false;
        CapturePartyStart(partyMembers);
        if (_eventsLabel != null) _eventsLabel.Text = string.Empty;

        _titleLabel.Text = "Victory";
        if (_rewardsSummaryLabel != null && _rewardsItemsLabel != null)
        {
            _rewardsSummaryLabel.Text = BuildRewardsSummaryText(0, 0, 0);
            _rewardsItemsLabel.Text = BuildRewardsItemsText(rewards);
        }
        else if (_rewardsLabel != null)
        {
            _rewardsLabel.Text = BuildRewardsText(rewards);
        }

        _continueButton.Text = "Continue";
        _continueButton.Visible = true;
        _retryButton.Visible = false;
        _quitButton.Visible = false;

        _summaryContainer.Show();
        _partyContainer.Hide();
        if (_eventsLabel != null) _eventsLabel.Hide();

        _victoryStage = VictoryStage.Summary;
        _continueButton.GrabFocus();

        _ = PlayVictoryIntroAsync(rewards);
    }

    public void ShowDefeat(bool allowRetry)
    {
        _titleLabel.Text = "Defeat";
        if (_rewardsSummaryLabel != null && _rewardsItemsLabel != null)
        {
            _rewardsSummaryLabel.Text = "[center]The party was defeated.[/center]";
            _rewardsItemsLabel.Text = string.Empty;
        }
        else if (_rewardsLabel != null)
        {
            _rewardsLabel.Text = "The party was defeated.";
        }

        _continueButton.Visible = false;
        _retryButton.Visible = allowRetry;
        _quitButton.Visible = true;

        _summaryContainer.Show();
        _partyContainer.Hide();

        ShowOverlay();
        if (allowRetry)
        {
            _retryButton.GrabFocus();
        }
        else
        {
            _quitButton.GrabFocus();
        }
    }

    public Task WaitForContinueAsync()
    {
        _continueTcs = new TaskCompletionSource<bool>();
        return _continueTcs.Task;
    }

    public Task<DefeatChoice> WaitForDefeatChoiceAsync()
    {
        _defeatTcs = new TaskCompletionSource<DefeatChoice>();
        return _defeatTcs.Task;
    }

    public async Task FadeOutAsync(float duration = 0.2f)
    {
        if (_root == null)
        {
            HideOverlay();
            return;
        }

        var tween = CreateTween();
        tween.TweenProperty(_root, "modulate:a", 0.0f, duration);
        await ToSignal(tween, Tween.SignalName.Finished);
        UnbindLevelEvents();
        HideOverlay();
    }

    private void CacheNodes()
    {
        _root = GetNodeOrNull<Control>(_rootPath);
        _backdrop = GetNodeOrNull<ColorRect>(_backdropPath);
        _titleLabel = GetNodeOrNull<Label>(_titleLabelPath);
        _summaryContainer = GetNodeOrNull<VBoxContainer>(_summaryContainerPath);
        _rewardsLabel = GetNodeOrNull<RichTextLabel>(_rewardsLabelPath);
        _rewardsSummaryLabel = GetNodeOrNull<RichTextLabel>(_rewardsSummaryLabelPath);
        _rewardsItemsLabel = GetNodeOrNull<RichTextLabel>(_rewardsItemsLabelPath);
        _partyContainer = GetNodeOrNull<VBoxContainer>(_partyContainerPath);
        _partyListContainer = GetNodeOrNull<VBoxContainer>(_partyListContainerPath);
        _eventsLabel = GetNodeOrNull<RichTextLabel>(_eventsLabelPath);
        _continueButton = GetNodeOrNull<Button>(_continueButtonPath);
        _retryButton = GetNodeOrNull<Button>(_retryButtonPath);
        _quitButton = GetNodeOrNull<Button>(_quitButtonPath);
        _panel = GetNodeOrNull<Control>(_panelPath);
        _buttonContainer = GetNodeOrNull<Control>(_buttonContainerPath);
        _rewardsSummaryPanel = GetNodeOrNull<Control>(_rewardsSummaryPanelPath);
        _rewardsItemsPanel = GetNodeOrNull<Control>(_rewardsItemsPanelPath);

        if (_panel != null) _panelBasePosition = _panel.Position;

        if (_root == null) GD.PrintErr("BattleResultsOverlay: Root path not set or invalid.");
        if (_titleLabel == null) GD.PrintErr("BattleResultsOverlay: TitleLabel path not set or invalid.");
        if (_summaryContainer == null) GD.PrintErr("BattleResultsOverlay: SummaryContainer path not set or invalid.");
        if (_rewardsLabel == null && (_rewardsSummaryLabel == null || _rewardsItemsLabel == null))
            GD.PrintErr("BattleResultsOverlay: Rewards labels not set or invalid.");
        if (_partyContainer == null) GD.PrintErr("BattleResultsOverlay: PartyContainer path not set or invalid.");
        if (_partyListContainer == null) GD.PrintErr("BattleResultsOverlay: PartyListContainer path not set or invalid.");
        if (_eventsLabel == null) GD.PrintErr("BattleResultsOverlay: EventsLabel path not set or invalid.");
        if (_continueButton == null) GD.PrintErr("BattleResultsOverlay: ContinueButton path not set or invalid.");
        if (_retryButton == null) GD.PrintErr("BattleResultsOverlay: RetryButton path not set or invalid.");
        if (_quitButton == null) GD.PrintErr("BattleResultsOverlay: QuitButton path not set or invalid.");
    }

    private void HookButtons()
    {
        _continueButton.Pressed += OnContinuePressed;
        _retryButton.Pressed += OnRetryPressed;
        _quitButton.Pressed += OnQuitPressed;
    }

    private void OnContinuePressed()
    {
        if (_victoryStage == VictoryStage.Summary)
        {
            ApplyPendingRewards();
            _victoryStage = VictoryStage.Party;
            _summaryContainer.Hide();
            _partyContainer.Show();
            if (_eventsLabel != null) _eventsLabel.Show();
            _continueButton.Text = "Continue";
            _continueButton.GrabFocus();
            _ = AnimatePartyExpAsync();
            return;
        }

        if (_isAnimatingExp)
        {
            _skipExpAnimation = true;
            return;
        }

        _continueTcs?.TrySetResult(true);
    }

    private void OnRetryPressed()
    {
        _defeatTcs?.TrySetResult(DefeatChoice.Retry);
    }

    private void OnQuitPressed()
    {
        _defeatTcs?.TrySetResult(DefeatChoice.Quit);
    }

    private void ShowOverlay()
    {
        if (_root == null)
        {
            Show();
            return;
        }
        _root.Show();
        _root.Modulate = new Color(1, 1, 1, 1);
    }

    private void HideOverlay()
    {
        if (_root == null)
        {
            Hide();
            return;
        }
        _root.Hide();
    }

    private static string BuildRewardsText(BattleRewards rewards)
    {
        if (rewards == null)
        {
            return "[center]No rewards.[/center]";
        }

        var sb = new StringBuilder();
        sb.AppendLine("[center]");
        sb.AppendLine($"EXP: {rewards.TotalExperience}");
        sb.AppendLine($"AP: {rewards.TotalApExperience}");
        sb.AppendLine($"Money: {rewards.TotalMoney}");
        sb.AppendLine("");
        sb.AppendLine("Items:");

        if (rewards.Items.Count == 0)
        {
            sb.AppendLine("None");
        }
        else
        {
            foreach (var kvp in rewards.Items.OrderBy(k => k.Key.ItemName))
            {
                sb.AppendLine($"{kvp.Key.ItemName} x{kvp.Value}");
            }
        }

        sb.AppendLine("[/center]");

        return sb.ToString();
    }

    private static string BuildRewardsSummaryText(BattleRewards rewards)
    {
        if (rewards == null)
        {
            return "[center]No rewards.[/center]";
        }
        return BuildRewardsSummaryText(rewards.TotalExperience, rewards.TotalApExperience, rewards.TotalMoney);
    }

    private static string BuildRewardsSummaryText(int exp, int ap, int money)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[center]");
        sb.AppendLine($"EXP: {exp}");
        sb.AppendLine($"AP: {ap}");
        sb.AppendLine($"Money: {money}");
        sb.AppendLine("[/center]");
        return sb.ToString();
    }

    private static string BuildRewardsItemsText(BattleRewards rewards)
    {
        if (rewards == null)
        {
            return "[center]Items:\nNone[/center]";
        }

        var sb = new StringBuilder();
        sb.AppendLine("[center]");
        sb.AppendLine("Items:");

        if (rewards.Items.Count == 0)
        {
            sb.AppendLine("None");
        }
        else
        {
            foreach (var kvp in rewards.Items.OrderBy(k => k.Key.ItemName))
            {
                sb.AppendLine($"{kvp.Key.ItemName} x{kvp.Value}");
            }
        }

        sb.AppendLine("[/center]");
        return sb.ToString();
    }

    private void CapturePartyStart(IEnumerable<Node> partyMembers)
    {
        _partySnapshots.Clear();
        if (partyMembers == null) return;

        foreach (var member in partyMembers)
        {
            if (member == null) continue;
            var leveling = member.GetNodeOrNull<LevelingComponent>(LevelingComponent.NodeName);
            var snapshot = new PartySnapshot
            {
                Member = member,
                Name = GetDisplayName(member),
                Leveling = leveling,
                Progression = leveling?.Progression,
                StartLevel = leveling?.CurrentLevel ?? 1,
                StartExp = leveling?.CurrentExperience ?? 0
            };
            _partySnapshots.Add(snapshot);
        }
    }

    private void CapturePartyEnd(IEnumerable<Node> partyMembers)
    {
        if (partyMembers == null) return;

        foreach (var snapshot in _partySnapshots)
        {
            var leveling = snapshot.Leveling;
            if (leveling == null) continue;
            snapshot.EndLevel = leveling.CurrentLevel;
            snapshot.EndExp = leveling.CurrentExperience;
        }
    }

    private async Task AnimatePartyExpAsync()
    {
        BuildPartyRows(useEndValues: false);
        await AnimatePartyRowsInAsync();
        _isAnimatingExp = true;
        _skipExpAnimation = false;

        var tasks = _partyRows.Select(AnimateExpForRowAsync).ToArray();
        if (_expBannerDelaySeconds > 0f)
        {
            await ToSignal(GetTree().CreateTimer(_expBannerDelaySeconds), SceneTreeTimer.SignalName.Timeout);
        }
        else
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        _canShowBanners = true;
        if (_canShowBanners)
        {
            FlushPendingRowBanners();
        }
        await Task.WhenAll(tasks);

        _isAnimatingExp = false;
    }

    private async Task AnimatePartyRowsInAsync()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        foreach (var row in _partyRows)
        {
            if (row?.Root == null) continue;
            row.Root.Modulate = new Color(1, 1, 1, 1);
            row.Root.Scale = Vector2.One;
            if (row.Wrapper != null)
            {
                row.Root.Size = row.Wrapper.Size;
            }
            var slideNode = GetRowSlideNode(row);
            if (slideNode != null)
            {
                float slideDistance = _rowSlideFromOffscreen
                    ? ((row.Wrapper?.Size.X ?? GetViewport().GetVisibleRect().Size.X) + _rowSlideOffscreenPadding)
                    : _rowSlidePixels;
                slideNode.Position = slideNode.Position + new Vector2(slideDistance, 0);
            }
        }

        foreach (var row in _partyRows)
        {
            if (row?.Root == null) continue;
            var slideNode = GetRowSlideNode(row);
            if (slideNode == null) continue;

            float slideDistance = _rowSlideFromOffscreen
                ? ((row.Wrapper?.Size.X ?? GetViewport().GetVisibleRect().Size.X) + _rowSlideOffscreenPadding)
                : _rowSlidePixels;
            var basePos = slideNode.Position - new Vector2(slideDistance, 0);
            slideNode.Position = basePos + new Vector2(slideDistance, 0);

            var tween = CreateTween();
            if (_rowSlideElastic)
            {
                tween.TweenProperty(slideNode, "position", basePos, _rowRevealSeconds)
                    .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
            }
            else
            {
                tween.TweenProperty(slideNode, "position", basePos - new Vector2(_rowSlideOvershootPixels, 0), _rowRevealSeconds)
                    .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                tween.TweenProperty(slideNode, "position", basePos, _rowSlideSettleSeconds)
                    .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            }
            await ToSignal(tween, Tween.SignalName.Finished);
            if (_rowRevealStaggerSeconds > 0f)
            {
                await ToSignal(GetTree().CreateTimer(_rowRevealStaggerSeconds), SceneTreeTimer.SignalName.Timeout);
            }
        }
    }

    private static Control GetRowSlideNode(PartyRow row)
    {
        return row?.Root;
    }

    private void BuildPartyRows(bool useEndValues)
    {
        foreach (var row in _partyRows)
        {
            row.Root?.QueueFree();
        }
        _partyRows.Clear();
        _partyRowsByMember.Clear();

        foreach (Node child in _partyListContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var snapshot in _partySnapshots)
        {
            PartyRow row = _partyRowScene != null
                ? CreatePartyRowFromScene(snapshot)
                : CreateDefaultPartyRow(snapshot);

            if (row != null)
            {
                if (row.Root != null)
                {
                    var wrapper = new Control
                    {
                        Name = $"{row.Root.Name}_Wrapper",
                        ClipContents = true,
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                        SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                        SizeFlagsStretchRatio = 1.0f,
                        CustomMinimumSize = row.Root.CustomMinimumSize
                    };
                    _partyListContainer.AddChild(wrapper);

                    if (row.Root.GetParent() != null)
                    {
                        row.Root.GetParent().RemoveChild(row.Root);
                    }
                    wrapper.AddChild(row.Root);
                    row.Wrapper = wrapper;

                    row.Root.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
                    row.Root.Position = Vector2.Zero;
                }
                _partyRows.Add(row);
                if (snapshot.Member != null)
                {
                    _partyRowsByMember[snapshot.Member] = row;
                }
                ApplyPartyRowValues(row, useEndValues);
            }
        }

        if (_canShowBanners)
        {
            FlushPendingRowBanners();
        }
    }

    private void ApplyPartyRowValues(PartyRow row, bool useEndValues)
    {
        var snapshot = row.Snapshot;
        int level = useEndValues ? snapshot.EndLevel : snapshot.StartLevel;
        int totalExp = useEndValues ? snapshot.EndExp : snapshot.StartExp;

        row.LevelLabel.Text = $"Lv {level}";

        if (snapshot.Progression != null)
        {
            int levelStartExp = snapshot.Progression.GetTotalExpForLevel(level);
            int levelEndExp = snapshot.Progression.GetTotalExpForLevel(level + 1);
            UpdateExpRow(row, totalExp, levelStartExp, levelEndExp);
        }
        else
        {
            row.ExpBar.Value = 0;
            row.ExpLabel.Text = $"EXP {totalExp}";
        }
    }

    private PartyRow CreatePartyRowFromScene(PartySnapshot snapshot)
    {
        var root = _partyRowScene.Instantiate<Control>();
        _partyListContainer.AddChild(root);

        var rowComponent = root as BattleResultsPartyRow;
        if (rowComponent == null)
        {
            GD.PrintErr("BattleResultsOverlay: PartyRow scene root must have BattleResultsPartyRow attached. Falling back to default row.");
            root.QueueFree();
            return CreateDefaultPartyRow(snapshot);
        }

        rowComponent.CacheNodes();

        var nameLabel = rowComponent.NameLabel;
        var levelLabel = rowComponent.LevelLabel;
        var expBar = rowComponent.ExpBar;
        var expLabel = rowComponent.ExpLabel;
        var portraitRect = rowComponent.PortraitRect;

        if (!rowComponent.HasRequiredNodes)
        {
            GD.PrintErr("BattleResultsOverlay: PartyRow scene missing required nodes in BattleResultsPartyRow. Falling back to default row.");
            root.QueueFree();
            return CreateDefaultPartyRow(snapshot);
        }

        nameLabel.Text = snapshot.Name;
        levelLabel.Text = $"Lv {snapshot.StartLevel}";
        expLabel.Text = "EXP 0 / 0";
        expBar.MinValue = 0;
        expBar.MaxValue = 1;
        expBar.Value = 0.0f;
        if (portraitRect != null)
        {
            portraitRect.Texture = GetPortraitTexture(snapshot.Member);
        }

        return new PartyRow
        {
            Snapshot = snapshot,
            Root = root,
            NameLabel = nameLabel,
            LevelLabel = levelLabel,
            ExpBar = expBar,
            ExpLabel = expLabel,
            RowComponent = rowComponent
        };
    }

    private PartyRow CreateDefaultPartyRow(PartySnapshot snapshot)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        _partyListContainer.AddChild(row);

        var nameLabel = new Label { Text = snapshot.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(nameLabel);

        var levelLabel = new Label { Text = $"Lv {snapshot.StartLevel}", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(levelLabel);

        var expBar = new ProgressBar { MinValue = 0, MaxValue = 1, Value = 0.0f, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        expBar.CustomMinimumSize = new Vector2(160, 12);
        row.AddChild(expBar);

        var expLabel = new Label { Text = "EXP 0 / 0", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(expLabel);

        return new PartyRow
        {
            Snapshot = snapshot,
            Root = row,
            NameLabel = nameLabel,
            LevelLabel = levelLabel,
            ExpBar = expBar,
            ExpLabel = expLabel
        };
    }

    private async Task AnimateExpForRowAsync(PartyRow row)
    {
        var snapshot = row.Snapshot;
        var progression = snapshot.Progression;

        if (snapshot.Leveling == null || progression == null)
        {
            row.ExpLabel.Text = $"EXP {snapshot.EndExp}";
            row.ExpBar.Value = 0;
            return;
        }

        int currentLevel = snapshot.StartLevel;
        int currentTotal = snapshot.StartExp;
        int targetTotal = snapshot.EndExp;

        while (currentTotal < targetTotal)
        {
            if (_skipExpAnimation)
            {
                break;
            }

            int levelStartExp = progression.GetTotalExpForLevel(currentLevel);
            int levelEndExp = progression.GetTotalExpForLevel(currentLevel + 1);

            int segmentEnd = Mathf.Min(targetTotal, levelEndExp);
            float segmentExp = Mathf.Max(0, segmentEnd - currentTotal);
            float speedScale = Mathf.Clamp(Mathf.Sqrt(segmentExp / Mathf.Max(1f, _expSpeedScaleUnit)), _expSpeedScaleMin, _expSpeedScaleMax);
            float duration = Mathf.Clamp(segmentExp / Mathf.Max(1f, _expPerSecond * speedScale), _expMinSegmentSeconds, _expMaxSegmentSeconds);

            await AnimateExpSegmentAsync(row, currentTotal, segmentEnd, levelStartExp, levelEndExp, duration);

            currentTotal = segmentEnd;

            if (currentTotal >= levelEndExp && currentLevel < snapshot.EndLevel)
            {
                currentLevel++;
                row.LevelLabel.Text = $"Lv {currentLevel}";
                row.ExpBar.Value = 0;
                if (_levelUpPauseSeconds > 0f)
                {
                    await ToSignal(GetTree().CreateTimer(_levelUpPauseSeconds), SceneTreeTimer.SignalName.Timeout);
                }
            }
        }

        if (_skipExpAnimation)
        {
            currentLevel = snapshot.EndLevel;
            row.LevelLabel.Text = $"Lv {currentLevel}";
            UpdateExpRow(row, snapshot.EndExp, progression);
        }
    }

    private async Task AnimateExpSegmentAsync(PartyRow row, int startTotal, int endTotal, int levelStartExp, int levelEndExp, float duration)
    {
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(v =>
        {
            UpdateExpRow(row, Mathf.RoundToInt(v), levelStartExp, levelEndExp);
        }), startTotal, endTotal, duration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private void UpdateExpRow(PartyRow row, int totalExp, LevelProgression progression)
    {
        int level = Mathf.Max(1, row.Snapshot.Leveling.CurrentLevel);
        int levelStartExp = progression.GetTotalExpForLevel(level);
        int levelEndExp = progression.GetTotalExpForLevel(level + 1);
        UpdateExpRow(row, totalExp, levelStartExp, levelEndExp);
    }

    private void UpdateExpRow(PartyRow row, int totalExp, int levelStartExp, int levelEndExp)
    {
        int levelExp = Mathf.Max(0, totalExp - levelStartExp);
        int required = Mathf.Max(1, levelEndExp - levelStartExp);
        float progress = Mathf.Clamp((float)levelExp / required, 0f, 1f);
        row.ExpBar.Value = progress;
        row.ExpLabel.Text = $"EXP {levelExp} / {required}";
    }

    private void BindLevelEvents(IEnumerable<Node> partyMembers)
    {
        UnbindLevelEvents();
        if (_eventsLabel != null) _eventsLabel.Text = string.Empty;

        if (partyMembers == null) return;

        foreach (var member in partyMembers)
        {
            if (member == null) continue;
            var leveling = member.GetNodeOrNull<LevelingComponent>(LevelingComponent.NodeName);
            if (leveling == null) continue;

            string memberName = GetDisplayName(member);
            var memberRef = member;

            LevelingComponent.LevelUpEventHandler levelUpHandler = (oldLevel, newLevel) =>
            {
                var message = $"{memberName} reached Lv {newLevel}!";
                ShowRowBanner(memberRef, $"LEVEL UP! Lv {newLevel}");
                AppendEventLine(message);
            };
            leveling.LevelUp += levelUpHandler;
            _unbindLevelEvents.Add(() => leveling.LevelUp -= levelUpHandler);

            LevelingComponent.StatIncreasedEventHandler statHandler = (statType, oldValue, newValue) =>
            {
                var message = $"{memberName} {((StatType)statType)} {oldValue} -> {newValue}";
                ShowRowBanner(memberRef, message);
                AppendEventLine(message);
            };
            leveling.StatIncreased += statHandler;
            _unbindLevelEvents.Add(() => leveling.StatIncreased -= statHandler);

            LevelingComponent.AbilityLearnedEventHandler abilityHandler = ability =>
            {
                var message = $"{memberName} learned {ability.AbilityName}";
                ShowRowBanner(memberRef, message);
                AppendEventLine(message);
            };
            leveling.AbilityLearned += abilityHandler;
            _unbindLevelEvents.Add(() => leveling.AbilityLearned -= abilityHandler);

            LevelingComponent.ActionLearnedEventHandler actionHandler = action =>
            {
                var message = $"{memberName} learned {action.CommandName}";
                ShowRowBanner(memberRef, message);
                AppendEventLine(message);
            };
            leveling.ActionLearned += actionHandler;
            _unbindLevelEvents.Add(() => leveling.ActionLearned -= actionHandler);
        }
    }

    private void UnbindLevelEvents()
    {
        foreach (var unbind in _unbindLevelEvents)
        {
            unbind();
        }
        _unbindLevelEvents.Clear();
    }

    private void AppendEventLine(string text)
    {
        if (_eventsLabel == null) return;
        if (string.IsNullOrEmpty(_eventsLabel.Text))
        {
            _eventsLabel.Text = text;
        }
        else
        {
            _eventsLabel.Text += "\n" + text;
        }
    }

    private void ApplyPendingRewards()
    {
        if (_rewardsApplied) return;
        _rewardsApplied = true;
        _canShowBanners = false;

        if (_pendingPartyMembers != null)
        {
            BindLevelEvents(_pendingPartyMembers);
        }

        _pendingApplyRewards?.Invoke();

        if (_pendingPartyMembers != null)
        {
            CapturePartyEnd(_pendingPartyMembers);
        }
    }

    private async Task PlayVictoryIntroAsync(BattleRewards rewards)
    {
        if (_backdrop != null)
        {
            var color = _backdrop.Color;
            color.A = 0f;
            _backdrop.Color = color;
        }

        if (_panel != null)
        {
            _panel.Position = _panelBasePosition + new Vector2(0, _introSlidePixels);
            _panel.Modulate = new Color(1, 1, 1, 0);
        }

        SetAlpha(_titleLabel, 0f);
        SetAlpha(_rewardsSummaryPanel, 0f);
        SetAlpha(_rewardsItemsPanel, 0f);
        SetAlpha(_buttonContainer, 0f);

        var introTween = CreateTween();
        if (_backdrop != null)
        {
            introTween.TweenProperty(_backdrop, "color:a", 0.6f, _introFadeSeconds);
        }
        if (_panel != null)
        {
            introTween.Parallel().TweenProperty(_panel, "modulate:a", 1f, _introFadeSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            introTween.Parallel().TweenProperty(_panel, "position", _panelBasePosition, _introSlideSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        }
        await ToSignal(introTween, Tween.SignalName.Finished);

        await FadeInAsync(_titleLabel, _introStaggerSeconds);
        await FadeInAsync(_rewardsSummaryPanel, _introStaggerSeconds);
        await FadeInAsync(_rewardsItemsPanel, _introStaggerSeconds);
        await FadeInAsync(_buttonContainer, _introStaggerSeconds);

        PulsePanel(_rewardsSummaryPanel);
        PulsePanel(_rewardsItemsPanel);

        if (_rewardsSummaryLabel != null)
        {
            await AnimateRewardCountsAsync(rewards);
        }
    }

    private async Task FadeInAsync(CanvasItem item, float delay)
    {
        if (item == null) return;
        item.Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(item, "modulate:a", 1f, _introFadeSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        await ToSignal(tween, Tween.SignalName.Finished);
        if (delay > 0f)
        {
            await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private void SetAlpha(CanvasItem item, float alpha)
    {
        if (item == null) return;
        var c = item.Modulate;
        c.A = alpha;
        item.Modulate = c;
    }

    private void PulsePanel(CanvasItem panel)
    {
        if (panel == null) return;
        var tween = CreateTween();
        tween.TweenProperty(panel, "modulate", new Color(1.1f, 1.1f, 1.1f, panel.Modulate.A), _panelPulseSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(panel, "modulate", new Color(1f, 1f, 1f, panel.Modulate.A), _panelPulseSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
    }

    private async Task AnimateRewardCountsAsync(BattleRewards rewards)
    {
        if (rewards == null) return;
        float elapsed = 0f;
        int exp = 0;
        int ap = 0;
        int money = 0;

        while (elapsed < _rewardCountSeconds)
        {
            float t = Mathf.Clamp(elapsed / _rewardCountSeconds, 0f, 1f);
            exp = Mathf.RoundToInt(Mathf.Lerp(0, rewards.TotalExperience, t));
            ap = Mathf.RoundToInt(Mathf.Lerp(0, rewards.TotalApExperience, t));
            money = Mathf.RoundToInt(Mathf.Lerp(0, rewards.TotalMoney, t));
            _rewardsSummaryLabel.Text = BuildRewardsSummaryText(exp, ap, money);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            elapsed += (float)GetProcessDeltaTime();
        }

        _rewardsSummaryLabel.Text = BuildRewardsSummaryText(rewards);
    }

    private void ShowRowBanner(Node member, string text)
    {
        if (member == null) return;
        if (_canShowBanners && _partyRowsByMember.TryGetValue(member, out var row) && row?.RowComponent != null)
        {
            row.RowComponent.EnqueueBanner(text);
            return;
        }

        if (!_pendingRowBanners.TryGetValue(member, out var queue))
        {
            queue = new Queue<string>();
            _pendingRowBanners[member] = queue;
        }
        queue.Enqueue(text);
    }

    private void FlushPendingRowBanners()
    {
        if (_pendingRowBanners.Count == 0) return;

        foreach (var kvp in _pendingRowBanners)
        {
            if (!_partyRowsByMember.TryGetValue(kvp.Key, out var row) || row?.RowComponent == null)
            {
                continue;
            }

            foreach (var text in kvp.Value)
            {
                row.RowComponent.EnqueueBanner(text);
            }
        }

        _pendingRowBanners.Clear();
    }

    private static string GetDisplayName(Node member)
    {
        if (member is BaseCharacter baseCharacter)
        {
            var displayName = baseCharacter.PresentationData?.DisplayName;
            if (!string.IsNullOrEmpty(displayName))
            {
                return displayName;
            }
        }
        return member.Name;
    }

    private static Texture2D GetPortraitTexture(Node member)
    {
        if (member is BaseCharacter baseCharacter)
        {
            return baseCharacter.PresentationData?.PortraitImage;
        }
        return null;
    }
}
