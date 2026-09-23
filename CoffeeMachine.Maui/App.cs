namespace CoffeeMachine.Maui;

// Single window hosting the machine screen, opens MainPage window
public sealed class App(MainPage mainPage) : Application
{
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(mainPage) { Title = "Virtual Coffee Machine" };
}
