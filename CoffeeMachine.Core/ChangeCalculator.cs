namespace CoffeeMachine.Core;

public static class ChangeCalculator
{
    // list of cointsn (in cents) that can be returning change
    private static readonly int[] Denominations = [200, 100, 50, 20, 10, 5];

    // Returns the coins to give back
    public static IReadOnlyList<ChangeItem> Calculate(int changeCents)
    {
        if (changeCents < 0 || changeCents % 5 != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(changeCents),
                changeCents,
                "Error while returning change.");
        }

        var remaining = changeCents;
        var items = new List<ChangeItem>(Denominations.Length);

        foreach (var denomination in Denominations)
        {
            var quantity = remaining / denomination;
            remaining %= denomination;

            if (quantity > 0)            
            {
                items.Add(new ChangeItem(denomination, quantity));
            }
        }

        if (remaining != 0)
        {
            throw new InvalidOperationException(
                $"Error while returning change: {changeCents} cents; {remaining} cents left over.");
        }

        return Array.AsReadOnly(items.ToArray());
    }
}
