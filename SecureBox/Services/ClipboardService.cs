using Avalonia.Input.Platform;
using SecureBox.DI;

namespace SecureBox.Services;

public interface IClipboardService
{
    Task SetTextAsync(string text);
}

public class ClipboardService(TopLevelProvider topLevelProvider) : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        var clipboard = topLevelProvider.GetTopLevel()?.Clipboard;
        if (clipboard is null)
            return;

        await clipboard.SetTextAsync(text);
    }
}
