using MILL06.ViewModels;

namespace MILL03.Views;

public partial class MainView : ContentPage
{
    private static readonly Lazy<MainView> _instance =
        new Lazy<MainView>(() => new MainView());

    public static MainView Instance => _instance.Value;

    private MainView() {
        InitializeComponent();

        BindingContext = MainViewModel.Instance;
    }
}