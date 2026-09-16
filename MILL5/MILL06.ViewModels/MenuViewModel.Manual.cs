using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MILL09.Models;
using MILL80.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace MILL06.ViewModels {
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
    }
}
