using Godot;

/// <summary>
/// Sets exploration music to a specific track or a random track from the map music controller.
/// Optional per-trigger volume/pitch overrides are applied as runtime mix on the controller.
/// </summary>
[GlobalClass]
public partial class SetExplorationMusicInteractionEffect : InteractionEffect
{
    [Export]
    public BattleMusicData Track { get; private set; }

    [Export]
    public bool UseRandomTrackFromLibrary { get; private set; } = false;

    [Export]
    public bool UseCrossfade { get; private set; } = true;

    [Export(PropertyHint.Range, "-1,8,0.01")]
    public float CrossfadeSecondsOverride { get; private set; } = -1f;

    [Export]
    public bool OverrideRuntimeMix { get; private set; } = false;

    [Export(PropertyHint.Range, "-24,24,0.1")]
    public float RuntimeVolumeOffsetDb { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0.1,4,0.01")]
    public float RuntimePitchMultiplier { get; private set; } = 1f;

    public override void Execute(ExplorationInteractionContext context, Node source)
    {
        var controller = context?.MapController?.MusicController;
        if (controller == null)
        {
            GD.PrintErr("[SetExplorationMusicInteractionEffect] ExplorationMusicController not found.");
            return;
        }

        if (OverrideRuntimeMix)
        {
            controller.SetRuntimeMix(RuntimeVolumeOffsetDb, RuntimePitchMultiplier, false);
        }

        if (UseRandomTrackFromLibrary)
        {
            controller.PlayRandomTrack(UseCrossfade);
            return;
        }

        if (Track != null)
        {
            controller.PlayTrack(Track, UseCrossfade, CrossfadeSecondsOverride);
        }
    }
}
