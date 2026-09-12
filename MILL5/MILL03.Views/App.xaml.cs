using Microsoft.Extensions.DependencyInjection;

namespace MILL03.Views; 
public partial class App : Application {
    public App() {
        InitializeComponent();

    }

    protected override Window CreateWindow(IActivationState? activationState) {
        // Explicitly create the OS Window and inject your singleton canvas
        return new Window(MainView.Instance) {
            Title = "Enterprise Dashboard"
        };
    }
}