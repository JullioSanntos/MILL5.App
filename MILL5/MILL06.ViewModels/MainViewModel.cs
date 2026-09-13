using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using MILL06.ViewModels;
using MILL09.Models;
using MILL80.Infrastructure;
using System;

namespace MILL06.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable {

    #region MainViewModel's Instance Singleton
    // Resolves directly from your global container, allowing test initialization to swap the provider
    public static MainViewModel Instance =>
        global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<MainViewModel>();

    protected internal MainViewModel() { }
    #endregion MainViewModel's Instance Singleton

    #region MainModel
    // Child properties pull from the locator instead of using 'new()', preserving full substitution
    private MainModel? _mainModel;
    public MainModel MainModel =>
        _mainModel ??= global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<MainModel>();
    #endregion MainModel

    #region DashboardViewModel
    private DashboardViewModel? _dashboardViewModel;
    public DashboardViewModel DashboardViewModel =>
        _dashboardViewModel ??= global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<DashboardViewModel>();
    #endregion DashboardViewModel

    #region IDisposable
    public void Dispose() {
        // MainModel is deliberately absent as it is owned by the container, not this class.
        _dashboardViewModel?.Dispose();
        GC.SuppressFinalize(this);
    }
    #endregion IDisposable

}