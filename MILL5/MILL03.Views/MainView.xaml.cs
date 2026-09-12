using MILL06.ViewModels;

namespace MILL03.Views;

public partial class MainView : ContentPage
{
	public MainView()
	{
		InitializeComponent();
		BindingContext = MainViewModel.Instance;
	}
}