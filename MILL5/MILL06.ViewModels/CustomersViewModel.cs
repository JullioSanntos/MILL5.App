using CommunityToolkit.Mvvm.Input;
using MILL09.Models; 
using System.Collections.ObjectModel;
using MILL80.Infrastructure;

namespace MILL06.ViewModels;

[Register]
public partial class CustomersViewModel : BaseViewModel {

    //// 1. Bridge the View to the MainModel's collection
    //public ObservableCollection<Customer> Customers => MainViewModel.Instance.MainModel.Customers;

    //// 2. The toolkit automatically generates an ICommand named 'LoadCustomersCommand'
    //[RelayCommand]
    //private async Task LoadCustomersAsync() {
    //    await Customer.LoadAllAsync();

    //    // Optional: If MainModel re-assigns the list entirely instead of clearing/adding, 
    //    // you may need to notify the UI that the bridging property updated:
    //    // OnPropertyChanged(nameof(Customers));
    //}
}