using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MILL09.Models;
using MILL80.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using MILL06.ViewModels.UIContracts;

namespace MILL06.ViewModels; 
[Register]
public partial class MenuViewModel {

    [ObservableProperty]
    private MenuItemViewModel? _selectedMenuNode;

    #region Commands
    [RelayCommand]
    private async Task LoadMenuItemsAsync() {
        OnPropertyChanged(nameof(MenuItems));
    }
    #endregion Commands

    protected override bool OnCanBeReplacedBy(
        RegionAssignmentContext context,
        bool currentDecision) {

        // Preserve the normal decision unless another Menu is incoming.
        //
        // This allows a Menu occupying a protected layout position to reject
        // ordinary content while still permitting another Menu to replace it.
        if (context.IncomingViewModel is MenuViewModel)
            return true;

        return currentDecision;
    }
}



