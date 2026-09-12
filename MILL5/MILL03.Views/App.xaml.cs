using Microsoft.Maui.Controls;

namespace MILL03.Views;

public partial class App : Application {
    public App() {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState) {
        // Spawns a dedicated OS Window with its own isolated MainView instance
        return new Window(new MainView()) {
            Title = "Enterprise Dashboard"
        };
    }
}