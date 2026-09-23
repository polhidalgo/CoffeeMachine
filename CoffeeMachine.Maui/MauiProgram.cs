using System.Reflection;
using System.Text.Json;
using CoffeeMachine.Core;
using CoffeeMachine.Maui.Configuration;
using CoffeeMachine.Maui.Services;
using Microsoft.Extensions.Logging;

namespace CoffeeMachine.Maui;

public static class MauiProgram
{
    private const string SupabaseResourceName = "CoffeeMachine.Maui.supabase.json";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddSingleton(ReadSupabaseOptions());
        builder.Services.AddSingleton<CoffeeMachineService>();
        builder.Services.AddSingleton<SupabaseTransactionService>();
        builder.Services.AddSingleton(_ => new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        });
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    // Reads the embedded settings without assuming a working directory
    private static SupabaseOptions ReadSupabaseOptions()
    {
        try
        {
            using var stream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream(SupabaseResourceName);

            if (stream is null) // if not exists returns empty (Does not break, as bbdd is not mandatory)
            {
                return new SupabaseOptions();
            }

            return JsonSerializer.Deserialize<SupabaseOptions>(stream) ?? new SupabaseOptions();
        }
        catch 
        {
            return new SupabaseOptions();
        }
    }
}
