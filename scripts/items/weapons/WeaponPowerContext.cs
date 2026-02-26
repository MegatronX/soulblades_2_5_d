using Godot;

/// <summary>
/// Runtime context used by weapon-power formulas.
/// This is intentionally extensible so formulas can use richer battle data over time.
/// </summary>
[GlobalClass]
public partial class WeaponPowerContext : RefCounted
{
    public Node Holder { get; private set; }
    public ActionContext ActionContext { get; private set; }
    public Node Target { get; private set; }
    public BattleMechanics BattleMechanics { get; private set; }
    public Node BattlefieldRoot { get; private set; }

    public static WeaponPowerContext Create(
        Node holder,
        ActionContext actionContext,
        Node target,
        BattleMechanics battleMechanics)
    {
        return new WeaponPowerContext
        {
            Holder = holder,
            ActionContext = actionContext,
            Target = target,
            BattleMechanics = battleMechanics,
            BattlefieldRoot = ResolveBattlefieldRoot(holder, actionContext, battleMechanics)
        };
    }

    private static Node ResolveBattlefieldRoot(
        Node holder,
        ActionContext actionContext,
        BattleMechanics battleMechanics)
    {
        if (battleMechanics != null)
        {
            return battleMechanics.GetParent();
        }

        if (actionContext?.Initiator != null)
        {
            return actionContext.Initiator.GetTree()?.CurrentScene;
        }

        return holder?.GetTree()?.CurrentScene;
    }
}
