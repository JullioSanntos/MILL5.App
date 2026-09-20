using System.ComponentModel;

namespace MILL06.ViewModels.UIContracts;

/// <summary>
/// Used by the cancellable pre-mutation lifecycle callbacks:
///
///     RegionReplacing
///     RegionRemoving
///     RegionAssigning
///
/// Cancel=true prevents the complete Region assignment transaction.
/// </summary>
public sealed class RegionAssignmentChangingEventArgs(RegionAssignmentContext context) : CancelEventArgs {
    public RegionAssignmentContext Context { get; } = context ??
                                                      throw new ArgumentNullException(nameof(context));
}