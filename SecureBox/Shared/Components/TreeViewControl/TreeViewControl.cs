using Avalonia;
using Avalonia.Controls;

namespace SecureBox.Components.TreeViewControl;

public class TreeViewControl : TreeView
{
    public static readonly StyledProperty<object?> BindableSelectedItemProperty =
        AvaloniaProperty.Register<TreeViewControl, object?>(nameof(BindableSelectedItem));

    public object? BindableSelectedItem
    {
        get => GetValue(BindableSelectedItemProperty);
        set => SetValue(BindableSelectedItemProperty, value);
    }

    /// <summary>
    /// Indents the items by their depth below the favorite that contains them,
    /// instead of by their depth in the vault
    /// </summary>
    public static readonly StyledProperty<bool> UseFavoriteDepthProperty =
        AvaloniaProperty.Register<TreeViewControl, bool>(nameof(UseFavoriteDepth));

    public bool UseFavoriteDepth
    {
        get => GetValue(UseFavoriteDepthProperty);
        set => SetValue(UseFavoriteDepthProperty, value);
    }

    public TreeViewControl()
    {
        this.PropertyChanged += (s, e) =>
        {
            if (e.Property == SelectedItemProperty)
            {
                BindableSelectedItem = this.SelectedItem;
            }
        };
    }

    protected override Control CreateContainerForItemOverride(
        object? item,
        int index,
        object? recycleKey
    )
    {
        return new TreeViewItemControl();
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
