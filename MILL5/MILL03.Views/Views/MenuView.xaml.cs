using MILL06.ViewModels;

namespace MILL03.Views.Views;

public partial class MenuView : ContentView
{
	public MenuView()
	{
		InitializeComponent();
	}

    private void MenuTreeView_ItemDoubleTapped(object sender, Syncfusion.Maui.TreeView.ItemDoubleTappedEventArgs e) {
        // 1. Ensure we only care about actual actionable items, not the "dbo" or "Person" grouping folders
        if (e.Node.Content is MenuItemViewModel clickedNode) {
            // 2. Grab the View's DataContext
            if (this.BindingContext is MenuViewModel vm) {
                // 3. Mutate the state. MainViewModel can observe this change!
                vm.SelectedMenuNode = clickedNode;
            }
        }
    }
}