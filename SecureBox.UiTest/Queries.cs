using Avalonia.Controls;
using Avalonia.VisualTree;
using FluentIcons.Avalonia;
using FluentIcons.Common;
using SecureBox.Components.TreeViewControl;
using SecureBox.ViewModels;
using Shape = Avalonia.Controls.Shapes.Path;

namespace SecureBox.UiTest;

/// <summary>
/// Reads what the views render, as a person looking at the window would see it
/// </summary>
internal static class Queries
{
    public static string ItemName(this Control control) =>
        ((FileItemViewModel)control.DataContext!).Name;

    /// <summary>
    /// The top level rows of the tree, excluding the rows of collapsed folders
    /// </summary>
    public static List<TreeViewItemControl> TreeRows(this AppHarness app) =>
        [.. app.Visible<TreeViewItemControl>().Where(r => r.DataContext is FileItemViewModel)];

    public static TreeViewItemControl TreeRow(this AppHarness app, string name) =>
        app.TreeRows().Single(r => r.ItemName() == name);

    public static List<string> TreeNames(this AppHarness app) =>
        [.. app.TreeRows().Select(r => r.ItemName())];

    /// <summary>
    /// The cards of one of the two mosaic lists, "DirectoriesList" or "FilesList"
    /// </summary>
    public static List<ListBoxItem> Cards(this AppHarness app, string list)
    {
        var listBox = app.Visible<ListBox>().FirstOrDefault(l => l.Name == list);
        if (listBox is null)
            return [];

        return
        [
            .. listBox
                .GetVisualDescendants()
                .OfType<ListBoxItem>()
                .Where(i => i.IsEffectivelyVisible && i.Bounds.Width > 0),
        ];
    }

    public static List<string> CardNames(this AppHarness app, string list) =>
        [.. app.Cards(list).Select(c => c.ItemName())];

    public static List<string> BreadCrumb(this AppHarness app) =>
        [
            .. app.Visible<Button>()
                .Where(b => b.Classes.Contains("BreadCrumb"))
                .Select(b => (string)b.Content!),
        ];

    /// <summary>
    /// The variant of the favorite star drawn inside a row or a card
    /// </summary>
    public static IconVariant Star(this Control control) =>
        control.GetVisualDescendants().OfType<SymbolIcon>().Single(i => i.Symbol == Symbol.Star).IconVariant;

    /// <summary>
    /// Whether the file icon of a row or a card has a shape to draw
    /// </summary>
    public static bool HasIcon(this Control control) =>
        control
            .GetVisualDescendants()
            .OfType<Shape>()
            .Any(p => p.Name == "Icon" && p.Data is not null && p.Bounds.Width > 0);
}
