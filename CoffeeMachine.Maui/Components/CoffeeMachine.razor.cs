using CoffeeMachine.Core;
using CoffeeMachine.Maui.Services;
using Microsoft.AspNetCore.Components;

namespace CoffeeMachine.Maui.Components;

//Code behind for CoffeMachine.razor
public partial class CoffeeMachine : ComponentBase
{
    // List of coins to select
    private static readonly int[] CoinButtons = [1, 2, 5, 10, 20, 50, 100, 200];

    private static readonly CoffeeType[] CoffeeTypes = Enum.GetValues<CoffeeType>();

    private string _message = "Insert coins to order.";

    // Which receipt the persistence state belongs to
    private Guid? _persistenceTransactionId;
    private PersistenceStatus? _persistenceStatus;

    [Inject]
    private CoffeeMachineService Machine { get; set; } = default!;

    [Inject]
    private SupabaseTransactionService Transactions { get; set; } = default!;

    private void InsertCoin(int cents)
    {
        var error = Machine.TryInsertCoin(cents);

        _message = error switch
        {
            MachineError.None => $"{FormatCoin(cents)} coin accepted.",
            MachineError.InvalidCoin => $"{FormatCoin(cents)} coin not accepted.",
            _ => "Error trying to insert coins."
        };
    }

    private async Task BuyAsync(CoffeeType coffeeType)
    {
        var error = Machine.TryPurchase(coffeeType, out var purchase);

        if (error != MachineError.None)
        {
            ShowMachineError(error, coffeeType);
            return;
        }

        var completedPurchase = purchase!;
        _persistenceTransactionId = completedPurchase.TransactionId;
        _persistenceStatus = null; 
        _message = $"{completedPurchase.CoffeeType}";

        // Marks that he balance is already zero and the receipt is already available.
        StateHasChanged();

        var status = await Transactions.SaveAsync(completedPurchase); // wait to save transaction

        // An older response must not overwrite the state of another purchase.
        if (_persistenceTransactionId == completedPurchase.TransactionId)
        {
            _persistenceStatus = status; // save success if post to database was okay
        }
    }

    private void ShowMachineError(MachineError error, CoffeeType coffeeType)
    {
        if (error == MachineError.InsufficientFunds)
        {
            var missingCents = CoffeeMachineService.GetPriceCents(coffeeType) - Machine.BalanceCents;
            _message = $"Insufficient credit. Insert {FormatAmount(missingCents)} more.";
            return;
        }

        _message = "Error while ordering.";
    }

    private string PersistenceMessage(PurchaseResult purchase)
    {
        if (_persistenceTransactionId != purchase.TransactionId)
        {
            return string.Empty;
        }

        return _persistenceStatus switch
        {
            null => "Saving history…",
            PersistenceStatus.Success => "History saved.",
            PersistenceStatus.Disabled => "History disabled.",
            PersistenceStatus.Unavailable => "History save could not be confirmed.",
            _ => "History could not be saved."
        };
    }

    // Match enum and name from database
    private static string CoffeeImageName(CoffeeType coffeeType) => coffeeType switch
    {
        CoffeeType.Cappuccino => "cappuccino",
        CoffeeType.Latte => "latte",
        CoffeeType.Decaf => "decaf",
        _ => throw new ArgumentOutOfRangeException(nameof(coffeeType))
    };

    private static string FormatAmount(int cents) => MoneyFormat.Amount(cents);

    private static string FormatCoin(int cents) => MoneyFormat.Coin(cents);
}
