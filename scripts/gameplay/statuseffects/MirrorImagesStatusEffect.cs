using Godot;

/// <summary>
/// Mirror Images doubles evasiveness and is represented visually via ghost after-images.
/// </summary>
[GlobalClass]
public partial class MirrorImagesStatusEffect : StatusEffect
{
    [Export(PropertyHint.Range, "1,10,0.1")]
    public float EvasionMultiplier { get; private set; } = 2.0f;

    public override void OnActionTargeted(ActionContext context, Node owner)
    {
        if (!StatusRuleUtils.IsOwnerTargetContext(context, owner)) return;
        if (!StatusRuleUtils.IsDamagingAction(context)) return;

        var damage = context.GetComponent<DamageComponent>();
        if (damage == null) return;

        float multiplier = Mathf.Max(1f, EvasionMultiplier);
        damage.Accuracy = Mathf.Clamp(Mathf.RoundToInt(damage.Accuracy / multiplier), 1, 100);
    }
}
