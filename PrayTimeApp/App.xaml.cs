using PrayTimeApp.Services;

namespace PrayTimeApp
{
    public partial class App : Application
    {
        public App()
        {
            LocalizationService.LoadSavedLanguage();
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}