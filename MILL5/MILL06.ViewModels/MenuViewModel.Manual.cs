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

    partial void OnSelectedMenuNodeChanged(MenuItemViewModel? value) {
        var targetViewModel = value?.TargetViewModel;
        if (targetViewModel == null) return;

        var targetNode = Regions.GetPreferredTargetNode(this);
        if (targetNode == null) return;

        Regions.AssignNode(targetViewModel, targetNode);
    }

    #endregion Selection

    #region Region Capabilities

    private bool _regionTreeSubscribed;

    public override bool CanBeClosed {
        get {
            EnsureRegionTreeSubscription();
            return Regions.GetRegionNodes(this).Count > 1;
        }
    }

    private void EnsureRegionTreeSubscription() {
        if (_regionTreeSubscribed) return;

        Regions.TreeChanged += Regions_TreeChanged;
        _regionTreeSubscribed = true;
    }

    private void Regions_TreeChanged(object? sender, EventArgs e) {
        OnPropertyChanged(nameof(CanBeClosed));
    }

    #endregion Region Capabilities

    //#region constructors
    //public MenuViewModel() {
    //    Regions.TreeChanged += Regions_TreeChanged;
    //}
    //#endregion constructors

    #region Region Assignment Lifecycle

    protected internal override void OnRegionAssigning(RegionAssigningContext context) {
        var isSource = ReferenceEquals(context.SourceNode.PayloadViewModel, this);
        var isDragged = ReferenceEquals(context.IncomingViewModel, this);
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