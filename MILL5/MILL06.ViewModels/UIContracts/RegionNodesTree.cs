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

    #region Region Assignment

    /// <summary>
    /// Performs the invariant lifecycle for a Region drag/drop assignment.
    ///
    /// Each distinct participating RegionBaseViewModel is consulted once.
    /// The target is consulted first, a distinct source second, and the
    /// dragged ViewModel last. Any participant may cancel the assignment.
    /// </summary>
    public bool AssignRegion(RegionNode sourceNode,
        BaseViewModel draggedViewModel, RegionNode targetNode) {

        ArgumentNullException.ThrowIfNull(sourceNode);
        ArgumentNullException.ThrowIfNull(draggedViewModel);
        ArgumentNullException.ThrowIfNull(targetNode);

        if (ReferenceEquals(sourceNode, targetNode))
            return false;

        var context = new RegionAssigningContext(
            sourceNode,
            draggedViewModel,
            targetNode);

        var targetViewModel =
            targetNode.PayloadViewModel as RegionBaseViewModel;

        var sourceViewModel =
            sourceNode.PayloadViewModel as RegionBaseViewModel;

        var draggedRegionViewModel =
            draggedViewModel as RegionBaseViewModel;

        // Target gets first refusal unless it is also the dragged ViewModel.
        // In that unusual case it is deferred so the dragged participant
        // retains the final decision.
        if (targetViewModel != null &&
            !ReferenceEquals(targetViewModel, draggedRegionViewModel)) {

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

        targetNode.PayloadViewModel = draggedViewModel;
        ActiveRegionNode = targetNode;

        RootRegionNode.RaiseRegionAssigned(
            sourceNode,
            targetNode,
            draggedViewModel);

        return true;
    }

    #endregion Region Assignment
}