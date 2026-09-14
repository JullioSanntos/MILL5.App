using MILL09.Models;
using Syncfusion.Maui.DataGrid;

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

    private void SfDataGrid_OnAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e) {
        if (e.Column.MappingName == nameof(Customer.Rowguid)) {
            e.Column.Visible = false;
        }       
    }
}