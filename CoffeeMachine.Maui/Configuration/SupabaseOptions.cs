namespace CoffeeMachine.Maui.Configuration;

// Local Supabase settings
public sealed class SupabaseOptions
{
    public string Url { get; init; } = string.Empty;
    public string PublishableKey { get; init; } = string.Empty;
    public string AssetBucket { get; init; } = "images";

    //True when both look usable, a small check without having to connect
    public bool IsConfigured =>
        PublishableKey.StartsWith("sb_publishable_", StringComparison.Ordinal)
        && Uri.TryCreate(Url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;

    public bool CanLoadAssets =>
        Uri.TryCreate(Url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && !string.IsNullOrWhiteSpace(AssetBucket);

    public string BuildPublicAssetUrl(string path)
    {
        if (!CanLoadAssets)
        {
            return string.Empty;
        }

        return $"{Url.TrimEnd('/')}/storage/v1/object/public/" +
               $"{AssetBucket}/{path.TrimStart('/')}";
    }
}
