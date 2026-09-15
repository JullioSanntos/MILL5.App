using System.Collections.Concurrent;
using MILL06.ViewModels;
using MILL80.Infrastructure;

namespace MILL03.Views.UIInfrastructure;


public class ViewLocator {

    #region ViewLocator's Instance Singleton
    public static ViewLocator Instance =>
        global::MILL80.Infrastructure.ServiceLocator.CurrentProvider.GetRequiredService<ViewLocator>();

    protected internal ViewLocator() { }
    #endregion

    // Thread-safe dictionary using a Tuple key: (ViewModel Type, Optional ViewType string)
    private readonly ConcurrentDictionary<(Type VmType, string? ViewType), Type> _registry = new();

    /// <summary>
    /// Explicitly registers a View to a ViewModel, overriding naming conventions.
    /// </summary>
    public void Register<TViewModel, TView>(string? viewType = null)
        where TViewModel : BaseViewModel
        where TView : View {

        var key = (typeof(TViewModel), viewType);
        _registry[key] = typeof(TView);
    }

    public View Resolve(BaseViewModel? viewModel) {
        if (viewModel == null) return new ContentView();

        var vmType = viewModel.GetType();
        var requestedViewType = viewModel.ViewType;
        var lookupKey = (vmType, requestedViewType);

        // 1. Check if explicitly registered or already cached
        if (_registry.TryGetValue(lookupKey, out var resolvedType)) {
            return InstantiateAndBind(resolvedType, viewModel);
        }

        // 2. Fallback to Convention-Based Reflection
        var vmName = vmType.Name;
        if (vmName.EndsWith("ViewModel")) {
            vmName = vmName.Substring(0, vmName.Length - "ViewModel".Length);
        }

        var expectedViewName = string.IsNullOrEmpty(requestedViewType)
            ? $"{vmName}View"
            : $"{vmName}{requestedViewType}View";

        resolvedType = typeof(ViewLocator).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name.Equals(expectedViewName, StringComparison.OrdinalIgnoreCase)
                                 && typeof(View).IsAssignableFrom(t));

        if (resolvedType != null) {
            // Cache the result so convention reflection only happens once per type
            _registry[lookupKey] = resolvedType;
            return InstantiateAndBind(resolvedType, viewModel);
        }

        // 3. Fallback for missing views
        return new ContentView {
            Content = new Label {
                Text = $"[View Not Found: {expectedViewName}]",
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Red
            }
        };
    }

    private static View InstantiateAndBind(Type viewType, BaseViewModel viewModel) {
        if (Activator.CreateInstance(viewType) is View view) {
            view.BindingContext = viewModel;
            return view;
        }
        throw new InvalidOperationException($"Failed to instantiate {viewType.Name}");
    }
}