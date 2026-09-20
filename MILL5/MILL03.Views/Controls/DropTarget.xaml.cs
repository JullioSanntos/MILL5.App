using Microsoft.Maui.Controls;

#if WINDOWS
using WinUIElement = Microsoft.UI.Xaml.UIElement;
using WinUIDragEventArgs = Microsoft.UI.Xaml.DragEventArgs;
#endif

namespace MILL03.Views.Controls;

public partial class DropTarget : ContentView {
#if WINDOWS
    private WinUIElement? _platformView;
#endif

    public event EventHandler<DropTargetDroppedEventArgs>? Dropped;

    #region InnerContent

    public static readonly BindableProperty InnerContentProperty = BindableProperty.Create(
        nameof(InnerContent), typeof(View), typeof(DropTarget), null,
        propertyChanged: OnInnerContentChanged);

    public View? InnerContent {
        get => (View?)GetValue(InnerContentProperty);
        set => SetValue(InnerContentProperty, value);
    }

    private static void OnInnerContentChanged(
        BindableObject bindable, object oldValue, object newValue) {

        ((DropTarget)bindable).InnerContentHost.Content = newValue as View;
    }

    #endregion InnerContent

    #region IsDroppable

    public static readonly BindableProperty IsDroppableProperty = BindableProperty.Create(
        nameof(IsDroppable), typeof(bool), typeof(DropTarget), true,
        propertyChanged: OnIsDroppableChanged);

    public bool IsDroppable {
        get => (bool)GetValue(IsDroppableProperty);
        set => SetValue(IsDroppableProperty, value);
    }

    private static void OnIsDroppableChanged(
        BindableObject bindable, object oldValue, object newValue) {

        ((DropTarget)bindable).UpdateDropAvailability();
    }

    #endregion IsDroppable

    #region Constructors

    public DropTarget() {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

#if WINDOWS
        Surface.HandlerChanged += Surface_HandlerChanged;
#else
        WireDropGesture();
#endif
    }

    #endregion Constructors

    #region Lifecycle

    private void OnLoaded(object? sender, EventArgs e) {
        DragDropCoordinator.DragStarted += Coordinator_DragStarted;
        DragDropCoordinator.DragEnded += Coordinator_DragEnded;

        UpdateDropAvailability();
    }

    private void OnUnloaded(object? sender, EventArgs e) {
        DragDropCoordinator.DragStarted -= Coordinator_DragStarted;
        DragDropCoordinator.DragEnded -= Coordinator_DragEnded;

        ShowNormal();
    }

    #endregion Lifecycle

    #region Coordinator

    private void Coordinator_DragStarted(object? sender, EventArgs e) {
        UpdateDropAvailability();
    }

    private void Coordinator_DragEnded(object? sender, EventArgs e) {
        ShowNormal();
    }

    private void UpdateDropAvailability() {
        if (IsDroppable && DragDropCoordinator.IsDragging) {
            ShowAvailable();
            return;
        }

        ShowNormal();
    }

    #endregion Coordinator

    #region Visual State

    private void ShowNormal() {
        AvailableCue.IsVisible = false;
        DragOverCue.IsVisible = false;
    }

    private void ShowAvailable() {
        AvailableCue.IsVisible = true;
        DragOverCue.IsVisible = false;
    }

    private void ShowDragOver() {
        AvailableCue.IsVisible = false;
        DragOverCue.IsVisible = true;
    }

    #endregion Visual State

#if WINDOWS

    #region Windows Drop

    private void Surface_HandlerChanged(object? sender, EventArgs e) {
        UnwirePlatformView();

        _platformView = Surface.Handler?.PlatformView as WinUIElement;
        if (_platformView == null) return;

        _platformView.AllowDrop = IsDroppable;

        _platformView.DragEnter += PlatformView_DragEnter;
        _platformView.DragOver += PlatformView_DragOver;
        _platformView.DragLeave += PlatformView_DragLeave;
        _platformView.Drop += PlatformView_Drop;
    }

    private void UnwirePlatformView() {
        if (_platformView == null) return;

        _platformView.DragEnter -= PlatformView_DragEnter;
        _platformView.DragOver -= PlatformView_DragOver;
        _platformView.DragLeave -= PlatformView_DragLeave;
        _platformView.Drop -= PlatformView_Drop;

        _platformView.AllowDrop = false;
        _platformView = null;
    }

    private void PlatformView_DragEnter(
        object sender,
        WinUIDragEventArgs e) {

        if (!CanAcceptDrop(e)) return;

        e.AcceptedOperation =
            Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;

        ShowDragOver();
    }

    private void PlatformView_DragOver(
        object sender,
        WinUIDragEventArgs e) {

        if (!CanAcceptDrop(e)) {
            e.AcceptedOperation =
                Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;

            return;
        }

        e.AcceptedOperation =
            Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;

        ShowDragOver();
    }

    private void PlatformView_DragLeave(
        object sender,
        WinUIDragEventArgs e) {

        UpdateDropAvailability();
    }

    private void PlatformView_Drop(
        object sender,
        WinUIDragEventArgs e) {

        var dragData = DragDropCoordinator.DragData;

        ShowNormal();

        if (!IsDroppable || dragData == null) return;

        Dropped?.Invoke(
            this,
            new DropTargetDroppedEventArgs(dragData));
    }

    private bool CanAcceptDrop(WinUIDragEventArgs e) {
        return IsDroppable &&
               DragDropCoordinator.DragData != null;
    }

    #endregion Windows Drop

#else

    #region MAUI Drop

    private void WireDropGesture() {
        var dropGesture = new DropGestureRecognizer();

        dropGesture.DragOver += OnDragOver;
        dropGesture.DragLeave += OnDragLeave;
        dropGesture.Drop += OnDrop;

        Surface.GestureRecognizers.Add(dropGesture);
    }

    private void OnDragOver(
        object? sender,
        Microsoft.Maui.Controls.DragEventArgs e) {

        if (!IsDroppable || DragDropCoordinator.DragData == null) {
            e.AcceptedOperation =
                Microsoft.Maui.Controls.DataPackageOperation.None;

            return;
        }

        e.AcceptedOperation =
            Microsoft.Maui.Controls.DataPackageOperation.Copy;

        ShowDragOver();
    }

    private void OnDragLeave(
        object? sender,
        Microsoft.Maui.Controls.DragEventArgs e) {

        UpdateDropAvailability();
    }

    private void OnDrop(
        object? sender,
        DropEventArgs e) {

        var dragData = DragDropCoordinator.DragData;

        ShowNormal();

        if (!IsDroppable || dragData == null) return;

        Dropped?.Invoke(
            this,
            new DropTargetDroppedEventArgs(dragData));
    }

    #endregion MAUI Drop

#endif
}