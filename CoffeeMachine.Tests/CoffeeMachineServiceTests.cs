using CoffeeMachine.Core;

namespace CoffeeMachine.Tests;

public class CoffeeMachineServiceTests
{
    [Fact]
    public void One_cent_coin_is_rejected()
    {
        var machine = new CoffeeMachineService();

        Assert.Equal(MachineError.InvalidCoin, machine.TryInsertCoin(1));
        Assert.Equal(0, machine.BalanceCents);
        Assert.Empty(machine.InsertedCoins);
    }

    [Fact]
    public void Two_cent_coin_is_rejected()
    {
        var machine = new CoffeeMachineService();

        Assert.Equal(MachineError.InvalidCoin, machine.TryInsertCoin(2));
        Assert.Equal(0, machine.BalanceCents);
        Assert.Empty(machine.InsertedCoins);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Invalid_coin_preserves_existing_credit(int rejectedCoin)
    {
        var machine = new CoffeeMachineService();
        machine.TryInsertCoin(100);

        Assert.Equal(MachineError.InvalidCoin, machine.TryInsertCoin(rejectedCoin));
        Assert.Equal(100, machine.BalanceCents);
        Assert.Equal([100], machine.InsertedCoins);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    public void Every_valid_coin_is_accepted(int cents)
    {
        var machine = new CoffeeMachineService();

        Assert.Equal(MachineError.None, machine.TryInsertCoin(cents));
        Assert.Equal(cents, machine.BalanceCents);
        Assert.Equal([cents], machine.InsertedCoins);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(3)]
    [InlineData(25)]
    [InlineData(500)]
    public void Values_outside_the_accepted_set_are_rejected(int cents)
    {
        var machine = new CoffeeMachineService();

        Assert.Equal(MachineError.InvalidCoin, machine.TryInsertCoin(cents));
        Assert.Equal(0, machine.BalanceCents);
        Assert.Empty(machine.InsertedCoins);
    }

    [Fact]
    public void Balance_is_the_sum_of_the_accepted_coins()
    {
        var machine = Machine(200, 100, 50, 20, 10, 5);

        Assert.Equal(385, machine.BalanceCents);
        Assert.Equal([200, 100, 50, 20, 10, 5], machine.InsertedCoins);
    }

    [Theory]
    [InlineData(CoffeeType.Cappuccino, 350)]
    [InlineData(CoffeeType.Latte, 300)]
    [InlineData(CoffeeType.Decaf, 400)]
    public void Coffee_prices_are_fixed(CoffeeType coffeeType, int expectedPriceCents)
    {
        Assert.Equal(expectedPriceCents, CoffeeMachineService.GetPriceCents(coffeeType));
    }

    [Fact]
    public void Insufficient_balance_keeps_the_whole_credit()
    {
        var machine = Machine(200);

        var error = machine.TryPurchase(CoffeeType.Cappuccino, out var purchase);

        Assert.Equal(MachineError.InsufficientFunds, error);
        Assert.Null(purchase);
        Assert.Null(machine.LatestPurchase);
        Assert.Equal(200, machine.BalanceCents);
        Assert.Equal([200], machine.InsertedCoins);
    }

    [Theory]
    [InlineData(CoffeeType.Cappuccino, 350, new[] { 200, 100, 50 })]
    [InlineData(CoffeeType.Latte, 300, new[] { 200, 100 })]
    [InlineData(CoffeeType.Decaf, 400, new[] { 200, 200 })]
    public void Exact_payment_returns_no_change(
        CoffeeType coffeeType, int expectedPriceCents, int[] coins)
    {
        var machine = Machine(coins);

        var error = machine.TryPurchase(coffeeType, out var purchase);

        Assert.Equal(MachineError.None, error);
        Assert.NotNull(purchase);
        Assert.Equal(coffeeType, purchase.CoffeeType);
        Assert.Equal(expectedPriceCents, purchase.CoffeePriceCents);
        Assert.Equal(expectedPriceCents, purchase.InsertedCents);
        Assert.Equal(0, purchase.ChangeCents);
        Assert.Empty(purchase.Change);
    }

    [Fact]
    public void Overpayment_returns_the_change_breakdown()
    {
        var machine = Machine(200, 200, 100);

        var error = machine.TryPurchase(CoffeeType.Cappuccino, out var purchase);

        Assert.Equal(MachineError.None, error);
        Assert.NotNull(purchase);
        Assert.Equal(500, purchase.InsertedCents);
        Assert.Equal(150, purchase.ChangeCents);
        Assert.Equal([new ChangeItem(100, 1), new ChangeItem(50, 1)], purchase.Change);
    }

    [Fact]
    public void Complex_purchase_returns_six_denominations()
    {
        var machine = Machine(200, 200, 200, 100, 20, 10, 5);

        var error = machine.TryPurchase(CoffeeType.Cappuccino, out var purchase);

        Assert.Equal(MachineError.None, error);
        Assert.NotNull(purchase);
        Assert.Equal(735, purchase.InsertedCents);
        Assert.Equal(385, purchase.ChangeCents);
        Assert.Equal(
            [
                new ChangeItem(200, 1),
                new ChangeItem(100, 1),
                new ChangeItem(50, 1),
                new ChangeItem(20, 1),
                new ChangeItem(10, 1),
                new ChangeItem(5, 1)
            ],
            purchase.Change);
    }

    [Fact]
    public void Successful_purchase_resets_the_credit_and_stores_the_receipt()
    {
        var machine = Machine(200, 200);

        machine.TryPurchase(CoffeeType.Decaf, out var purchase);

        Assert.Equal(0, machine.BalanceCents);
        Assert.Empty(machine.InsertedCoins);
        Assert.Same(purchase, machine.LatestPurchase);
    }

    [Fact]
    public void Receipt_survives_the_next_insertion()
    {
        var machine = Machine(200, 200);
        machine.TryPurchase(CoffeeType.Decaf, out var purchase);

        machine.TryInsertCoin(200);

        Assert.Equal(200, machine.BalanceCents);
        Assert.Same(purchase, machine.LatestPurchase);
        Assert.Equal(400, machine.LatestPurchase!.InsertedCents);
        Assert.Equal(CoffeeType.Decaf, machine.LatestPurchase.CoffeeType);
    }

    [Fact]
    public void Consecutive_purchases_use_independent_credit()
    {
        var machine = Machine(200, 100);
        machine.TryPurchase(CoffeeType.Latte, out var firstPurchase);

        machine.TryInsertCoin(200);
        machine.TryInsertCoin(200);
        machine.TryInsertCoin(50);
        var error = machine.TryPurchase(CoffeeType.Decaf, out var secondPurchase);

        Assert.Equal(MachineError.None, error);
        Assert.NotNull(firstPurchase);
        Assert.NotNull(secondPurchase);
        Assert.NotEqual(firstPurchase.TransactionId, secondPurchase.TransactionId);
        Assert.Equal(300, firstPurchase.InsertedCents);
        Assert.Equal(450, secondPurchase.InsertedCents);
        Assert.Equal(50, secondPurchase.ChangeCents);
        Assert.Same(secondPurchase, machine.LatestPurchase);
    }

    [Fact]
    public void Repeating_a_purchase_without_new_coins_fails_and_keeps_the_receipt()
    {
        var machine = Machine(200, 200);
        machine.TryPurchase(CoffeeType.Decaf, out var purchase);

        var error = machine.TryPurchase(CoffeeType.Decaf, out var secondPurchase);

        Assert.Equal(MachineError.InsufficientFunds, error);
        Assert.Null(secondPurchase);
        Assert.Same(purchase, machine.LatestPurchase);
        Assert.Equal(0, machine.BalanceCents);
    }

    [Fact]
    public void Unknown_coffee_type_is_an_internal_state_error()
    {
        var machine = Machine(200, 200, 200);

        var error = machine.TryPurchase((CoffeeType)999, out var purchase);

        Assert.Equal(MachineError.InvalidInternalState, error);
        Assert.Null(purchase);
        Assert.Null(machine.LatestPurchase);
        Assert.Equal(600, machine.BalanceCents);
    }

    [Fact]
    public void Receipt_invariants_hold()
    {
        var before = DateTimeOffset.UtcNow;
        var machine = Machine(200, 200, 100);

        machine.TryPurchase(CoffeeType.Cappuccino, out var purchase);
        var after = DateTimeOffset.UtcNow;

        Assert.NotNull(purchase);
        Assert.NotEqual(Guid.Empty, purchase.TransactionId);
        Assert.InRange(purchase.CreatedAt, before, after);
        Assert.Equal(TimeSpan.Zero, purchase.CreatedAt.Offset);
        Assert.Equal(
            purchase.InsertedCents,
            purchase.CoffeePriceCents + purchase.ChangeCents);
        Assert.Equal(
            purchase.ChangeCents,
            purchase.Change.Sum(item => item.DenominationCents * item.Quantity));
    }

    private static CoffeeMachineService Machine(params int[] coins)
    {
        var machine = new CoffeeMachineService();

        foreach (var cents in coins)
        {
            Assert.Equal(MachineError.None, machine.TryInsertCoin(cents));
        }

        return machine;
    }
}
