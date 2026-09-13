using Microsoft.Maui.Controls;

namespace MILL03.Views;

public partial class App : Application {
    public App() {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState) {
        // Spawns a dedicated OS Window with the SplashView as the initial content
        return new Window(new SplashView()) {
            Title = "MILL5 Dashboard"
        };
    }
}