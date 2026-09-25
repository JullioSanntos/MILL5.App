using Microsoft.Maui.Controls;

#if WINDOWS
using InputSystemCursorShape = Microsoft.UI.Input.InputSystemCursorShape;
using WinUIElement = Microsoft.UI.Xaml.UIElement;
using WinUIDragStartingEventArgs = Microsoft.UI.Xaml.DragStartingEventArgs;
using WinUIDropCompletedEventArgs = Microsoft.UI.Xaml.DropCompletedEventArgs;
using WinUIPointerEventArgs = Microsoft.UI.Xaml.Input.PointerRoutedEventArgs;
#endif

namespace MILL03.Views.Controls;

public partial class Draggable : ContentView {

    #region Styles

    private const string NormalStyle = "DraggableSurfaceStyle";
    private const string DraggingStyle = "DraggableSurfaceDraggingStyle";
    private const string DefaultDragIndicatorStyle = "DragIndicatorStyle";

    #endregion Styles

    #region State

    private bool _isDragging;

#if WINDOWS
    private WinUIElement? _platformView;
#endif

    #endregion State

    #region DragData

    public static readonly BindableProperty DragDataProperty = BindableProperty.Create(
        nameof(DragData), typeof(object), typeof(Draggable));

    public object? DragData {
        get => GetValue(DragDataProperty);
        set => SetValue(DragDataProperty, value);
    }

    #endregion DragData

    #region IsDraggable

    public static readonly BindableProperty IsDraggableProperty = BindableProperty.Create(
        nameof(IsDraggable), typeof(bool), typeof(Draggable), true,
        propertyChanged: OnIsDraggableChanged);

    public bool IsDraggable {
        get => (bool)GetValue(IsDraggableProperty);
        set => SetValue(IsDraggableProperty, value);
    }

    private static void OnIsDraggableChanged(
        BindableObject bindable, object oldValue, object newValue) {

        ((Draggable)bindable).UpdateDraggableState();
    }

    #endregion IsDraggable

    #region DragIndicatorStyle

    /// <summary>
    /// Optional host-specific presentation for the drag indicator.
    /// When null, the application DragIndicatorStyle resource is used.
    /// </summary>
    public static readonly BindableProperty DragIndicatorStyleProperty = BindableProperty.Create(
        nameof(DragIndicatorStyle), typeof(Style), typeof(Draggable), null,
        propertyChanged: OnDragIndicatorStyleChanged);

    public Style? DragIndicatorStyle {
        get => (Style?)GetValue(DragIndicatorStyleProperty);
        set => SetValue(DragIndicatorStyleProperty, value);
    }

    private static void OnDragIndicatorStyleChanged(
        BindableObject bindable, object oldValue, object newValue) {

        ((Draggable)bindable).ApplyDragIndicatorStyle();
    }

    private void ApplyDragIndicatorStyle() {
        if (DragIndicatorStyle != null) {
            DragIndicator.Style = DragIndicatorStyle;
            return;
        }

        DragIndicator.SetDynamicResource(
            NavigableElement.StyleProperty,
            DefaultDragIndicatorStyle);
    }

    #endregion DragIndicatorStyle

    #region InnerContent

    public static readonly BindableProperty InnerContentProperty = BindableProperty.Create(
        nameof(InnerContent), typeof(View), typeof(Draggable), null,
        propertyChanged: OnInnerContentChanged);

    public View? InnerContent {
        get => (View?)GetValue(InnerContentProperty);
        set => SetValue(InnerContentProperty, value);
    }

    private static void OnInnerContentChanged(
        BindableObject bindable, object oldValue, object newValue) {

        ((Draggable)bindable).InnerContentHost.Content = newValue as View;
    }

    #endregion InnerContent

    #region Constructors

    public Draggable() {
        InitializeComponent();

        ApplyDragIndicatorStyle();

#if WINDOWS
        DragIndicator.HandlerChanged += DragIndicator_HandlerChanged;
#else
        WirePointerGesture();
        WireDragGesture();
#endif

        UpdateDraggableState();
    }

    #endregion Constructors

    #region State Management

    private void UpdateDraggableState() {
        DragIndicator.IsVisible = IsDraggable;

#if WINDOWS
        if (_platformView != null)
            _platformView.CanDrag = IsDraggable;
#endif

        if (IsDraggable) return;

        _isDragging = false;

        ApplyStyle(NormalStyle);
        DragDropCoordinator.End();

#if WINDOWS
        SetCursor(InputSystemCursorShape.Arrow);
#endif
    }

    private void EndDrag() {
        _isDragging = false;

        DragDropCoordinator.End();
        ApplyStyle(NormalStyle);
    }

    #endregion State Management

#if WINDOWS

    #region Windows Interaction

    private void DragIndicator_HandlerChanged(object? sender, EventArgs e) {
        UnwirePlatformView();

        _platformView = DragIndicator.Handler?.PlatformView as WinUIElement;
        if (_platformView == null) return;

        _platformView.CanDrag = IsDraggable;

        _platformView.PointerEntered += PlatformView_PointerEntered;
        _platformView.PointerExited += PlatformView_PointerExited;
        _platformView.DragStarting += PlatformView_DragStarting;
        _platformView.DropCompleted += PlatformView_DropCompleted;
    }

    private void UnwirePlatformView() {
        if (_platformView == null) return;

        _platformView.PointerEntered -= PlatformView_PointerEntered;
        _platformView.PointerExited -= PlatformView_PointerExited;
        _platformView.DragStarting -= PlatformView_DragStarting;
        _platformView.DropCompleted -= PlatformView_DropCompleted;

        _platformView.CanDrag = false;
        _platformView = null;
    }

    private void PlatformView_PointerEntered(
        object sender, WinUIPointerEventArgs e) {

        if (!IsDraggable) return;

        SetCursor(InputSystemCursorShape.Hand);
    }

    private void PlatformView_PointerExited(
        object sender, WinUIPointerEventArgs e) {

        SetCursor(InputSystemCursorShape.Arrow);
    }

    private void PlatformView_DragStarting(
        WinUIElement sender, WinUIDragStartingEventArgs e) {

        if (!IsDraggable || DragData == null) {
            e.Cancel = true;
            return;
        }

        _isDragging = true;

        DragDropCoordinator.Begin(DragData);
        ApplyStyle(DraggingStyle);

        e.Data.RequestedOperation =
            Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;

        e.Data.SetText(DragData.ToString() ?? string.Empty);
    }

    private void PlatformView_DropCompleted(
        WinUIElement sender, WinUIDropCompletedEventArgs e) {

        EndDrag();
    }

    private void SetCursor(InputSystemCursorShape cursor) {
        if (_platformView != null)
            WinUICursorExtensions.ForceSetCursor(_platformView, cursor);
    }

    #endregion Windows Interaction

#else

    #region MAUI Interaction

    private void WirePointerGesture() {
        var pointerGesture = new PointerGestureRecognizer();

        pointerGesture.PointerEntered += OnPointerEntered;
        pointerGesture.PointerExited += OnPointerExited;

        DragIndicator.GestureRecognizers.Add(pointerGesture);
    }

    private void OnPointerEntered(
        object? sender, Microsoft.Maui.Controls.PointerEventArgs e) {
    }

    private void OnPointerExited(
        object? sender, Microsoft.Maui.Controls.PointerEventArgs e) {
    }

    private void WireDragGesture() {
        var dragGesture = new DragGestureRecognizer();

        dragGesture.DragStarting += OnDragStarting;
        dragGesture.DropCompleted += OnDropCompleted;

        DragIndicator.GestureRecognizers.Add(dragGesture);
    }

    private void OnDragStarting(object? sender, DragStartingEventArgs e) {
        if (!IsDraggable || DragData == null) {
            e.Cancel = true;
            return;
        }

        _isDragging = true;

        DragDropCoordinator.Begin(DragData);
        ApplyStyle(DraggingStyle);

        e.Data.Properties[DragDropData.DataKey] = DragData;
    }

    private void OnDropCompleted(object? sender, DropCompletedEventArgs e) {
        EndDrag();
    }

    #endregion MAUI Interaction

#endif

    #region Styles

    private void ApplyStyle(string resourceKey) {
        Surface.SetDynamicResource(
            NavigableElement.StyleProperty,
            resourceKey);
    }

    #endregion Styles
}