namespace CoffeeMachine.Maui.Services;


// Shape of one public.transactions row
public sealed record TransactionRecord(
    Guid TransactionId,
    string CoffeeType,
    int CoffeePriceCents,
    int InsertedCents,
    int ChangeCents,
    DateTimeOffset CreatedAt);
