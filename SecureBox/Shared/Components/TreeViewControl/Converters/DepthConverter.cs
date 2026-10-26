using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace SecureBox.Components.TreeViewControl.Converters;

public class DepthConverter : MarkupExtension, IValueConverter, IMultiValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return new Thickness(0);

        int depth = (int)value;
        Thickness margin = new Thickness(depth * 20, 0, 0, 0);
        return margin;
    }

    /// <summary>
    /// Expects the depth, the favorite depth and whether the favorite depth is used
    /// </summary>
    public object Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        bool useFavoriteDepth = values.Count > 2 && values[2] is true;
        object? depth = useFavoriteDepth ? values[1] : values[0];

        return depth is int value && value > 0 ? new Thickness(value * 20, 0, 0, 0) : new Thickness(0);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        return BindingOperations.DoNothing;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
