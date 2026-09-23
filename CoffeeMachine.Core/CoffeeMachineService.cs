namespace CoffeeMachine.Core;

// Main logic 
public sealed class CoffeeMachineService
{

    private const int CoinIncrementCents = 5;

    private readonly List<int> _insertedCoins = [];
    private readonly IReadOnlyList<int> _insertedCoinsView;

    public CoffeeMachineService()
    {
        _insertedCoinsView = _insertedCoins.AsReadOnly();
    }

    public int BalanceCents => _insertedCoins.Sum();

    public IReadOnlyList<int> InsertedCoins => _insertedCoinsView;

    public PurchaseResult? LatestPurchase { get; private set; }

    public static bool IsAcceptedCoin(int cents) =>
        cents is 5 or 10 or 20 or 50 or 100 or 200;

    // get coffe prices
    public static int GetPriceCents(CoffeeType coffeeType) => coffeeType switch
    {
        CoffeeType.Cappuccino => 350,
        CoffeeType.Latte => 300,
        CoffeeType.Decaf => 400,
        _ => throw new ArgumentOutOfRangeException(
            nameof(coffeeType), coffeeType, "Error Unknown coffee type.") // If manages to enter an unknown enum
    };

    // Inserts coin
    public MachineError TryInsertCoin(int cents)
    {
        if (!IsAcceptedCoin(cents))
        {
            return MachineError.InvalidCoin;
        }

        try
        {
            checked
            {
                _ = BalanceCents + cents;
            }
        }
        catch (OverflowException)
        {
            return MachineError.InvalidInternalState;
        }

        _insertedCoins.Add(cents);
        return MachineError.None;
    }

    // Try purchase
    public MachineError TryPurchase(CoffeeType coffeeType, out PurchaseResult? purchase)
    {
        purchase = null;

        if (!Enum.IsDefined(coffeeType)) // ensures enum exists (type of coffee)
        {
            return MachineError.InvalidInternalState;
        }

        int balanceCents;
        int priceCents;
        int changeCents;
        IReadOnlyList<ChangeItem> change;

        try
        {
            balanceCents = BalanceCents;
            priceCents = GetPriceCents(coffeeType);

            if (balanceCents < 0 || balanceCents % CoinIncrementCents != 0) // if balance is not multiple of 5 theres an error (or ofc negative gives an error too)
            {
                return MachineError.InvalidInternalState;
            }

            if (balanceCents < priceCents) // check funds
            {
                return MachineError.InsufficientFunds;
            }

            changeCents = balanceCents - priceCents;
            change = ChangeCalculator.Calculate(changeCents); // calculates change to return

            if (SumOf(change) != changeCents)// checks that changes calculated by quantity and coin equals the resting balance
            {
                return MachineError.InvalidInternalState;
            }
        }
        catch (Exception ex)
            when (ex is OverflowException
                or ArgumentOutOfRangeException
                or InvalidOperationException)
        {
            return MachineError.InvalidInternalState;
        }

        var result = new PurchaseResult(
            Guid.NewGuid(),
            coffeeType,
            priceCents,
            balanceCents,
            change,
            DateTimeOffset.UtcNow);

        LatestPurchase = result; // shows the "receipt"
        _insertedCoins.Clear();

        purchase = result;
        return MachineError.None;
    }

    private static long SumOf(IReadOnlyList<ChangeItem> change)
    {
        long total = 0;

        foreach (var item in change)
        {
            total += (long)item.DenominationCents * item.Quantity;
        }

        return total;
    }
}
