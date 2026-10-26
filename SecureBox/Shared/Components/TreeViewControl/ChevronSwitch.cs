using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace SecureBox.Components.TreeViewControl;

public class ChevronSwitch : ToggleButton
{
    // Definir una propiedad de dependencia para el color de relleno
    public static readonly StyledProperty<IBrush> ChevronColorProperty = AvaloniaProperty.Register<
        ChevronSwitch,
        IBrush
    >(nameof(ChevronColor), new SolidColorBrush(Color.Parse("#555555")));

    public Brush ChevronColor
    {
        get { return (Brush)GetValue(ChevronColorProperty); }
        set { SetValue(ChevronColorProperty, value); }
    }
}
