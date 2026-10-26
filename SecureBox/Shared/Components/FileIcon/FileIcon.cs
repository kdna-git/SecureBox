using Avalonia;
using Avalonia.Controls.Primitives;

namespace SecureBox.Components.FileIcon;

public class FileIcon : TemplatedControl
{
    public static readonly StyledProperty<string> ExtensionProperty = AvaloniaProperty.Register<
        FileIcon,
        string
    >(nameof(Extension));

    public string Extension
    {
        get => GetValue(ExtensionProperty);
        set => SetValue(ExtensionProperty, value);
    }

    public static readonly StyledProperty<string> TypeProperty = AvaloniaProperty.Register<
        FileIcon,
        string
    >(nameof(Type));
    public string Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }
}