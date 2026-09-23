using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MILL06.ViewModels.UIContracts;
using MILL80.Infrastructure;

namespace MILL06.ViewModels;

[Register]
public partial class MenuViewModel {

    #region Selection

    [ObservableProperty]
    private MenuItemViewModel? _selectedMenuNode;

    #endregion Selection

    #region Region Assignment Lifecycle

    protected internal override void OnRegionAssigning(RegionAssigningContext context) {
        var isSource = ReferenceEquals(context.SourceNode.PayloadViewModel, this);
        var isDragged = ReferenceEquals(context.DraggedViewModel, this);
        var isTarget = ReferenceEquals(context.TargetNode.PayloadViewModel, this);

        // MenuViewModel is a specialized source: it transports the ViewModel
        // represented by a Menu item rather than transporting itself.
        if (isSource && !isDragged && !isTarget) {
            var callbackResult = RegionAssigningCallback?.Invoke(context);

            if (callbackResult.HasValue)
                context.Cancel = !callbackResult.Value;

            return;
        }

        base.OnRegionAssigning(context);
    }

    #endregion Region Assignment Lifecycle

    #region Commands

    [RelayCommand]
    private async Task LoadMenuItemsAsync() {
        OnPropertyChanged(nameof(MenuItems));
    }

    #endregion Commands
}