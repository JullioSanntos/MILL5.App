using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using MILL06.ViewModels.UIContracts;
using MILL09.Models;
using System.Reflection;
using System.Xml.Linq;

namespace MILL06.ViewModels;

public partial class MainViewModel : BaseViewModel, IDisposable {

    #region RegionManager properties

    #region MainModel

    // Child properties pull from the locator instead of using 'new()', preserving full substitution.
    private MainModel? _mainModel;
    public MainModel MainModel =>
        _mainModel ??= global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<MainModel>();

    #endregion MainModel

    #region RootRegionNode

    // The root node of the entire layout tree.
    private RegionNode? _rootRegionNode;
    public RegionNode RootRegionNode {
        get {
            if (_rootRegionNode != null) return _rootRegionNode;

            RootRegionNode = new RegionNode {
                PayloadViewModel = StartupViewModel
            };

            return _rootRegionNode!;
        }
        set {
            if (_rootRegionNode != null) {
                _rootRegionNode.NodeChanging -= RootRegionNode_NodeChanging;
                _rootRegionNode.RegionAssigning -= RootRegionNode_RegionAssigning;
            }

            _rootRegionNode = value;

            if (_rootRegionNode != null) {
                _rootRegionNode.NodeChanging += RootRegionNode_NodeChanging;
                _rootRegionNode.RegionAssigning += RootRegionNode_RegionAssigning;
            }
        }
    }

    private void RootRegionNode_NodeChanging(object? sender, RegionNodeChangingEventArgs e) {
        // Temporary testing:
        System.Diagnostics.Debug.WriteLine($"RegionNode {e.NodeId}: {e.Action}");
    }

    private void RootRegionNode_RegionAssigning(object? sender, RegionAssigningEventArgs e) {
        // Candidate-specific assignment policy belongs here or in handlers subscribed here.
        //
        // e.Cancel = true;
    }

    #endregion RootRegionNode

    #region ActiveRegionNode

    // Active node tracking for target rules (Active preferred, otherwise First Empty).
    private RegionNode? _activeRegionNode;
    public RegionNode ActiveRegionNode {
        get {
            if (_activeRegionNode != null) return _activeRegionNode;

            ActiveRegionNode = RootRegionNode;

            return _activeRegionNode!;
        }
        set {
            if (ReferenceEquals(_activeRegionNode, value)) return;

            _activeRegionNode = value;
            OnPropertyChanged();
        }
    }

    #endregion ActiveRegionNode

    #region StartupViewModel

    /// <summary>
    /// Region content initially assigned to the root Region.
    ///
    /// The startup ViewModel participates in the Region lifecycle even though
    /// its initial layout position may normally prevent it from being replaced
    /// or moved.
    /// </summary>
    private RegionBaseViewModel? _startupViewModel;
    public RegionBaseViewModel StartupViewModel =>
        _startupViewModel ??= MenuViewModel;

    #endregion StartupViewModel

    #endregion RegionManager properties

    #region MainViewModel's Instance Singleton

    // Resolves directly from the global container, allowing test initialization to swap the provider.
    public static MainViewModel Instance =>
        global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<MainViewModel>();

    protected internal MainViewModel() {
        MenuViewModel.PropertyChanged += MenuViewModel_PropertyChanged;
    }

    private void MenuViewModel_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e) {

        if (e.PropertyName != nameof(MenuViewModel.SelectedMenuNode)) return;

        var selectedNode = ((MenuViewModel)sender!).SelectedMenuNode;

        if (selectedNode == null ||
            string.IsNullOrEmpty(selectedNode.TargetViewModelName)) {

            return;
        }

        // Prefer the oldest empty Region.
        var targetNode = FindTargetNode(RootRegionNode);

        // Existing fallback behavior when there are no empty Regions.
        targetNode ??= GetReplaceableNode(ActiveRegionNode, RootRegionNode);

        if (targetNode == null) return;

        TryAssignMenuItemToRegion(selectedNode, targetNode);
    }

    #endregion MainViewModel's Instance Singleton

    #region Region Assignment

    /// <summary>
    /// Entry point used by the View tier after a physical drop occurs.
    ///
    /// DragData remains generic in the Views project. Interpretation of that
    /// data and assignment policy remain in the ViewModel tier.
    /// </summary>
    public bool TryAssignDrop(object dragData, RegionNode targetRegionNode) {

        if (dragData is MenuItemViewModel menuItem) {
            return TryAssignMenuItemToRegion(
                menuItem,
                targetRegionNode);
        }

        // RegionNode-to-RegionNode movement will be added separately.
        return false;
    }

    private bool TryAssignMenuItemToRegion(MenuItemViewModel menuItem, RegionNode targetRegionNode) {

        var incomingViewModel =
            ResolveTargetViewModel(menuItem);

        if (incomingViewModel == null)
            return false;

        // A Menu item is a descriptor, not Region content.
        // Therefore this assignment has no source RegionNode.
        return TryAssignViewModelToRegion(
            null,
            targetRegionNode,
            incomingViewModel);
    }

    private BaseViewModel? ResolveTargetViewModel(
        MenuItemViewModel menuItem) {

        if (string.IsNullOrEmpty(menuItem.TargetViewModelName))
            return null;

        var property = GetType().GetProperty(
            menuItem.TargetViewModelName,
            BindingFlags.Public | BindingFlags.Instance);

        return property?.GetValue(this) as BaseViewModel;
    }

    private bool TryAssignViewModelToRegion(
        RegionNode? sourceRegionNode,
        RegionNode targetRegionNode,
        BaseViewModel incomingViewModel) {

        // Target-only capability.
        if (!targetRegionNode.CanBeReplaced)
            return false;

        // Final source + target compatibility gate.
        if (!RootRegionNode.RaiseRegionAssigning(
                sourceRegionNode,
                targetRegionNode,
                incomingViewModel)) {

            return false;
        }

        targetRegionNode.PayloadViewModel =
            incomingViewModel;

        ActiveRegionNode =
            targetRegionNode;

        RootRegionNode.RaiseRegionAssigned(
            sourceRegionNode,
            targetRegionNode,
            incomingViewModel);

        return true;
    }

    #endregion Region Assignment

    #region Target Selection

    private RegionNode? GetReplaceableNode(
        RegionNode? activeNode,
        RegionNode rootNode) {

        // If Active Node is a leaf and is not holding StartupViewModel,
        // it is the preferred existing destination.
        if (activeNode != null &&
            !activeNode.IsSplit &&
            activeNode.PayloadViewModel != StartupViewModel) {

            return activeNode;
        }

        // Otherwise find an unprotected leaf.
        return FindFirstUnprotectedLeaf(rootNode);
    }

    private RegionNode? FindFirstUnprotectedLeaf(
        RegionNode currentNode) {

        if (!currentNode.IsSplit) {
            return currentNode.PayloadViewModel == StartupViewModel
                ? null
                : currentNode;
        }

        if (currentNode.FirstChild != null) {
            var found =
                FindFirstUnprotectedLeaf(
                    currentNode.FirstChild);

            if (found != null)
                return found;
        }

        if (currentNode.SecondChild != null) {
            var found =
                FindFirstUnprotectedLeaf(
                    currentNode.SecondChild);

            if (found != null)
                return found;
        }

        return null;
    }

    private RegionNode? FindTargetNode(
        RegionNode rootNode) {

        RegionNode? oldestEmptyNode = null;

        FindOldestEmptyNode(
            rootNode,
            ref oldestEmptyNode);

        return oldestEmptyNode;
    }

    private void FindOldestEmptyNode(
        RegionNode currentNode,
        ref RegionNode? oldestEmptyNode) {

        if (!currentNode.IsSplit) {
            if (currentNode.PayloadViewModel == null &&
                (oldestEmptyNode == null ||
                 currentNode.CreationOrder <
                 oldestEmptyNode.CreationOrder)) {

                oldestEmptyNode =
                    currentNode;
            }

            return;
        }

        if (currentNode.FirstChild != null) {
            FindOldestEmptyNode(
                currentNode.FirstChild,
                ref oldestEmptyNode);
        }

        if (currentNode.SecondChild != null) {
            FindOldestEmptyNode(
                currentNode.SecondChild,
                ref oldestEmptyNode);
        }
    }

    #endregion Target Selection

    #region CreationOrder

    private static long _nextCreationOrder;
    public long CreationOrder { get; } =
        Interlocked.Increment(ref _nextCreationOrder);

    #endregion CreationOrder

    #region OnDisposing

    partial void OnDisposing() {
        if (_rootRegionNode != null) {
            _rootRegionNode.NodeChanging -=
                RootRegionNode_NodeChanging;

            _rootRegionNode.RegionAssigning -=
                RootRegionNode_RegionAssigning;
        }

        MenuViewModel.PropertyChanged -=
            MenuViewModel_PropertyChanged;

        GC.SuppressFinalize(this);
    }

    #endregion OnDisposing
}