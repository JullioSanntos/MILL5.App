using Microsoft.Extensions.DependencyInjection;
using MILL09.Models;

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
    }

    #endregion MainViewModel's Instance Singleton
}