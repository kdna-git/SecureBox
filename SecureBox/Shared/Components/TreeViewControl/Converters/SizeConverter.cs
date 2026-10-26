using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace SecureBox.Components.TreeViewControl.Converters;

public class SizeConverter : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        value = value ?? 0;
        string[] units = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        double size = long.Parse(value?.ToString() ?? "0");
        int unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            ++unit;
        }
        if (size == 0)
            return "";
        return string.Format("{0:#,##0.#} {1}", size, units[unit]);
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

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
