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

    #region StartupViewModel

    /// <summary>
    /// Region content initially assigned to the root Region.
    /// </summary>
    private RegionBaseViewModel? _startupViewModel;
    public RegionBaseViewModel StartupViewModel =>
        _startupViewModel ??= MenuViewModel;

    #endregion StartupViewModel

    #region MainViewModel's Instance Singleton

    // Resolves directly from the global container, allowing test initialization to swap the provider.
    public static MainViewModel Instance =>
        global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<MainViewModel>();

    protected internal MainViewModel() {
        if (Regions.RootRegionNode.PayloadViewModel == null)
            Regions.RootRegionNode.PayloadViewModel = StartupViewModel;

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
        var targetNode = FindTargetNode(Regions.RootRegionNode);

        // Existing fallback behavior when there are no empty Regions.
        targetNode ??= GetReplaceableNode(
            Regions.ActiveRegionNode,
            Regions.RootRegionNode);

        if (targetNode == null) return;

        AssignMenuItemToNode(selectedNode, targetNode);
    }

    private bool AssignMenuItemToNode(MenuItemViewModel menuItem, RegionNode targetNode) {
        var viewModel = ResolveTargetViewModel(menuItem);
        if (viewModel == null) return false;

        // Temporary until NodeAssigning / NodeAssigned are implemented.
        if (!targetNode.CanBeReplaced) return false;

        targetNode.PayloadViewModel = viewModel;
        Regions.ActiveRegionNode = targetNode;

        return true;
    }

    #endregion Menu Selection

    #region Menu Drag Adapter

    /// <summary>
    /// Temporary adapter for the current Menu drag payload.
    ///
    /// MainViewModel participates only because it has application-global
    /// knowledge required to resolve TargetViewModelName. Region assignment
    /// policy and lifecycle orchestration belong to RegionNodesTree.
    /// </summary>
    public bool TryAssignDrop(object dragData, RegionNode targetNode) {
        if (dragData is not MenuItemViewModel menuItem)
            return false;

        var draggedViewModel = ResolveTargetViewModel(menuItem);
        if (draggedViewModel == null) return false;

        var sourceNode = Regions.GetRegionNode(MenuViewModel);
        if (sourceNode == null) return false;

        return Regions.AssignRegion(sourceNode, draggedViewModel, targetNode);
    }

    private BaseViewModel? ResolveTargetViewModel(MenuItemViewModel menuItem) {
        if (string.IsNullOrEmpty(menuItem.TargetViewModelName)) return null;

        var property = GetType().GetProperty(
            menuItem.TargetViewModelName,
            BindingFlags.Public | BindingFlags.Instance);

        return property?.GetValue(this) as BaseViewModel;
    }

    #endregion Menu Drag Adapter

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