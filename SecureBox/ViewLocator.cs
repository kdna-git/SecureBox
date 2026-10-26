using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MVVM.Core.ViewModels;
using System.Diagnostics.CodeAnalysis;

namespace SecureBox;

/// <summary>
/// Resolves view models to their corresponding Avalonia views using naming conventions.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Creates a view corresponding to the supplied view model.
    /// </summary>
    /// <param name="param">The view model used to resolve the corresponding view.</param>
    /// <returns>
    /// The resolved view, or a placeholder text block when no matching view is found.
    /// </returns>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        // Resolve the view using the view model's namespace and naming convention.
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type is not null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        // Fall back to the application's Views namespace for shared MVVM.Core view models.
        name = name.Replace("MVVM.Core.Views", "SecureBox.Views", StringComparison.Ordinal);
        type = Type.GetType(name);

        if (type is not null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        // Return a visible placeholder when the expected view cannot be resolved.
        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <summary>
    /// Determines whether the supplied data is a view model that can be resolved by this locator.
    /// </summary>
    /// <param name="data">The data object to evaluate.</param>
    /// <returns><see langword="true"/> when the data is a <see cref="ViewModelBase"/>; otherwise, <see langword="false"/>.</returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
