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

            // 1. Try to find an empty node first (Existing logic)
            var targetNode = FindTargetNode(RootRegionNode, ActiveRegionNode);

            // 2. NEW REQUIREMENT: If no empty cells, fallback to the last populated cell
            if (targetNode == null) {
                targetNode = GetReplaceableNode(ActiveRegionNode, RootRegionNode);
            }

            if (targetNode != null) {
                // 3. Resolve and Inject
                var vmProperty = this.GetType().GetProperty(selectedNode.TargetViewModelName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (vmProperty?.GetValue(this) is BaseViewModel vmInstance) {
                    targetNode.PayloadViewModel = vmInstance;
                    ActiveRegionNode = targetNode; // This updates the "last populated" tracker!
                }
            }
        }
    }

    private RegionNode? GetReplaceableNode(RegionNode? activeNode, RegionNode rootNode) {
        // 1. If the Active Node is a leaf and is NOT holding the StartupViewModel, 
        // then it is our last populated cell. We replace its view!
        if (activeNode != null && !activeNode.IsSplit && activeNode.PayloadViewModel != this.StartupViewModel) {
            return activeNode;
        }

        // 2. If the Active Node IS the protected StartupView (or is a split parent), 
        // we must not overwrite it. Instead, find ANY unprotected leaf in the tree.
        return FindFirstUnprotectedLeaf(rootNode);
    }

    private RegionNode? FindFirstUnprotectedLeaf(RegionNode currentNode) {
        // If it's a leaf, check if it's protected by the StartupViewModel
        if (!currentNode.IsSplit) {
            return (currentNode.PayloadViewModel == this.StartupViewModel) ? null : currentNode;
        }

        // Recurse down children branches
        if (currentNode.FirstChild != null) {
            var found = FindFirstUnprotectedLeaf(currentNode.FirstChild);
            if (found != null) return found;
        }

        if (currentNode.SecondChild != null) {
            var found = FindFirstUnprotectedLeaf(currentNode.SecondChild);
            if (found != null) return found;
        }

        return null;
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