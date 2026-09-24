using System.Reflection;

namespace MILL06.ViewModels;

public partial class MenuItemViewModel {

    #region Target ViewModel

    /// <summary>
    /// Gets the ViewModel represented by this Menu item from the
    /// MainViewModel object graph.
    /// </summary>
    public BaseViewModel? TargetViewModel {
        get {
            if (string.IsNullOrEmpty(TargetViewModelName)) return null;

            var property = typeof(MainViewModel).GetProperty(
                TargetViewModelName,
                BindingFlags.Public | BindingFlags.Instance);

            return property?.GetValue(MainViewModel.Instance) as BaseViewModel;
        }
    }

    #endregion Target ViewModel
}