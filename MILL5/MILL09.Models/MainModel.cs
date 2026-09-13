using Microsoft.Extensions.DependencyInjection;
using MILL80.Infrastructure;
using System;

namespace MILL09.Models;

public class MainModel : IDisposable {
    protected internal MainModel() { }

    #region MainModel's Instance Singleton
    public static MainModel Instance =>
        global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<MainModel>();
    #endregion MainModel's Instance Singleton

    #region CurrentContext
    private string? _currentContext;
    public string CurrentContext {
        get {
            if (_currentContext == null) {
                _currentContext = "DefaultContext";
            }
            return _currentContext;
        }
        set {
            if (_currentContext != value) {
                _currentContext = value;
            }
        }
    }
    #endregion CurrentContext

    #region IDisposable
    public void Dispose() {
        GC.SuppressFinalize(this);
    }
    #endregion IDisposable
}