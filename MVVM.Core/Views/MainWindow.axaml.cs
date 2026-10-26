using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace MVVM.Core.Views;

public sealed partial class MainWindow : Window
{
    private const int ResizeBorderThickness = 6;

    public MainWindow()
    {
        InitializeComponent();

        SetUpCorners();

        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty && OperatingSystem.IsWindows())
            {
                Padding =
                    WindowState == WindowState.Maximized ? new Thickness(0) : new Thickness(8);

                SetUpCorners();
            }
        };
    }

    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            return;

        var point = e.GetPosition(this);
        var edge = GetEdge(point);

        if (edge is not null)
        {
            BeginResizeDrag(edge.Value, e);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            return;

        var point = e.GetPosition(this);

        var edge = GetEdge(point);

        Cursor = edge switch
        {
            WindowEdge.West or WindowEdge.East => new Cursor(StandardCursorType.SizeWestEast),
            WindowEdge.North or WindowEdge.South => new Cursor(StandardCursorType.SizeNorthSouth),
            WindowEdge.NorthWest or WindowEdge.SouthEast => new Cursor(
                StandardCursorType.BottomRightCorner
            ),
            WindowEdge.NorthEast or WindowEdge.SouthWest => new Cursor(
                StandardCursorType.BottomLeftCorner
            ),
            _ => new Cursor(StandardCursorType.Arrow),
        };
    }

    private WindowEdge? GetEdge(Point p)
    {
        bool left = p.X <= ResizeBorderThickness;
        bool right = p.X >= Bounds.Width - ResizeBorderThickness;
        bool top = p.Y <= ResizeBorderThickness;
        bool bottom = p.Y >= Bounds.Height - ResizeBorderThickness;

        if (left && top)
            return WindowEdge.NorthWest;
        if (right && top)
            return WindowEdge.NorthEast;
        if (left && bottom)
            return WindowEdge.SouthWest;
        if (right && bottom)
            return WindowEdge.SouthEast;

        if (left)
            return WindowEdge.West;
        if (right)
            return WindowEdge.East;
        if (top)
            return WindowEdge.North;
        if (bottom)
            return WindowEdge.South;

        return null;
    }

    private void SetUpCorners()
    {
        var maximized = WindowState == WindowState.Maximized;

        if (this.FindControl<Border>("HeaderBorder") is { } header)
            header.CornerRadius = maximized ? new CornerRadius(0) : new CornerRadius(12, 12, 0, 0);

        if (this.FindControl<Border>("SidebarBorder") is { } sidebar)
            sidebar.CornerRadius = maximized ? new CornerRadius(0) : new CornerRadius(0, 0, 0, 12);

        if (this.FindControl<Border>("ContentBorder") is { } content)
            content.CornerRadius = maximized ? new CornerRadius(0) : new CornerRadius(0, 0, 12, 0);
    }
}
