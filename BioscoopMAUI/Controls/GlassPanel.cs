namespace BioscoopMAUI.Controls;

public partial class GlassPanel : Border
{
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        ApplyPlatformGlassEffect();
    }

    partial void ApplyPlatformGlassEffect();
}
