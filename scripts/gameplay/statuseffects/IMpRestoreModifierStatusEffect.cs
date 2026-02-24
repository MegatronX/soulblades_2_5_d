using Godot;

/// <summary>
/// Optional status hook for modifying incoming MP restoration amounts.
/// </summary>
public interface IMpRestoreModifierStatusEffect
{
    int ModifyIncomingMpRestore(Node owner, StatusEffectManager manager, StatusEffectManager.StatusEffectInstance instance, int incomingAmount);
}
