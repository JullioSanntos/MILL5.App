using MILL06.ViewModels;

namespace MILL06.ViewModels.UIContracts;

/// <summary>
/// Provides information about a completed Region assignment.
/// </summary>
public sealed class RegionAssignedEventArgs(
    RegionNode? sourceRegionNode,
    RegionNode targetRegionNode,
    BaseViewModel assignedViewModel)
    : EventArgs {
    public RegionNode? SourceRegionNode { get; } = sourceRegionNode;
    public RegionNode TargetRegionNode { get; } = targetRegionNode ??
                                                  throw new ArgumentNullException(nameof(targetRegionNode));

    public BaseViewModel AssignedViewModel { get; } = assignedViewModel ??
                                                      throw new ArgumentNullException(nameof(assignedViewModel));
}