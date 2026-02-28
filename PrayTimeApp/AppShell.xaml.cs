namespace Nooria
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(NoLocationPage), typeof(NoLocationPage));
#if IOS
            Navigated += (_, _) => PaintAll();
#endif
        }

#if IOS
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            PaintAll();
        }

        static readonly UIKit.UIColor AppBg = UIKit.UIColor.FromRGB(0x0D, 0x16, 0x23);

        static void PaintAll()
        {
            var root = GetRootViewController();
            if (root is null) return;

            // Paint every view controller whose view is already loaded.
            WalkAndPaint(root);

            // Also pre-paint the UINavigationController container view for EVERY tab,
            // including tabs the user has never visited yet (not in ChildViewControllers).
            // UINavigationController.View is a plain container UIView — force-loading it
            // here is safe and does NOT trigger the MAUI ContentPage lifecycle.
            var tabVc = FindTabBarController(root);
            if (tabVc is null) return;

            if (tabVc.IsViewLoaded && tabVc.View is not null)
                tabVc.View.BackgroundColor = AppBg;

            if (tabVc.ViewControllers is not null)
                foreach (var vc in tabVc.ViewControllers)
                    try { vc.View.BackgroundColor = AppBg; } catch { }
        }

        static void WalkAndPaint(UIKit.UIViewController vc)
        {
            if (vc.IsViewLoaded && vc.View is not null)
                vc.View.BackgroundColor = AppBg;
            foreach (var child in vc.ChildViewControllers)
                WalkAndPaint(child);
        }

        static UIKit.UIViewController? GetRootViewController()
        {
            foreach (var scene in UIKit.UIApplication.SharedApplication.ConnectedScenes)
                if (scene is UIKit.UIWindowScene ws)
                    foreach (var w in ws.Windows)
                        if (w.RootViewController is { } rvc) return rvc;
            return null;
        }

        static UIKit.UITabBarController? FindTabBarController(UIKit.UIViewController? vc)
        {
            if (vc is null) return null;
            if (vc is UIKit.UITabBarController tbc) return tbc;
            foreach (var child in vc.ChildViewControllers)
            {
                var found = FindTabBarController(child);
                if (found is not null) return found;
            }
            return null;
        }
#endif
    }
}
