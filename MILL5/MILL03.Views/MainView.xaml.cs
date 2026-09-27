using MILL03.Views.Controls;
using MILL06.ViewModels;

namespace MILL03.Views;

public partial class MainView : ContentPage {

    #region Constructors

    public MainView() {
        InitializeComponent();

        var viewModel =
            MainViewModel.Instance;

        BindingContext =
            viewModel;

        RootGrid.Children.Add(
            new RegionCell {
                RegionNode =
                    viewModel.Regions.RootRegionNode
            });

        Loaded += MainView_Loaded;
        Unloaded += MainView_Unloaded;
    }

    #endregion Constructors

    #region Lifecycle

    private void MainView_Loaded(
        object? sender, EventArgs e) {

        DragDropCoordinator.DroppedOutside +=
            DragDropCoordinator_DroppedOutside;
    }

    private void MainView_Unloaded(
        object? sender, EventArgs e) {

        DragDropCoordinator.DroppedOutside -=
            DragDropCoordinator_DroppedOutside;
    }

    #endregion Lifecycle

    #region Drag Drop

    private void DropTarget_Dropped(
        object? sender,
        DropTargetDroppedEventArgs e) {

        if (e.DragData is MenuNodeViewModel menuNode) {
            System.Diagnostics.Debug.WriteLine(
                $"Dropped: {menuNode.Title}");
        }
    }

    private void DragDropCoordinator_DroppedOutside(
        object? sender, EventArgs e) {

        Dispatcher.Dispatch(
            OpenFloatingWindow);
    }

    #endregion Drag Drop

    #region Windows

    private static void OpenFloatingWindow() {
        var page = new ContentPage {
            Title = "Floating Window",
            Content = new Grid {
                Children = {
                    new Label {
                        Text = "New Window",
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                }
            }
        };

        Application.Current?.OpenWindow(
            new Window(page));
    }

    #endregion Windows
}