using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using SecureBox.ViewModels;

namespace SecureBox.Components.TreeViewControl;

public class TreeViewItemControl : TreeViewItem
{
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        var item = e.NameScope.Find<Grid>("Item");
        if (item != null)
        {
            item.PointerPressed += Border_PointerPressed;
        }
    }

    protected override Control CreateContainerForItemOverride(
        object? item,
        int index,
        object? recycleKey
    )
    {
        return new TreeViewItemControl();
    }

    private void Border_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        this.IsSelected = true;

        // Check for double click
        if (e.ClickCount != 2)
            return;

        // We check the data context and the type
        if (sender is not Control fe || fe.DataContext is not FileItemViewModel fi)
            return;

        // We check the parent data context
        var userControl = fe.FindAncestorOfType<UserControl>();
        if (
            userControl is null
            || userControl.DataContext is not ItemsViewModel vm
            || fi.Type != "File"
        )
            return;

        // We check if the command can execute
        if (vm.DoubleClickCommand?.CanExecute(fi) == true)
        {
            vm.DoubleClickCommand.Execute(fi);
            // We mark the event as handled
            e.Handled = true;
        }
    }

    public static T? FindAncestor<T>(Visual current)
        where T : Visual
    {
        return current.FindAncestorOfType<T>();
    }

    public static readonly StyledProperty<string> TypeProperty = AvaloniaProperty.Register<
        TreeViewItemControl,
        string
    >(nameof(Type));

    public string Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is TreeViewItemControl)
        {
            recycleKey = null;
            return false;
        }
        recycleKey = typeof(TreeViewItemControl);
        return true;
    }
}
