using CoffeeMachine.Core;

namespace CoffeeMachine.Tests;

public class ChangeCalculatorTests
{
    private static readonly int[] AllowedDenominations = [200, 100, 50, 20, 10, 5];

    [Fact]
    public void Zero_change_returns_no_coins()
    {
        Assert.Empty(ChangeCalculator.Calculate(0));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    public void Single_denomination_returns_one_coin(int denomination)
    {
        var change = ChangeCalculator.Calculate(denomination);

        var item = Assert.Single(change);
        Assert.Equal(denomination, item.DenominationCents);
        Assert.Equal(1, item.Quantity);
    }

    [Fact]
    public void Fifteen_cents_returns_ten_and_five()
    {
        Assert.Equal(
            [new ChangeItem(10, 1), new ChangeItem(5, 1)],
            ChangeCalculator.Calculate(15));
    }

    [Fact]
    public void Forty_cents_returns_two_twenties()
    {
        Assert.Equal(
            [new ChangeItem(20, 2)],
            ChangeCalculator.Calculate(40));
    }

    [Fact]
    public void One_hundred_fifty_cents_returns_hundred_and_fifty()
    {
        Assert.Equal(
            [new ChangeItem(100, 1), new ChangeItem(50, 1)],
            ChangeCalculator.Calculate(150));
    }

    [Fact]
    public void Three_hundred_eighty_five_cents_returns_one_of_each_denomination()
    {
        Assert.Equal(
            [
                new ChangeItem(200, 1),
                new ChangeItem(100, 1),
                new ChangeItem(50, 1),
                new ChangeItem(20, 1),
                new ChangeItem(10, 1),
                new ChangeItem(5, 1)
            ],
            ChangeCalculator.Calculate(385));
    }

    [Fact]
    public void Four_hundred_ninety_cents_repeats_denominations()
    {
        Assert.Equal(
            [new ChangeItem(200, 2), new ChangeItem(50, 1), new ChangeItem(20, 2)],
            ChangeCalculator.Calculate(490));
    }

    [Fact]
    public void Six_hundred_cents_returns_three_two_euro_coins()
    {
        Assert.Equal(
            [new ChangeItem(200, 3)],
            ChangeCalculator.Calculate(600));
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    public void Invalid_amounts_are_rejected(int changeCents)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ChangeCalculator.Calculate(changeCents));
    }

    [Fact]
    public void Every_multiple_of_five_up_to_one_thousand_holds_the_invariants()
    {
        for (var changeCents = 0; changeCents <= 1000; changeCents += 5)
        {
            var change = ChangeCalculator.Calculate(changeCents);

            Assert.Equal(changeCents, change.Sum(item => item.DenominationCents * item.Quantity));
            Assert.All(change, item => Assert.Contains(item.DenominationCents, AllowedDenominations));
            Assert.All(change, item => Assert.True(item.Quantity > 0));
            Assert.Equal(
                change.Select(item => item.DenominationCents).OrderByDescending(cents => cents),
                change.Select(item => item.DenominationCents));
            Assert.Equal(
                change.Select(item => item.DenominationCents).Distinct().Count(),
                change.Count);
        }
    }
}
