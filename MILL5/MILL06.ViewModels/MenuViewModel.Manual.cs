using CommunityToolkit.Mvvm.Input;
using MILL09.Models;
using System;
using System.Collections.Generic;
using System.Text;
using MILL80.Infrastructure;

namespace MILL06.ViewModels {
    [Register]
    public partial class MenuViewModel {

        #region Commands
        [RelayCommand]
        private async Task LoadMenuItemsAsync() {
            OnPropertyChanged(nameof(MenuItems));
        }
        #endregion Commands
    }
}
