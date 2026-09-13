using Microsoft.Maui.Controls;
using MILL09.Models;
using System;

namespace MILL03.Views.Views;

public partial class CustomersView : ContentView {
    public CustomersView() {
        InitializeComponent();
    }

    private async void OnLoadCustomersClicked(object sender, EventArgs e) {
        await Customer.LoadAllAsync();
    }
}