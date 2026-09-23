using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace MILL06.ViewModels.UIContracts;

/// <summary>
/// Owns the application's RegionNode tree and its single active Region.
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
}