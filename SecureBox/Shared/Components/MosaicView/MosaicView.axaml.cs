using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using SecureBox.ViewModels;

namespace SecureBox.Views.Components;

/// <summary>
/// Shows the folders and files of an <see cref="ItemsViewModel"/> as two grids of cards
/// </summary>
public partial class MosaicView : UserControl
{
    private bool _updatingSelection;

    public MosaicView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Both lists share the selection of the view model, so selecting an item in
    /// one of them clears the selection of the other
    /// </summary>
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_updatingSelection || sender is not ListBox list || list.SelectedItem is null)
            return;

        if (DataContext is not ItemsViewModel vm)
            return;

        _updatingSelection = true;
        try
        {
            var other = list == DirectoriesList ? FilesList : DirectoriesList;
            other.SelectedItem = null;
            vm.SelectedItem = list.SelectedItem as FileItemViewModel;
        }
        finally
        {
            _updatingSelection = false;
        }
    }

    /// <summary>
    /// Opens a folder, or a file in the modal, when its card is double tapped
    /// </summary>
    private void OnItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Control source || DataContext is not ItemsViewModel vm)
            return;

        // A double tap on the favorite button only toggles the favorite
        if (source.FindAncestorOfType<Button>(includeSelf: true) is not null)
            return;

        var item =
            source.FindAncestorOfType<ListBoxItem>(includeSelf: true)?.DataContext
            as FileItemViewModel;
        if (item is null)
            return;

        if (vm.DoubleClickCommand.CanExecute(item))
            vm.DoubleClickCommand.Execute(item);

        e.Handled = true;
    }
}
