using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MILL06.ViewModels; 
public class MainViewModel : ObservableObject {

    #region singleton
    private static readonly Lazy<MainViewModel> _instance =
        new Lazy<MainViewModel>(() => new MainViewModel());

    // Global access point
    public static MainViewModel Instance => _instance.Value;

    protected internal MainViewModel() { }
    #endregion singleton

}