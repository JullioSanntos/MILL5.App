using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using MILL06.ViewModels;

namespace MILL06.ViewModels.UIContracts;

public readonly record struct RegionSplitResult(
    RegionNode BranchNode,
    RegionNode EmptyNode);

public readonly record struct RegionMoveResult(
    RegionNode EmptySourceNode,
    RegionNode ReplacedTargetNode);

/// <summary>
/// Owns the application's RegionNode tree, its active Region, tree queries,
/// topology operations, and invariant Region assignment infrastructure.
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

    #region Events

    public event EventHandler? TreeChanged;

    public void NotifyTreeChanged() {
        TreeChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion Events

    #region Active Region

    [ObservableProperty]
    private RegionNode? _activeRegionNode;

    #endregion Active Region

    #region Topology

    /// <summary>
    /// Splits a leaf without changing its identity.
    /// A structural branch is inserted above the existing node and a new empty sibling is created.
    /// </summary>
    public RegionSplitResult SplitNode(
        RegionNode node, SplitOrientation orientation, double firstWeight) {

        ArgumentNullException.ThrowIfNull(node);

        if (node.IsSplit)
            throw new InvalidOperationException("Only leaf RegionNodes may be split.");

        var oldParent = node.Parent;

        var branchNode = new RegionNode {
            FirstChildWeight = firstWeight,
            SecondChildWeight = 1.0 - firstWeight
        };

        var emptyNode = new RegionNode();

        ReplaceNode(oldParent, node, branchNode);

        branchNode.FirstChild = node;
        branchNode.SecondChild = emptyNode;
        branchNode.Orientation = orientation;

        ActiveRegionNode = emptyNode;

        return new RegionSplitResult(branchNode, emptyNode);
    }

    /// <summary>
    /// Closes a leaf by removing its structural parent and splicing its
    /// surviving sibling into the parent's former position.
    /// </summary>
    public RegionNode? CloseNode(RegionNode closedNode) {
        ArgumentNullException.ThrowIfNull(closedNode);

        var branchNode = closedNode.Parent;
        if (branchNode == null || !branchNode.IsSplit) return null;

        RegionNode? survivingNode;

        if (ReferenceEquals(branchNode.FirstChild, closedNode))
            survivingNode = branchNode.SecondChild;
        else if (ReferenceEquals(branchNode.SecondChild, closedNode))
            survivingNode = branchNode.FirstChild;
        else
            return null;

        if (survivingNode == null) return null;

        var parentNode = branchNode.Parent;

        ReplaceNode(parentNode, branchNode, survivingNode);

        branchNode.FirstChild = null;
        branchNode.SecondChild = null;
        branchNode.Orientation = null;

        if (ReferenceEquals(ActiveRegionNode, closedNode) ||
            ReferenceEquals(ActiveRegionNode, branchNode)) {

            ActiveRegionNode = FindFirstLeaf(survivingNode);
        }

        return survivingNode;
    }

    /// <summary>
    /// Moves an existing Region presentation to another leaf position.
    ///
    /// The source RegionNode retains its identity. Its former position receives
    /// a new empty RegionNode, while the target RegionNode leaves the tree.
    /// </summary>
    public RegionMoveResult? MoveRegion(
        RegionNode sourceNode, RegionNode targetNode) {

        ArgumentNullException.ThrowIfNull(sourceNode);
        ArgumentNullException.ThrowIfNull(targetNode);

        if (ReferenceEquals(sourceNode, targetNode)) return null;
        if (sourceNode.IsSplit || targetNode.IsSplit) return null;
        if (!sourceNode.CanBeDragged || !targetNode.CanBeReplaced) return null;

        var incomingViewModel = sourceNode.PayloadViewModel;
        if (incomingViewModel == null) return null;

        if (!CanAssignRegion(sourceNode, incomingViewModel, targetNode))
            return null;

        var sourceParent = sourceNode.Parent;
        var targetParent = targetNode.Parent;
        var emptySourceNode = new RegionNode();

        ReplaceNode(sourceParent, sourceNode, emptySourceNode);
        ReplaceNode(targetParent, targetNode, sourceNode);

        ActiveRegionNode = sourceNode;

        NotifyTreeChanged();

        RootRegionNode.RaiseRegionAssigned(
            sourceNode, targetNode, incomingViewModel);

        return new RegionMoveResult(
            emptySourceNode,
            targetNode);
    }

    private void ReplaceNode(
        RegionNode? parentNode, RegionNode oldNode, RegionNode newNode) {

        if (parentNode == null) {
            if (!ReferenceEquals(RootRegionNode, oldNode))
                throw new InvalidOperationException(
                    "The RegionNode being replaced is not the current root.");

            RootRegionNode = newNode;
            newNode.Parent = null;

            return;
        }

        if (ReferenceEquals(parentNode.FirstChild, oldNode)) {
            parentNode.FirstChild = newNode;
            return;
        }

        if (ReferenceEquals(parentNode.SecondChild, oldNode)) {
            parentNode.SecondChild = newNode;
            return;
        }

        throw new InvalidOperationException(
            "The RegionNode being replaced is not a child of its Parent.");
    }

    private static RegionNode FindFirstLeaf(RegionNode node) {
        if (!node.IsSplit) return node;

        if (node.FirstChild != null)
            return FindFirstLeaf(node.FirstChild);

        return FindFirstLeaf(node.SecondChild!);
    }

    #endregion Topology

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

    private static void FindRegionNodes(
        RegionNode node, BaseViewModel viewModel, List<RegionNode> regionNodes) {

        if (ReferenceEquals(node.PayloadViewModel, viewModel))
            regionNodes.Add(node);

        if (node.FirstChild != null)
            FindRegionNodes(node.FirstChild, viewModel, regionNodes);

        if (node.SecondChild != null)
            FindRegionNodes(node.SecondChild, viewModel, regionNodes);
    }

    #endregion Queries

    #region Assignment Target Queries

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

        return FindFirstUnprotectedLeaf(rootNode, protectedViewModel);
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

    public bool AssignNode(BaseViewModel viewModel, RegionNode targetNode) {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(targetNode);

        if (!targetNode.CanBeReplaced) return false;

        targetNode.PayloadViewModel = viewModel;
        ActiveRegionNode = targetNode;

        NotifyTreeChanged();

        return true;
    }

    #endregion Node Assignment

    #region Region Assignment

    /// <summary>
    /// Assigns another presentation of a ViewModel to a target Region.
    /// The source Region remains unchanged.
    ///
    /// Region MOVE uses MoveRegion because MOVE preserves RegionNode and
    /// presentation identity.
    /// </summary>
    public bool AssignRegion(
        RegionNode sourceNode,
        BaseViewModel incomingViewModel,
        RegionNode targetNode) {

        ArgumentNullException.ThrowIfNull(sourceNode);
        ArgumentNullException.ThrowIfNull(incomingViewModel);
        ArgumentNullException.ThrowIfNull(targetNode);

        if (ReferenceEquals(sourceNode, targetNode)) return false;
        if (!targetNode.CanBeReplaced) return false;

        if (!CanAssignRegion(sourceNode, incomingViewModel, targetNode))
            return false;

        targetNode.PayloadViewModel = incomingViewModel;
        ActiveRegionNode = targetNode;

        NotifyTreeChanged();

        RootRegionNode.RaiseRegionAssigned(
            sourceNode,
            targetNode,
            incomingViewModel);

        return true;
    }

    /// <summary>
    /// Runs the invariant pre-assignment lifecycle.
    /// Each distinct participant is consulted once in target/source/incoming order.
    /// </summary>
    private static bool CanAssignRegion(
        RegionNode sourceNode,
        BaseViewModel incomingViewModel,
        RegionNode targetNode) {

        var context = new RegionAssigningContext(
            sourceNode,
            incomingViewModel,
            targetNode);

        var targetViewModel =
            targetNode.PayloadViewModel as RegionBaseViewModel;

        var sourceViewModel =
            sourceNode.PayloadViewModel as RegionBaseViewModel;

        var incomingRegionViewModel =
            incomingViewModel as RegionBaseViewModel;

        if (targetViewModel != null &&
            !ReferenceEquals(targetViewModel, incomingRegionViewModel)) {

            targetViewModel.OnRegionAssigning(context);
            if (context.Cancel) return false;
        }

        if (sourceViewModel != null &&
            !ReferenceEquals(sourceViewModel, targetViewModel) &&
            !ReferenceEquals(sourceViewModel, incomingRegionViewModel)) {

            sourceViewModel.OnRegionAssigning(context);
            if (context.Cancel) return false;
        }

        if (incomingRegionViewModel != null) {
            incomingRegionViewModel.OnRegionAssigning(context);
            if (context.Cancel) return false;
        }

        return true;
    }

    #endregion Region Assignment
}