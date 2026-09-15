using MILL09.Models;
using Syncfusion.Maui.DataGrid;

namespace MILL03.Views.Views;

public partial class CustomersSyncFusionView : ContentView
{
	public CustomersSyncFusionView()
	{
		InitializeComponent();
	}

    private void SfDataGrid_OnAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e) {
        if (e.Column.MappingName == nameof(Customer.Rowguid)) {
            e.Column.Visible = false;
        }       
    }
}