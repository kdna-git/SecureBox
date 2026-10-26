using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FluentIcons.Common;

namespace MVVM.Core.Extensions;

/// <summary>
/// Provides attached properties for configuring icons and icon-related
/// settings on Avalonia controls.
/// </summary>
public class ExtensionIcons
{
    #region Icon

    /// <summary>
    /// Defines the <see cref="Icon"/> attached property.
    /// </summary>
    public static readonly AttachedProperty<Symbol> IconProperty =
        AvaloniaProperty.RegisterAttached<ExtensionIcons, Control, Symbol>(
            "Icon",
            defaultValue: Symbol.Person
        );

    /// <summary>
    /// Sets the icon symbol for the specified control.
    /// </summary>
    /// <param name="control">The control on which to set the icon.</param>
    /// <param name="value">The icon symbol to display.</param>
    public static void SetIcon(Control control, Symbol value) =>
        control.SetValue(IconProperty, value);

    /// <summary>
    /// Gets the icon symbol assigned to the specified control.
    /// </summary>
    /// <param name="control">The control from which to retrieve the icon.</param>
    /// <returns>The configured icon symbol.</returns>
    public static Symbol GetIcon(Control control) => control.GetValue(IconProperty);

    #endregion

    #region IconSize

    /// <summary>
    /// Defines the <see cref="IconSize"/> attached property.
    /// </summary>
    public static readonly AttachedProperty<double> IconSizeProperty =
        AvaloniaProperty.RegisterAttached<ExtensionIcons, Control, double>(
            "IconSize",
            defaultValue: 16.0
        );

    /// <summary>
    /// Sets the icon size for the specified control.
    /// </summary>
    /// <param name="control">The control on which to set the icon size.</param>
    /// <param name="value">The icon size in device-independent pixels.</param>
    public static void SetIconSize(Control control, double value) =>
        control.SetValue(IconSizeProperty, value);

    /// <summary>
    /// Gets the icon size assigned to the specified control.
    /// </summary>
    /// <param name="control">The control from which to retrieve the icon size.</param>
    /// <returns>The configured icon size.</returns>
    public static double GetIconSize(Control control) => control.GetValue(IconSizeProperty);

    #endregion

    #region IconFont

    /// <summary>
    /// Defines the <see cref="IconFont"/> attached property.
    /// </summary>
    public static readonly AttachedProperty<FontFamily> IconFontProperty =
        AvaloniaProperty.RegisterAttached<ExtensionIcons, Control, FontFamily>(
            "IconFont",
            defaultValue: new FontFamily("Segoe UI")
        );

    /// <summary>
    /// Gets the icon font assigned to the specified control.
    /// </summary>
    /// <param name="control">The control from which to retrieve the icon font.</param>
    /// <returns>The configured icon font.</returns>
    public static FontFamily GetIconFont(Control control) => control.GetValue(IconFontProperty);

    /// <summary>
    /// Sets the icon font for the specified control.
    /// </summary>
    /// <param name="control">The control on which to set the icon font.</param>
    /// <param name="value">The font family used to render the icon.</param>
    public static void SetIconFont(Control control, FontFamily value) =>
        control.SetValue(IconFontProperty, value);

    #endregion
}
