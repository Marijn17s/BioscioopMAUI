using UIKit;

namespace BioscoopMAUI.Controls;

public partial class GlassPanel
{
    private const float CornerRadius = 28;

    partial void ApplyPlatformGlassEffect()
    {
        if (Handler?.PlatformView is not UIView platformView)
            return;

        foreach (var subview in platformView.Subviews)
        {
            if (subview is UIVisualEffectView)
            {
                return;
            }
        }

        platformView.BackgroundColor = UIColor.Clear;
        platformView.ClipsToBounds = true;
        platformView.Layer.CornerRadius = CornerRadius;
        platformView.Layer.MasksToBounds = true;

        var blurEffect = UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemUltraThinMaterialDark);
        var visualEffectView = new UIVisualEffectView(blurEffect)
        {
            Frame = platformView.Bounds,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            UserInteractionEnabled = false
        };
        visualEffectView.ClipsToBounds = true;
        visualEffectView.Layer.CornerRadius = CornerRadius;
        visualEffectView.Layer.MasksToBounds = true;

        platformView.InsertSubview(visualEffectView, 0);
    }
}