using Avalonia.Controls;

namespace SecureBox.DI;

public class TopLevelProvider
{
    public Func<TopLevel?>? TopLevelSupplier { get; set; }

    public TopLevel? GetTopLevel() => TopLevelSupplier?.Invoke();
}
