using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class BattleResultsOverlay
{
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
        public Label ExpGainLabel;
        public ProgressBar ExpBar;
        public BattleResultsPartyRow RowComponent;
        public Control Wrapper;
        public Vector2 SlideBasePosition;
        public float SlideDistance;
        public bool SlidePrepared;
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
            PrepareRowSlide(row);
        }

        var tasks = new List<Task>();
        float startDelay = 0f;
        foreach (var row in _partyRows)
        {
            tasks.Add(AnimateRowSlideAsync(row, startDelay));
            if (_rowRevealStaggerSeconds > 0f)
            {
                startDelay += _rowRevealStaggerSeconds;
            }
        }
        await Task.WhenAll(tasks);
    }

    private async Task AnimateRowSlideAsync(PartyRow row, float delay)
    {
        if (row?.Root == null) return;
        if (delay > 0f)
        {
            await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
        }

        var slideNode = GetRowSlideNode(row);
        if (slideNode == null) return;

        if (!row.SlidePrepared)
        {
            PrepareRowSlide(row);
        }

        var basePos = row.SlideBasePosition;
        var slideDistance = row.SlideDistance;
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
    }

    private static Control GetRowSlideNode(PartyRow row)
    {
        return row?.Root;
    }

    private void PrepareRowSlide(PartyRow row)
    {
        if (row?.Root == null) return;

        row.SlideBasePosition = row.Root.Position;
        row.SlideDistance = GetRowSlideDistance(row);
        row.SlidePrepared = true;

        var slideNode = GetRowSlideNode(row);
        if (slideNode != null)
        {
            slideNode.Position = row.SlideBasePosition + new Vector2(row.SlideDistance, 0);
        }
    }

    private float GetRowSlideDistance(PartyRow row)
    {
        if (!_rowSlideFromOffscreen)
        {
            return _rowSlidePixels;
        }

        float viewportWidth = GetViewport()?.GetVisibleRect().Size.X ?? 0f;
        float wrapperWidth = row?.Wrapper?.Size.X ?? 0f;
        float width = Mathf.Max(viewportWidth, wrapperWidth);
        if (width <= 0f)
        {
            width = _rowSlidePixels;
        }
        return width + _rowSlideOffscreenPadding;
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
            UpdateExpGainLabel(row, totalExp);
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
        var expGainLabel = rowComponent.ExpGainLabel;
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
            ExpGainLabel = expGainLabel,
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
        var expGainLabel = new Label { Text = "EXP +0", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(expGainLabel);

        return new PartyRow
        {
            Snapshot = snapshot,
            Root = row,
            NameLabel = nameLabel,
            LevelLabel = levelLabel,
            ExpBar = expBar,
            ExpLabel = expLabel,
            ExpGainLabel = expGainLabel
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
            UpdateExpGainLabel(row, snapshot.EndExp);
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
        UpdateExpGainLabel(row, totalExp);
    }

    private void UpdateExpGainLabel(PartyRow row, int totalExp)
    {
        if (row.ExpGainLabel == null) return;
        int endExp = row.Snapshot?.EndExp ?? totalExp;
        int remaining = Mathf.Max(0, endExp - totalExp);
        row.ExpGainLabel.Text = $"EXP +{remaining}";
    }
}
