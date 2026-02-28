using Godot;

/// <summary>
/// Adjusts map music runtime volume/pitch mix on the exploration music controller.
/// </summary>
[GlobalClass]
public partial class AdjustExplorationMusicMixInteractionEffect : InteractionEffect
{
    [Export]
    public bool ResetMix { get; private set; } = false;

    [Export]
    public bool AbsoluteValues { get; private set; } = true;

    [Export(PropertyHint.Range, "-24,24,0.1")]
    public float VolumeOffsetDb { get; private set; } = 0f;

    [Export(PropertyHint.Range, "-2,2,0.01")]
    public float PitchMultiplierDelta { get; private set; } = 0f;

    [Export(PropertyHint.Range, "0.1,4,0.01")]
    public float PitchMultiplier { get; private set; } = 1f;

    public override void Execute(ExplorationInteractionContext context, Node source)
    {
        var controller = context?.MapController?.MusicController;
        if (controller == null)
        {
            GD.PrintErr("[AdjustExplorationMusicMixInteractionEffect] ExplorationMusicController not found.");
            return;
        }

        if (ResetMix)
        {
            controller.ResetRuntimeMix();
            return;
        }

        if (AbsoluteValues)
        {
            controller.SetRuntimeMix(VolumeOffsetDb, PitchMultiplier);
            return;
        }

        controller.AdjustRuntimeMix(VolumeOffsetDb, PitchMultiplierDelta);
    }
}
