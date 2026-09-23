using Microsoft.Extensions.DependencyInjection;
using MILL06.ViewModels.UIContracts;
using MILL09.Models;
using System.Reflection;

namespace MILL06.ViewModels;

public partial class MainViewModel : BaseViewModel {

    #region MainModel

    // Child properties pull from the locator instead of using 'new()', preserving full substitution.
    private MainModel? _mainModel;
    public MainModel MainModel =>
        _mainModel ??= global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<MainModel>();

    #endregion MainModel

    #region Regions

    // Temporary forwarding properties preserve existing MainViewModel bindings/callers
    // while RegionNodesTree owns the authoritative Region state.
    public RegionNode RootRegionNode => Regions.RootRegionNode;

    public RegionNode ActiveRegionNode {
        get => Regions.ActiveRegionNode ?? Regions.RootRegionNode;
        set {
            if (ReferenceEquals(Regions.ActiveRegionNode, value)) return;

            Regions.ActiveRegionNode = value;
            OnPropertyChanged();
        }
    }

    #region StartupViewModel

    /// <summary>
    /// Region content initially assigned to the root Region.
    /// </summary>
    private RegionBaseViewModel? _startupViewModel;
    public RegionBaseViewModel StartupViewModel =>
        _startupViewModel ??= MenuViewModel;

    #endregion StartupViewModel

    #endregion Regions

    #region MainViewModel's Instance Singleton

    // Resolves directly from the global container, allowing test initialization to swap the provider.
    public static MainViewModel Instance =>
        global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<MainViewModel>();

    protected internal MainViewModel() {
        if (RootRegionNode.PayloadViewModel == null)
            RootRegionNode.PayloadViewModel = StartupViewModel;

        MenuViewModel.PropertyChanged += MenuViewModel_PropertyChanged;
    }

    #endregion MainViewModel's Instance Singleton

    #region Menu Selection

    private void MenuViewModel_PropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e) {

        if (e.PropertyName != nameof(MenuViewModel.SelectedMenuNode)) return;

        var selectedNode = ((MenuViewModel)sender!).SelectedMenuNode;
        if (selectedNode == null || string.IsNullOrEmpty(selectedNode.TargetViewModelName)) return;

        // Prefer the oldest empty Region.
        var targetNode = FindTargetNode(RootRegionNode);

        // Existing fallback behavior when there are no empty Regions.
        targetNode ??= GetReplaceableNode(ActiveRegionNode, RootRegionNode);

        if (targetNode == null) return;

        TryAssignMenuItemToRegion(selectedNode, targetNode);
    }

    #endregion Menu Selection

    #region Region Assignment - Temporary Bridge

    /// <summary>
    /// Entry point used by the View tier after a physical drop occurs.
    ///
    /// DragData remains generic in the Views project. Interpretation of that
    /// data remains in the ViewModel tier.
    ///
    /// This transaction code is temporary until RegionAssigning and
    /// NodeAssigning are migrated to the agreed lifecycle architecture.
    /// </summary>
    public bool TryAssignDrop(object dragData, RegionNode targetRegionNode) {
        if (dragData is MenuItemViewModel menuItem)
            return TryAssignMenuItemToRegion(menuItem, targetRegionNode);

        // RegionNode-to-RegionNode movement will be added separately.
        return false;
    }

    private bool TryAssignMenuItemToRegion(MenuItemViewModel menuItem, RegionNode targetRegionNode) {
        var incomingViewModel = ResolveTargetViewModel(menuItem);
        if (incomingViewModel == null) return false;

        // A Menu item is a descriptor, not Region content.
        // Therefore this assignment has no source RegionNode.
        return TryAssignViewModelToRegion(null, targetRegionNode, incomingViewModel);
    }

    private BaseViewModel? ResolveTargetViewModel(MenuItemViewModel menuItem) {
        if (string.IsNullOrEmpty(menuItem.TargetViewModelName)) return null;

        var property = GetType().GetProperty(
            menuItem.TargetViewModelName,
            BindingFlags.Public | BindingFlags.Instance);

        return property?.GetValue(this) as BaseViewModel;
    }

    private bool TryAssignViewModelToRegion(RegionNode? sourceRegionNode,
        RegionNode targetRegionNode, BaseViewModel incomingViewModel) {

        if (!targetRegionNode.CanBeReplaced) return false;

        // TODO: Replace with the agreed lifecycle architecture:
        //   NodeAssigning     - ordinary Region assignment, target participant only.
        //   RegionAssigning   - drag/drop, source and target participants.
        //
        // Retained temporarily so the currently working assignment path is not broken.
        if (!RootRegionNode.RaiseRegionAssigning(sourceRegionNode, targetRegionNode, incomingViewModel))
            return false;

        targetRegionNode.PayloadViewModel = incomingViewModel;
        ActiveRegionNode = targetRegionNode;

        RootRegionNode.RaiseRegionAssigned(sourceRegionNode, targetRegionNode, incomingViewModel);
        return true;
    }

    #endregion Region Assignment - Temporary Bridge

    #region Target Selection

    private RegionNode? GetReplaceableNode(RegionNode? activeNode, RegionNode rootNode) {
        // If Active Region is a leaf and is not holding StartupViewModel,
        // it is the preferred existing destination.
        if (activeNode != null &&
            !activeNode.IsSplit &&
            activeNode.PayloadViewModel != StartupViewModel) {

            return activeNode;
        }

        return FindFirstUnprotectedLeaf(rootNode);
    }

    private RegionNode? FindFirstUnprotectedLeaf(RegionNode currentNode) {
        if (!currentNode.IsSplit)
            return currentNode.PayloadViewModel == StartupViewModel ? null : currentNode;

        if (currentNode.FirstChild != null) {
            var found = FindFirstUnprotectedLeaf(currentNode.FirstChild);
            if (found != null) return found;
        }

        if (currentNode.SecondChild != null) {
            var found = FindFirstUnprotectedLeaf(currentNode.SecondChild);
            if (found != null) return found;
        }

        return null;
    }

    private RegionNode? FindTargetNode(RegionNode rootNode) {
        RegionNode? oldestEmptyNode = null;
        FindOldestEmptyNode(rootNode, ref oldestEmptyNode);
        return oldestEmptyNode;
    }

    private void FindOldestEmptyNode(RegionNode currentNode, ref RegionNode? oldestEmptyNode) {
        if (!currentNode.IsSplit) {
            if (currentNode.PayloadViewModel == null &&
                (oldestEmptyNode == null ||
                 currentNode.CreationOrder < oldestEmptyNode.CreationOrder)) {

                oldestEmptyNode = currentNode;
            }

            return;
        }

        if (currentNode.FirstChild != null)
            FindOldestEmptyNode(currentNode.FirstChild, ref oldestEmptyNode);

        if (currentNode.SecondChild != null)
            FindOldestEmptyNode(currentNode.SecondChild, ref oldestEmptyNode);
    }

    #endregion Target Selection

    #region OnDisposing

    partial void OnDisposing() {
        MenuViewModel.PropertyChanged -= MenuViewModel_PropertyChanged;
    }

    #endregion OnDisposing
}