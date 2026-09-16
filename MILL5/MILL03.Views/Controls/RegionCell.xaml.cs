using Microsoft.Maui.Controls;
using MILL03.Views.UIInfrastructure;
using MILL06.ViewModels;
using MILL06.ViewModels.UIContracts;

namespace MILL03.Views.Controls;

public enum SplitDirection { Top, Bottom, Left, Right }

public partial class RegionCell : ContentView {
    // --- STATE-DRIVEN TREE BINDING ---
    public static readonly BindableProperty RegionNodeProperty = BindableProperty.Create(
        nameof(RegionNode),
        typeof(RegionNode),
        typeof(RegionCell),
        null,
        propertyChanged: (bindable, oldValue, newValue) => {
            if (bindable is RegionCell cell) {
                if (oldValue is RegionNode oldNode) {
                    oldNode.PropertyChanged -= cell.OnNodePropertyChanged;
                }
                if (newValue is RegionNode newNode) {
                    newNode.PropertyChanged += cell.OnNodePropertyChanged;
                    // Force an initial sync when the node is first attached
                    cell.SyncWithNode(newNode);
                }
            }
        });

    public RegionNode? RegionNode {
        get => (RegionNode?)GetValue(RegionNodeProperty);
        set => SetValue(RegionNodeProperty, value);
    }

    private void OnNodePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
        if (sender is RegionNode node && e.PropertyName == nameof(RegionNode.PayloadViewModel)) {
            MainThread.BeginInvokeOnMainThread(() => SyncWithNode(node));
        }
    }

    private void SyncWithNode(RegionNode node) {
        if (PayloadContainer == null) return;

        // If the node has no payload, it's an empty placeholder. Give it a dummy color.
        if (node.PayloadViewModel == null) {
            PayloadContainer.Content = new ContentView { BackgroundColor = GetNextColor() };
            return;
        }

        // Direct ViewModel-to-View resolution.
        var resolvedView = ViewLocator.Instance.Resolve(node.PayloadViewModel);

        // 1. It's a REAL view! Force background to White if it doesn't already have one set in XAML
        if (resolvedView != null && resolvedView.BackgroundColor == null) {
            resolvedView.BackgroundColor = Colors.White;
        }

        InjectPayload(resolvedView);
    }

    // --- EXISTING DIRECT BINDINGS (Kept for backward compatibility/flexibility) ---
    public static readonly BindableProperty PayloadViewModelProperty = BindableProperty.Create(
        nameof(PayloadViewModel),
        typeof(BaseViewModel),
        typeof(RegionCell),
        null,
        propertyChanged: (bindable, oldValue, newValue) => {
            if (bindable is RegionCell cell && newValue is BaseViewModel vm) {
                var resolvedView = ViewLocator.Instance.Resolve(vm);
                cell.InjectPayload(resolvedView);
            }
        });

    public BaseViewModel? PayloadViewModel {
        get => (BaseViewModel?)GetValue(PayloadViewModelProperty);
        set => SetValue(PayloadViewModelProperty, value);
    }

    public static readonly BindableProperty PayloadProperty = BindableProperty.Create(
        nameof(Payload),
        typeof(View),
        typeof(RegionCell),
        null,
        propertyChanged: (bindable, oldValue, newValue) => {
            if (newValue is View newView) {
                ((RegionCell)bindable).InjectPayload(newView);
            }
        });

    public View Payload {
        get => (View)GetValue(PayloadProperty);
        set => SetValue(PayloadProperty, value);
    }

    // --- UI INFRASTRUCTURE & ADORNERS ---
    private static readonly Color[] RegionColors = {
        Colors.White, Colors.LightBlue, Colors.LightCoral, Colors.LightGreen,
        Colors.LightGoldenrodYellow, Colors.LightPink, Colors.MediumPurple,
        Colors.LightSeaGreen, Colors.Orange, Colors.LightSkyBlue, Colors.Plum
    };
    private static readonly Random _random = new Random();
    private static int _lastColorIndex = -1;

    public static Color GetNextColor() {
        int nextIndex;
        if (_lastColorIndex == -1) { nextIndex = 0; }
        else {
            do {
                nextIndex = _random.Next(RegionColors.Length);
            } while (nextIndex == _lastColorIndex);
        }
        _lastColorIndex = nextIndex;
        return RegionColors[nextIndex];
    }

    public static readonly BindableProperty IsSplittingProperty = BindableProperty.Create(
        nameof(IsSplitting),
        typeof(bool),
        typeof(RegionCell),
        false,
        BindingMode.TwoWay,
        propertyChanged: OnIsSplittingStateChanged);

    public bool IsSplitting {
        get => (bool)GetValue(IsSplittingProperty);
        set => SetValue(IsSplittingProperty, value);
    }

    private readonly Lazy<Dictionary<SplitDirection, BoxView>> _adorners;

    public RegionCell() {
        InitializeComponent();

        _adorners = new Lazy<Dictionary<SplitDirection, BoxView>>(() => new Dictionary<SplitDirection, BoxView> {
            { SplitDirection.Top, TopAdorner },
            { SplitDirection.Bottom, BottomAdorner },
            { SplitDirection.Left, LeftAdorner },
            { SplitDirection.Right, RightAdorner }
        });

        WireUpAutonomousLogic();
    }

    protected override void OnHandlerChanged() {
        base.OnHandlerChanged();
        // Failsafe: Ensure content paints if the binding fired before InitializeComponent finished
        if (Handler != null && RegionNode != null) {
            SyncWithNode(RegionNode);
        }
    }

    private static void OnIsSplittingStateChanged(BindableObject bindable, object oldValue, object newValue) {
        var cell = (RegionCell)bindable;
        bool isSplitting = (bool)newValue;
        var targetColor = isSplitting ? Color.FromArgb("#80808080") : Colors.Transparent;

        foreach (var adorner in cell._adorners.Value.Values) {
            adorner.BackgroundColor = targetColor;
        }

        cell.IndicatorLabel.IsVisible = isSplitting;
        cell.AnticipationLineBox.IsVisible = isSplitting;
    }

    private void WireUpAutonomousLogic() {
        foreach (var kvp in _adorners.Value) {
            var direction = kvp.Key;
            var adorner = kvp.Value;

            var pointerGesture = new PointerGestureRecognizer();
            pointerGesture.PointerEntered += (s, e) => this.IsSplitting = true;
            pointerGesture.PointerExited += (s, e) => this.IsSplitting = false;
            pointerGesture.PointerMoved += (s, e) => TrackInteractions(e, direction);
            adorner.GestureRecognizers.Add(pointerGesture);

            var tapGesture = new TapGestureRecognizer();
            bool isTopBottom = direction == SplitDirection.Top || direction == SplitDirection.Bottom;

            tapGesture.Tapped += (s, e) => {
                var pos = e.GetPosition(RootGrid);
                if (pos.HasValue) PerformSplit(isTopBottom, pos.Value.X, pos.Value.Y);
            };

            adorner.GestureRecognizers.Add(tapGesture);
        }
    }

    private void TrackInteractions(PointerEventArgs e, SplitDirection direction) {
        var position = e.GetPosition(RootGrid);
        if (!position.HasValue) return;

        if (IndicatorLabel.IsVisible) {
            IndicatorLabel.TranslationX = position.Value.X + 8;
            IndicatorLabel.TranslationY = position.Value.Y - 9;
        }

        if (AnticipationLineBox.IsVisible) {
            if (direction == SplitDirection.Top || direction == SplitDirection.Bottom) {
                AnticipationLineBox.WidthRequest = 2;
                AnticipationLineBox.HeightRequest = -1;
                AnticipationLineBox.HorizontalOptions = LayoutOptions.Start;
                AnticipationLineBox.VerticalOptions = LayoutOptions.Fill;
                AnticipationLineBox.TranslationX = position.Value.X;
                AnticipationLineBox.TranslationY = 0;
            }
            else {
                AnticipationLineBox.HeightRequest = 2;
                AnticipationLineBox.WidthRequest = -1;
                AnticipationLineBox.VerticalOptions = LayoutOptions.Start;
                AnticipationLineBox.HorizontalOptions = LayoutOptions.Fill;
                AnticipationLineBox.TranslationY = position.Value.Y;
                AnticipationLineBox.TranslationX = 0;
            }
        }
    }

    //public void InjectPayload(View payload) {
    //    PayloadContainer.Content = payload;
    //    if (payload != null && payload.BackgroundColor == null) {
    //        payload.BackgroundColor = GetNextColor();
    //    }
    //}

    public void InjectPayload(View payload) {
        PayloadContainer.Content = payload;
        // Removed the blanket GetNextColor() logic from here!
    }

    private void PerformSplit(bool isTopBottomClick, double clickX, double clickY) {
        // --- YOUR EXACT UI EXTRACTION ---
        var extractedPayload = PayloadContainer.Content ?? new ContentView { BackgroundColor = GetNextColor() };
        PayloadContainer.Content = null;

        RootGrid.Children.Clear();
        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        var splitGrid = new Grid();

        // --- STATE TREE MUTATION (INJECTED) ---
        RegionNode? child1Node = null;
        RegionNode? child2Node = null;

        if (this.RegionNode != null) {
            double stateStar1 = isTopBottomClick ? (clickX / RootGrid.Width) : (clickY / RootGrid.Height);
            double stateStar2 = 1.0 - stateStar1;

            // Child 1 inherits the current payload. Child 2 is empty.
            child1Node = new RegionNode { PayloadViewModel = this.RegionNode.PayloadViewModel };
            child2Node = new RegionNode { PayloadViewModel = null };

            // Update the current node to become a split parent
            this.RegionNode.PayloadViewModel = null;
            this.RegionNode.FirstChildWeight = stateStar1;
            this.RegionNode.SecondChildWeight = stateStar2;
            this.RegionNode.FirstChild = child1Node;
            this.RegionNode.SecondChild = child2Node;
            this.RegionNode.Orientation = isTopBottomClick ? SplitOrientation.Vertical : SplitOrientation.Horizontal;
        }
        // --------------------------------------

        // --- YOUR EXACT CELL CREATION ---
        var cell1 = new RegionCell();
        if (child1Node != null) cell1.RegionNode = child1Node; // Wire to state
        cell1.InjectPayload(extractedPayload); // Keeps your view intact

        var cell2 = new RegionCell();
        if (child2Node != null) cell2.RegionNode = child2Node; // Wire to state
        cell2.InjectPayload(new ContentView { BackgroundColor = GetNextColor() });

        // --- YOUR EXACT GRID SPLITTER & MATH ---
        var splitter = new GridSplitter {
            Orientation = isTopBottomClick ? GridSplitter.SplitOrientation.Vertical : GridSplitter.SplitOrientation.Horizontal,
            WidthRequest = isTopBottomClick ? 5 : -1,
            HeightRequest = isTopBottomClick ? -1 : 5,
            HorizontalOptions = isTopBottomClick ? LayoutOptions.Center : LayoutOptions.Fill,
            VerticalOptions = isTopBottomClick ? LayoutOptions.Fill : LayoutOptions.Center
        };

        if (isTopBottomClick) {
            double star1 = clickX / RootGrid.Width;
            double star2 = 1.0 - star1;

            splitGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(star1, GridUnitType.Star)));
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(star2, GridUnitType.Star)));

            Grid.SetColumn(cell1, 0);
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(cell2, 2);
        }
        else {
            double star1 = clickY / RootGrid.Height;
            double star2 = 1.0 - star1;

            splitGrid.RowDefinitions.Add(new RowDefinition(new GridLength(star1, GridUnitType.Star)));
            splitGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            splitGrid.RowDefinitions.Add(new RowDefinition(new GridLength(star2, GridUnitType.Star)));

            Grid.SetRow(cell1, 0);
            Grid.SetRow(splitter, 1);
            Grid.SetRow(cell2, 2);
        }

        splitGrid.Children.Add(cell1);
        splitGrid.Children.Add(splitter);
        splitGrid.Children.Add(cell2);

        RootGrid.Children.Add(splitGrid);

        // --- TARGETING UPDATE (INJECTED) ---
        if (child2Node != null) {
            // Tell the ViewModel that the new empty cell is the active target!
            MainViewModel.Instance.ActiveRegionNode = child2Node;
        }
    }

    //private void PerformSplit(bool isTopBottomClick, double clickX, double clickY) {
    //    var extractedPayload = PayloadContainer.Content ?? new ContentView { BackgroundColor = GetNextColor() };
    //    PayloadContainer.Content = null;

    //    RootGrid.Children.Clear();
    //    RootGrid.RowDefinitions.Clear();
    //    RootGrid.ColumnDefinitions.Clear();

    //    var splitGrid = new Grid();
    //    var cell1 = new RegionCell();
    //    cell1.InjectPayload(extractedPayload);

    //    var cell2 = new RegionCell();
    //    cell2.InjectPayload(new ContentView { BackgroundColor = GetNextColor() });

    //    var splitter = new GridSplitter {
    //        Orientation = isTopBottomClick ? GridSplitter.SplitOrientation.Vertical : GridSplitter.SplitOrientation.Horizontal,
    //        WidthRequest = isTopBottomClick ? 5 : -1,
    //        HeightRequest = isTopBottomClick ? -1 : 5,
    //        HorizontalOptions = isTopBottomClick ? LayoutOptions.Center : LayoutOptions.Fill,
    //        VerticalOptions = isTopBottomClick ? LayoutOptions.Fill : LayoutOptions.Center
    //    };

    //    if (isTopBottomClick) {
    //        double star1 = clickX / RootGrid.Width;
    //        double star2 = 1.0 - star1;

    //        splitGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(star1, GridUnitType.Star)));
    //        splitGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
    //        splitGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(star2, GridUnitType.Star)));

    //        Grid.SetColumn(cell1, 0);
    //        Grid.SetColumn(splitter, 1);
    //        Grid.SetColumn(cell2, 2);
    //    }
    //    else {
    //        double star1 = clickY / RootGrid.Height;
    //        double star2 = 1.0 - star1;

    //        splitGrid.RowDefinitions.Add(new RowDefinition(new GridLength(star1, GridUnitType.Star)));
    //        splitGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
    //        splitGrid.RowDefinitions.Add(new RowDefinition(new GridLength(star2, GridUnitType.Star)));

    //        Grid.SetRow(cell1, 0);
    //        Grid.SetRow(splitter, 1);
    //        Grid.SetRow(cell2, 2);
    //    }

    //    splitGrid.Children.Add(cell1);
    //    splitGrid.Children.Add(splitter);
    //    splitGrid.Children.Add(cell2);

    //    RootGrid.Children.Add(splitGrid);
    //}

    private void OnCloseButtonTapped(object sender, TappedEventArgs e) {
        if (this.Parent is Grid parentGrid) {
            var splitter = parentGrid.Children.OfType<GridSplitter>().FirstOrDefault();
            if (splitter != null) {
                parentGrid.Children.Remove(splitter);
            }

            parentGrid.Children.Remove(this);

            var sibling = parentGrid.Children.OfType<RegionCell>().FirstOrDefault();
            if (sibling != null) {
                parentGrid.ColumnDefinitions.Clear();
                parentGrid.RowDefinitions.Clear();

                Grid.SetColumn(sibling, 0);
                Grid.SetRow(sibling, 0);
                Grid.SetColumnSpan(sibling, 1);
                Grid.SetRowSpan(sibling, 1);
            }
        }
    }
}