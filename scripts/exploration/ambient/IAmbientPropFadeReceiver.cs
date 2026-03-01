/// <summary>
/// Optional hook for ambient props that want custom fade application.
/// </summary>
public interface IAmbientPropFadeReceiver
{
    void SetAmbientFadeMultiplier(float alphaMultiplier);
}
