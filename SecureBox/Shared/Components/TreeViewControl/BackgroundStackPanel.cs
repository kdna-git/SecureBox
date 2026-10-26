using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SecureBox.Components.TreeViewControl;

public class BackgroundStackPanel : StackPanel
{
    public static readonly StyledProperty<double> ItemHeightProperty = AvaloniaProperty.Register<
        BackgroundStackPanel,
        double
    >(nameof(ItemHeight), 0.0);

    public double ItemHeight
    {
        get => GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public static readonly StyledProperty<double> RowHeightProperty = AvaloniaProperty.Register<
        BackgroundStackPanel,
        double
    >(nameof(RowHeight), 30.0);

    public double RowHeight
    {
        get => GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    public static readonly StyledProperty<IBrush> OddRowBrushProperty = AvaloniaProperty.Register<
        BackgroundStackPanel,
        IBrush
    >(nameof(OddRowBrush), new SolidColorBrush(Color.Parse("#1f1f1f")));

    public IBrush OddRowBrush
    {
        get => GetValue(OddRowBrushProperty);
        set => SetValue(OddRowBrushProperty, value);
    }

    public static readonly StyledProperty<IBrush> EvenRowBrushProperty = AvaloniaProperty.Register<
        BackgroundStackPanel,
        IBrush
    >(nameof(EvenRowBrush), new SolidColorBrush(Color.Parse("#2a2a2a")));

    public IBrush EvenRowBrush
    {
        get => GetValue(EvenRowBrushProperty);
        set => SetValue(EvenRowBrushProperty, value);
    }

    public BackgroundStackPanel()
    {
        this.GetObservable(ItemHeightProperty).Subscribe(new ItemHeightObserver(this));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // The rows copy the brushes, so they are rebuilt when the theme changes them
        if (change.Property == OddRowBrushProperty || change.Property == EvenRowBrushProperty)
            RebuildChildren();
    }

    private void RebuildChildren()
    {
        Children.Clear();
        if (ItemHeight <= 0)
            return;

        Height = ItemHeight;

        int index = (int)(ItemHeight / RowHeight);

        for (int i = 0; i < index; i++)
        {
            var border = new Border { Height = RowHeight, Background = i % 2 == 0 ? OddRowBrush : EvenRowBrush };
            Children.Add(border);
        }
    }

    private class ItemHeightObserver : IObserver<double>
    {
        private readonly BackgroundStackPanel _panel;

        public ItemHeightObserver(BackgroundStackPanel panel)
        {
            _panel = panel;
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(double value)
        {
            _panel.RebuildChildren();
        }
    }
}