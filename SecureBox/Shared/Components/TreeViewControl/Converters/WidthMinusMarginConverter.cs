using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SecureBox.Components.TreeViewControl.Converters;

public class WidthMinusMarginConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            // Subtract margin (10 on each side)
            return Math.Max(0, width - 20);
        }
        return 0;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        throw new NotImplementedException();
    }
}
