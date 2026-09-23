using System.Globalization;

namespace CoffeeMachine.Maui.Components;

// Format coins to dollars
internal static class MoneyFormat
{
    //Renders cents as 350 = $3.50
    public static string Amount(int cents) =>
        "$" + (cents / 100m).ToString("0.00", CultureInfo.InvariantCulture);

    //Renders a coin as 50 0 50c only when its smaller than 100 then it shows as $ 100 = 1$
    public static string Coin(int cents) =>
        cents < 100
            ? cents.ToString(CultureInfo.InvariantCulture) + "c"
            : "$" + (cents / 100m).ToString("0.##", CultureInfo.InvariantCulture);
}
