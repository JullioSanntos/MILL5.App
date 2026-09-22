namespace MILL03.Views;

public partial class SplashView : ContentPage {
    public SplashView() {
        InitializeComponent();
    }

    protected override async void OnAppearing() {
        base.OnAppearing();

        // Non-blocking delay to keep the splash screen visible
        await Task.Delay(300);

        // Since you override CreateWindow, swap the Page directly on the active Window
        if (this.Window != null) {
            this.Window.Page = new MainView();
        }
    }
}