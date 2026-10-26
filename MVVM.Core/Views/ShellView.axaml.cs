using Avalonia.Controls;
using Avalonia.Input;
using MVVM.Core.ViewModels;

namespace MVVM.Core.Views;

public sealed partial class ShellView : UserControl
{
    public ShellView()
    {
        InitializeComponent();
    }

    private void OnBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsNavigationDrawerOpen = false;
        }
    }
}
