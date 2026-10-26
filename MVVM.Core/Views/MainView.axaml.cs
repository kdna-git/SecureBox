using Avalonia.Controls;
using Avalonia.Interactivity;
using MVVM.Core.ViewModels;

namespace MVVM.Core.Views;

public sealed partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.BackRequested += OnBackRequested;
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.BackRequested -= OnBackRequested;
        }

        base.OnUnloaded(e);
    }

    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.NavigateBackCommand.CanExecute(null))
        {
            vm.NavigateBackCommand.Execute(null);
            e.Handled = true;
        }
    }
}
