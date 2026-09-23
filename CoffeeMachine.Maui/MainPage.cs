using Microsoft.AspNetCore.Components.WebView.Maui;

namespace CoffeeMachine.Maui;

// Hosts the BlazorWebView
public sealed class MainPage : ContentPage
{
    public MainPage()
    {
        var webView = new BlazorWebView
        {
            HostPage = "wwwroot/index.html"
        };

        webView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(Components.CoffeeMachine)
        });

        Content = webView;
    }
}
