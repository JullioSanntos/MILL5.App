using Microsoft.Maui.Controls;
using MILL06.ViewModels.UIContracts;

namespace MILL03.Views.Controls;

public partial class RegionTreeHost : ContentView {

    #region RegionTree

    public static readonly BindableProperty RegionTreeProperty = BindableProperty.Create(
        nameof(RegionTree), typeof(RegionNodesTree), typeof(RegionTreeHost), null,
        propertyChanged: OnRegionTreeChanged);

    public RegionNodesTree? RegionTree {
        get => (RegionNodesTree?)GetValue(RegionTreeProperty);
        set => SetValue(RegionTreeProperty, value);
    }

    private static void OnRegionTreeChanged(
        BindableObject bindable, object oldValue, object newValue) {

        if (bindable is RegionTreeHost host)
            host.AttachRegionTree(newValue as RegionNodesTree);
    }

    private void AttachRegionTree(RegionNodesTree? regionTree) {
        RootGrid.Children.Clear();

        if (regionTree == null) return;

        RootGrid.Children.Add(
            new RegionCell {
                RegionNode = regionTree.RootRegionNode
            });
    }

    #endregion RegionTree

    #region Constructors

    public RegionTreeHost() {
        InitializeComponent();
    }

    #endregion Constructors
}