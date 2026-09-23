using CoffeeMachine.Maui.Configuration;
using Microsoft.AspNetCore.Components;

namespace CoffeeMachine.Maui.Components;

// Image. It shows in when it loads and disappears when it fails, so no connection never leaves a broken icon.
public partial class RemoteImage : ComponentBase
{
    private string? _requestedPath;
    private bool _loaded;
    private bool _failed;

    [Parameter, EditorRequired]
    public string Path { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Alt { get; set; } = string.Empty;

    [Parameter]
    public string CssClass { get; set; } = string.Empty;

    [Inject]
    private SupabaseOptions Options { get; set; } = default!;

    private string ImageUrl => Options.BuildPublicAssetUrl(Path);

    private string ImageClass =>
        _loaded
            ? $"remote-image remote-image--loaded {CssClass}".Trim()
            : $"remote-image {CssClass}".Trim();

    // Only reset when the parent points this component at a different file.
    // The parent re-renders on every coin, and resetting there would hide an
    // image that already loaded and will not raise the load event again.
    protected override void OnParametersSet()
    {
        if (_requestedPath == Path)
        {
            return;
        }

        _requestedPath = Path;
        _loaded = false;
        _failed = false;
    }

    private void HandleLoaded() => _loaded = true;

    private void HandleError() => _failed = true;
}
