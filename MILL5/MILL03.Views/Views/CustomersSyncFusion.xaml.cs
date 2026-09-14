using MILL09.Models;

namespace MILL03.Views.Views;

public partial class CustomersSyncFusion : ContentView
{
	public CustomersSyncFusion()
	{
		InitializeComponent();
	}

    private async void OnLoadCustomersClicked(object sender, EventArgs e) {
        await Customer.LoadAllAsync();
    }
}