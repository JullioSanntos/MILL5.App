using System.ComponentModel;
using MILL06.ViewModels;

namespace MILL06.ViewModels.UIContracts;

/// <summary>
/// Provides information about a pending Region assignment.
///
/// RegionAssigning is cancellable. SourceRegionNode may be null when
/// the incoming ViewModel does not originate from an existing Region.
/// </summary>
public sealed class RegionAssigningEventArgs(
    RegionNode? sourceRegionNode,
    RegionNode targetRegionNode,
    BaseViewModel incomingViewModel)
    : CancelEventArgs {
    /// <summary>
    /// Gets the Region from which the incoming ViewModel originates,
    /// or null when the assignment has no Region source.
    /// </summary>
    public RegionNode? SourceRegionNode { get; } = sourceRegionNode;

    /// <summary>
    /// Gets the Region that is about to receive the incoming ViewModel.
    /// </summary>
    public RegionNode TargetRegionNode { get; } = targetRegionNode ??
                                                  throw new ArgumentNullException(nameof(targetRegionNode));

    /// <summary>
    /// Gets the ViewModel that is about to be assigned to the target Region.
    /// </summary>
    public BaseViewModel IncomingViewModel { get; } = incomingViewModel ??
                                                      throw new ArgumentNullException(nameof(incomingViewModel));
}