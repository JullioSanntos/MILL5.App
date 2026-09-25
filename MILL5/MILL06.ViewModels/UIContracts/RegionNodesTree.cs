using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using MILL06.ViewModels;

namespace MILL06.ViewModels.UIContracts;

/// <summary>
/// Owns the application's RegionNode tree, its active Region, tree queries,
/// and invariant Region assignment infrastructure.
/// </summary>
public partial class RegionNodesTree : ObservableObject {

    #region Instance Singleton

    public static RegionNodesTree Instance =>
        global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<RegionNodesTree>();

    protected internal RegionNodesTree() {
        RootRegionNode = new RegionNode();
        ActiveRegionNode = RootRegionNode;
    }

    #endregion Instance Singleton

    #region Root Region

    [ObservableProperty]
    private RegionNode _rootRegionNode;

    #endregion Root Region

    #region events
    public event EventHandler? TreeChanged;

    public void NotifyTreeChanged() {
        TreeChanged?.Invoke(this, EventArgs.Empty);
    }
    #endregion events

    #region Active Region

    [ObservableProperty]
    private RegionNode? _activeRegionNode;

    #endregion Active Region

    #region Queries

    /// <summary>
    /// Gets every RegionNode currently presenting the specified ViewModel instance.
    /// Instance identity is used intentionally.
    /// </summary>
    public IReadOnlyList<RegionNode> GetRegionNodes(BaseViewModel viewModel) {
        ArgumentNullException.ThrowIfNull(viewModel);

        var regionNodes = new List<RegionNode>();
        FindRegionNodes(RootRegionNode, viewModel, regionNodes);

        return regionNodes;
    }

    /// <summary>
    /// Gets the unambiguous Region presentation for a ViewModel.
    ///
    /// A single presentation is returned directly. When multiple presentations
    /// exist, ActiveRegionNode is returned only when it presents that ViewModel.
    /// Otherwise the result is null because the source is ambiguous.
    /// </summary>
    public RegionNode? GetRegionNode(BaseViewModel viewModel) {
        var regionNodes = GetRegionNodes(viewModel);

        if (regionNodes.Count == 0) return null;
        if (regionNodes.Count == 1) return regionNodes[0];

        if (ActiveRegionNode != null) {
            foreach (var regionNode in regionNodes) {
                if (ReferenceEquals(regionNode, ActiveRegionNode))
                    return regionNode;
            }
        }

        return null;
    }

    private static void FindRegionNodes(RegionNode node, BaseViewModel viewModel,
        List<RegionNode> regionNodes) {

        if (ReferenceEquals(node.PayloadViewModel, viewModel))
            regionNodes.Add(node);

        if (node.FirstChild != null)
            FindRegionNodes(node.FirstChild, viewModel, regionNodes);

        if (node.SecondChild != null)
            FindRegionNodes(node.SecondChild, viewModel, regionNodes);
    }

    #endregion Queries

    #region Assignment Target Queries

    /// <summary>
    /// Gets the preferred Region destination for an assignment.
    ///
    /// The oldest empty Region is preferred. When no empty Region exists,
    /// the active leaf is preferred unless it contains the protected ViewModel.
    /// Otherwise the first unprotected leaf is returned.
    /// </summary>
    public RegionNode? GetPreferredTargetNode(BaseViewModel protectedViewModel) {
        ArgumentNullException.ThrowIfNull(protectedViewModel);

        var targetNode = FindOldestEmptyNode(RootRegionNode);

        targetNode ??= GetReplaceableNode(
            ActiveRegionNode,
            RootRegionNode,
            protectedViewModel);

        return targetNode;
    }

    private static RegionNode? GetReplaceableNode(
        RegionNode? activeNode,
        RegionNode rootNode,
        BaseViewModel protectedViewModel) {

        if (activeNode != null &&
            !activeNode.IsSplit &&
            !ReferenceEquals(activeNode.PayloadViewModel, protectedViewModel)) {

            return activeNode;
        }

        return FindFirstUnprotectedLeaf(
            rootNode,
            protectedViewModel);
    }

    private static RegionNode? FindFirstUnprotectedLeaf(
        RegionNode currentNode,
        BaseViewModel protectedViewModel) {

        if (!currentNode.IsSplit) {
            return ReferenceEquals(
                currentNode.PayloadViewModel,
                protectedViewModel)
                    ? null
                    : currentNode;
        }

        if (currentNode.FirstChild != null) {
            var found = FindFirstUnprotectedLeaf(
                currentNode.FirstChild,
                protectedViewModel);

            if (found != null) return found;
        }

        if (currentNode.SecondChild != null) {
            var found = FindFirstUnprotectedLeaf(
                currentNode.SecondChild,
                protectedViewModel);

            if (found != null) return found;
        }

        return null;
    }

    private static RegionNode? FindOldestEmptyNode(RegionNode rootNode) {
        RegionNode? oldestEmptyNode = null;
        FindOldestEmptyNode(rootNode, ref oldestEmptyNode);

        return oldestEmptyNode;
    }

    private static void FindOldestEmptyNode(
        RegionNode currentNode,
        ref RegionNode? oldestEmptyNode) {

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

    #endregion Assignment Target Queries

    #region Node Assignment

    /// <summary>
    /// Assigns a ViewModel to a Region outside the drag/drop lifecycle.
    /// </summary>
    public bool AssignNode(BaseViewModel viewModel, RegionNode targetNode) {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(targetNode);

        if (!targetNode.CanBeReplaced)
            return false;

        targetNode.PayloadViewModel = viewModel;
        ActiveRegionNode = targetNode;

        NotifyTreeChanged();

        return true;
    }

    #endregion Node Assignment

    #region Region Assignment
    /// <summary>
    /// Performs the invariant lifecycle for a Region drag/drop assignment.
    ///
    /// Each distinct participating RegionBaseViewModel is consulted once.
    /// The target is consulted first, a distinct source second, and the
    /// dragged ViewModel last. Any participant may cancel the assignment.
    ///
    /// When the source Region contains the ViewModel being transported,
    /// a successful assignment is a MOVE and the source Region is cleared.
    /// Specialized sources such as MenuViewModel remain unchanged because
    /// they transport another ViewModel rather than themselves.
    /// </summary>
    public bool AssignRegion(RegionNode sourceNode, BaseViewModel incomingViewModel
        , RegionNode targetNode, bool retainSource = false) {

        ArgumentNullException.ThrowIfNull(sourceNode);
        ArgumentNullException.ThrowIfNull(incomingViewModel);
        ArgumentNullException.ThrowIfNull(targetNode);

        if (ReferenceEquals(sourceNode, targetNode)) { return false; }

        var context = new RegionAssigningContext(sourceNode, incomingViewModel, targetNode);

        var targetViewModel = targetNode.PayloadViewModel as RegionBaseViewModel;

        var sourceViewModel = sourceNode.PayloadViewModel as RegionBaseViewModel;

        var draggedRegionViewModel = incomingViewModel as RegionBaseViewModel;

        // Capture this before changing either node.
        var isMove = !retainSource && ReferenceEquals(sourceNode.PayloadViewModel, incomingViewModel);

        // Target gets first refusal unless it is also the dragged ViewModel.
        // In that unusual case it is deferred so the dragged participant
        // retains the final decision.
        if (targetViewModel != null && !ReferenceEquals(targetViewModel, draggedRegionViewModel)) {
            targetViewModel.OnRegionAssigning(context);
            if (context.Cancel) return false;
        }

        // A distinct source participant is consulted second.
        if (sourceViewModel != null &&
            !ReferenceEquals(sourceViewModel, targetViewModel) &&
            !ReferenceEquals(sourceViewModel, draggedRegionViewModel)) {

            sourceViewModel.OnRegionAssigning(context);
            if (context.Cancel) return false;
        }

        // The ViewModel being transported always gets the final decision.
        if (draggedRegionViewModel != null) {
            draggedRegionViewModel.OnRegionAssigning(context);
            if (context.Cancel) return false;
        }

        targetNode.PayloadViewModel = incomingViewModel;

        if (isMove) { sourceNode.PayloadViewModel = null; }

        ActiveRegionNode = targetNode;

        NotifyTreeChanged();

        RootRegionNode.RaiseRegionAssigned(sourceNode, targetNode, incomingViewModel);

        return true;
    }
    #endregion Region Assignment

}