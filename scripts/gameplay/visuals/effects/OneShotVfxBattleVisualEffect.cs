using Godot;

[GlobalClass]
public partial class OneShotVfxBattleVisualEffect : BattleVisualEffect
{
    [Export(PropertyHint.ResourceType, "PackedScene")]
    public PackedScene VfxScene { get; private set; }

    [Export]
    public Vector3 VfxOffset { get; private set; } = Vector3.Zero;

    [Export]
    public bool AttachToOwner { get; private set; } = true;

    [Export]
    public bool RequireDamagingAction { get; private set; } = false;

    [Export]
    public bool RequireHitResult { get; private set; } = false;

    public override void OnEvent(BattleVisualEffectContext context)
    {
        if (context == null || VfxScene == null) return;

        if (RequireDamagingAction)
        {
            if (context.ActionContext == null || !StatusRuleUtils.IsDamagingAction(context.ActionContext)) return;
        }

        if (RequireHitResult)
        {
            if (context.ActionResult == null || !context.ActionResult.IsHit) return;
        }

        context.EmitOneShotVfx(VfxScene, VfxOffset, AttachToOwner);
    }
}
