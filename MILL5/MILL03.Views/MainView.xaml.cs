using MILL03.Views.Controls;
using MILL06.ViewModels;

namespace MILL03.Views;

public partial class MainView : ContentPage
{
	public MainView()
	{
		InitializeComponent();
		BindingContext = MainViewModel.Instance;
	}

    private void DropTarget_Dropped(
        object? sender,
        DropTargetDroppedEventArgs e) {

        if (e.DragData is MenuNodeViewModel menuNode) {
            System.Diagnostics.Debug.WriteLine(
                $"Dropped: {menuNode.Title}");
        }
    }
}