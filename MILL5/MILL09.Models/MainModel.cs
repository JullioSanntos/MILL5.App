using Microsoft.Extensions.DependencyInjection;
using MILL80.Infrastructure;
using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MILL09.Models;

public partial class MainModel : ObservableObject, IDisposable {
    protected internal MainModel() { }

    #region MainModel's Instance Singleton
    public static MainModel Instance =>
        global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<MainModel>();
    #endregion MainModel's Instance Singleton

    #region IDisposable
    public void Dispose() {
        GC.SuppressFinalize(this);
    }
    #endregion IDisposable
}