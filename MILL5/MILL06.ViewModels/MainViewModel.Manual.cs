using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using MILL06.ViewModels.UIContracts;
using MILL09.Models;

namespace MILL06.ViewModels;

public partial class MainViewModel : BaseViewModel, IDisposable {

    #region RegionManager properties

    #region MainModel
    // Child properties pull from the locator instead of using 'new()', preserving full substitution
    private MainModel? _mainModel;
    public MainModel MainModel =>
        _mainModel ??= global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<MainModel>();
    #endregion MainModel

    // 1. The root node of your entire layout tree. 
    private RegionNode? _rootRegionNode;
    public RegionNode RootRegionNode => _rootRegionNode ??= new RegionNode {
        PayloadViewModel = StartupViewModel
    };

    // Active node tracking for your target rules (Active preferred, otherwise First Empty)
    [ObservableProperty]
    private RegionNode? _activeRegionNode;

    private BaseViewModel? _startupViewModel;
    public BaseViewModel? StartupViewModel {
        get {
            // Assuming MenuViewModel is defined elsewhere in your partial class
            return _startupViewModel ??= this.MenuViewModel;
        }
    }
    #endregion RegionManager properties

    #region MainViewModel's Instance Singleton
    // Resolves directly from your global container, allowing test initialization to swap the provider
    public static MainViewModel Instance =>
        global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<MainViewModel>();

    protected internal MainViewModel() {
        // 2. Initialize the tree with a default single root leaf node so the app launches cleanly
        // (Moved the instantiation to the property getter to avoid double-instantiation)
        _activeRegionNode = RootRegionNode; // Default focus to the root

        this.MenuViewModel.PropertyChanged += MenuViewModel_PropertyChanged;
    }

    private void MenuViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(MenuViewModel.SelectedMenuNode)) {
            var selectedNode = ((MenuViewModel)sender!).SelectedMenuNode;
            if (selectedNode == null || RootRegionNode == null || string.IsNullOrEmpty(selectedNode.TargetViewModelName)) return;

            // 3. Find the target node using your rules (Active preferred, then First Empty)
            var targetNode = FindTargetNode(RootRegionNode, ActiveRegionNode);

            if (targetNode != null) {
                // 4. Resolve the ViewModel instance from the string name!
                // This grabs the property (e.g., "AddressesViewModel") from this MainViewModel instance.
                var vmProperty = this.GetType().GetProperty(selectedNode.TargetViewModelName, BindingFlags.Public | BindingFlags.Instance);

                if (vmProperty?.GetValue(this) is BaseViewModel vmInstance) {
                    // Inject the actual ViewModel instance into the tree!
                    targetNode.PayloadViewModel = vmInstance;

                    // Move active focus to this node
                    ActiveRegionNode = targetNode;
                }
            }
        }
    }
    #endregion MainViewModel's Instance Singleton

    #region Properties

    private RegionNode? FindTargetNode(RegionNode currentNode, RegionNode? preferredNode) {
        // UPDATED: Check for PayloadViewModel == null instead of empty string

        // If preferred node is a valid empty leaf, use it
        if (preferredNode != null && !preferredNode.IsSplit && preferredNode.PayloadViewModel == null) {
            return preferredNode;
        }

        // If current node is an empty leaf, use it
        if (!currentNode.IsSplit && currentNode.PayloadViewModel == null) {
            return currentNode;
        }

        // Recurse down children branches
        if (currentNode.FirstChild != null) {
            var found = FindTargetNode(currentNode.FirstChild, preferredNode);
            if (found != null) return found;
        }

        if (currentNode.SecondChild != null) {
            var found = FindTargetNode(currentNode.SecondChild, preferredNode);
            if (found != null) return found;
        }

        return null;
    }
    #endregion Properties

    // The engineer just types this to add their manual cleanup!
    partial void OnDisposing() {
        this.MenuViewModel.PropertyChanged -= MenuViewModel_PropertyChanged;
        // Unsubscribe from events, clear custom messengers, etc.
        // e.g., WeakReferenceMessenger.Default.UnregisterAll(this);
        GC.SuppressFinalize(this);
    }
}