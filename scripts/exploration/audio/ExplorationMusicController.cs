using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scene-level music controller for explorable maps.
/// Supports weighted random selection, track swaps, and runtime mix overrides.
/// </summary>
[GlobalClass]
public partial class ExplorationMusicController : Node
{
    [Export]
    public Godot.Collections.Array<BattleMusicData> MusicLibrary { get; private set; } = new();

    [Export]
    public BattleMusicData InitialTrack { get; private set; }

    [Export]
    public bool PlayRandomOnReady { get; private set; } = true;

    [Export(PropertyHint.Range, "0,4,0.01")]
    public float DefaultCrossfadeSeconds { get; private set; } = 0.6f;

    [Export(PropertyHint.Range, "-24,24,0.1")]
    public float RuntimeVolumeOffsetDb { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0.1,4,0.01")]
    public float RuntimePitchMultiplier { get; private set; } = 1f;

    [Export]
    public bool ForceLoopPlayback { get; private set; } = true;

    private readonly RandomNumberGenerator _rng = new();
    private AudioStreamPlayer _playerA;
    private AudioStreamPlayer _playerB;
    private AudioStreamPlayer _currentPlayer;
    private BattleMusicData _currentTrack;
    private Tween _tween;

    public override void _Ready()
    {
        _rng.Randomize();
        EnsurePlayers();

        if (InitialTrack != null)
        {
            PlayTrack(InitialTrack, false);
            return;
        }

        if (PlayRandomOnReady)
        {
            PlayRandomTrack(false);
        }
    }

    public void PlayRandomTrack(bool crossfade = true)
    {
        var track = GetRandomTrack();
        if (track == null) return;
        PlayTrack(track, crossfade);
    }

    public void PlayTrack(BattleMusicData track, bool crossfade = true, float crossfadeSeconds = -1f)
    {
        if (track == null || track.Stream == null) return;
        EnsurePlayers();

        if (_currentTrack == track && _currentPlayer != null && _currentPlayer.Playing)
        {
            ReapplyMixToCurrentPlayer();
            return;
        }

        float duration = crossfadeSeconds >= 0f ? crossfadeSeconds : DefaultCrossfadeSeconds;
        duration = Mathf.Max(0f, duration);

        if (!crossfade || _currentPlayer == null || !_currentPlayer.Playing || duration <= 0.001f)
        {
            _tween?.Kill();
            _playerA.Stop();
            _playerB.Stop();
            _currentPlayer = _playerA;
            ConfigurePlayer(_currentPlayer, track, ResolveTrackVolume(track));
            _currentPlayer.Play();
            _currentTrack = track;
            return;
        }

        _tween?.Kill();
        var outgoing = _currentPlayer;
        var incoming = outgoing == _playerA ? _playerB : _playerA;

        ConfigurePlayer(incoming, track, -80f);
        incoming.Play();

        _tween = CreateTween();
        _tween.SetParallel(true);
        _tween.TweenProperty(outgoing, "volume_db", -80f, duration);
        _tween.TweenProperty(incoming, "volume_db", ResolveTrackVolume(track), duration);

        _currentPlayer = incoming;
        _currentTrack = track;
    }

    public void SetRuntimeMix(float volumeOffsetDb, float pitchMultiplier, bool reapplyToCurrentTrack = true)
    {
        RuntimeVolumeOffsetDb = volumeOffsetDb;
        RuntimePitchMultiplier = Mathf.Max(0.1f, pitchMultiplier);
        if (reapplyToCurrentTrack)
        {
            ReapplyMixToCurrentPlayer();
        }
    }

    public void AdjustRuntimeMix(float volumeOffsetDeltaDb, float pitchMultiplierDelta, bool reapplyToCurrentTrack = true)
    {
        RuntimeVolumeOffsetDb += volumeOffsetDeltaDb;
        RuntimePitchMultiplier = Mathf.Max(0.1f, RuntimePitchMultiplier + pitchMultiplierDelta);
        if (reapplyToCurrentTrack)
        {
            ReapplyMixToCurrentPlayer();
        }
    }

    public void ResetRuntimeMix(bool reapplyToCurrentTrack = true)
    {
        RuntimeVolumeOffsetDb = 0f;
        RuntimePitchMultiplier = 1f;
        if (reapplyToCurrentTrack)
        {
            ReapplyMixToCurrentPlayer();
        }
    }

    private void EnsurePlayers()
    {
        _playerA ??= GetNodeOrNull<AudioStreamPlayer>("MapMusicPlayerA");
        if (_playerA == null)
        {
            _playerA = new AudioStreamPlayer { Name = "MapMusicPlayerA" };
            AddChild(_playerA);
        }
        var playerAFinishedCallable = Callable.From(OnPlayerAFinished);
        if (_playerA.IsConnected(AudioStreamPlayer.SignalName.Finished, playerAFinishedCallable))
        {
            _playerA.Disconnect(AudioStreamPlayer.SignalName.Finished, playerAFinishedCallable);
        }
        _playerA.Connect(AudioStreamPlayer.SignalName.Finished, playerAFinishedCallable);

        _playerB ??= GetNodeOrNull<AudioStreamPlayer>("MapMusicPlayerB");
        if (_playerB == null)
        {
            _playerB = new AudioStreamPlayer { Name = "MapMusicPlayerB" };
            AddChild(_playerB);
        }
        var playerBFinishedCallable = Callable.From(OnPlayerBFinished);
        if (_playerB.IsConnected(AudioStreamPlayer.SignalName.Finished, playerBFinishedCallable))
        {
            _playerB.Disconnect(AudioStreamPlayer.SignalName.Finished, playerBFinishedCallable);
        }
        _playerB.Connect(AudioStreamPlayer.SignalName.Finished, playerBFinishedCallable);

        _currentPlayer ??= _playerA;
    }

    private void OnPlayerFinished(AudioStreamPlayer player)
    {
        if (player != _currentPlayer) return;
        if (_currentTrack == null) return;
        if (!ShouldLoopTrack(_currentTrack)) return;

        player.Play();
    }

    private void OnPlayerAFinished()
    {
        OnPlayerFinished(_playerA);
    }

    private void OnPlayerBFinished()
    {
        OnPlayerFinished(_playerB);
    }

    private bool ShouldLoopTrack(BattleMusicData track)
    {
        if (track == null) return false;
        return ForceLoopPlayback || track.Loop;
    }

    private void ConfigurePlayer(AudioStreamPlayer player, BattleMusicData track, float volumeDb)
    {
        player.Stream = track.Stream;
        player.VolumeDb = volumeDb;
        player.PitchScale = ResolveTrackPitch(track);
    }

    private void ReapplyMixToCurrentPlayer()
    {
        if (_currentPlayer == null || _currentTrack == null) return;
        _currentPlayer.VolumeDb = ResolveTrackVolume(_currentTrack);
        _currentPlayer.PitchScale = ResolveTrackPitch(_currentTrack);
    }

    private float ResolveTrackVolume(BattleMusicData track)
    {
        return (track?.VolumeDb ?? 0f) + RuntimeVolumeOffsetDb;
    }

    private float ResolveTrackPitch(BattleMusicData track)
    {
        float basePitch = track?.PitchScale ?? 1f;
        return Mathf.Max(0.1f, basePitch * RuntimePitchMultiplier);
    }

    private BattleMusicData GetRandomTrack()
    {
        var candidates = MusicLibrary.Where(t => t != null && t.Stream != null).ToList();
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        int totalWeight = 0;
        foreach (var track in candidates)
        {
            totalWeight += Mathf.Max(1, track.Weight);
        }
        if (totalWeight <= 0) return candidates[_rng.RandiRange(0, candidates.Count - 1)];

        int roll = _rng.RandiRange(0, totalWeight - 1);
        foreach (var track in candidates)
        {
            int w = Mathf.Max(1, track.Weight);
            if (roll < w) return track;
            roll -= w;
        }

        return candidates[0];
    }
}
