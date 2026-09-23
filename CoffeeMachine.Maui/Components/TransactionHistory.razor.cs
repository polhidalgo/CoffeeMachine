using System.Globalization;
using CoffeeMachine.Maui.Services;
using Microsoft.AspNetCore.Components;

namespace CoffeeMachine.Maui.Components;

// Loads the recent remote rows 
public partial class TransactionHistory : ComponentBase
{
    private IReadOnlyList<TransactionRecord> _records = [];
    private PersistenceStatus? _status;
    private bool _isOpen;
    private bool _isLoading;

    [Inject]
    private SupabaseTransactionService Transactions { get; set; } = default!;

    // Changes/inverts _isopen
    private async Task ToggleAsync()
    {
        _isOpen = !_isOpen;

        // Load the first time it is opened
        if (_isOpen && _status is null)
        {
            await RefreshAsync();
        }
    }

    private async Task RefreshAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        StateHasChanged();

        try
        {
            var (status, records) = await Transactions.LoadRecentAsync();
            _status = status;

            // A failed read keeps the previously loaded rows 
            if (status == PersistenceStatus.Success)
            {
                _records = records;
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static string FormatAmount(int cents) => MoneyFormat.Amount(cents);

    private static string FormatTimestamp(DateTimeOffset createdAt) =>
        createdAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
}
