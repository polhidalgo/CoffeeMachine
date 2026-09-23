namespace CoffeeMachine.Core;

// Builds the receipt 
public sealed record PurchaseResult(
    Guid TransactionId,
    CoffeeType CoffeeType,
    int CoffeePriceCents,
    int InsertedCents,
    IReadOnlyList<ChangeItem> Change,
    DateTimeOffset CreatedAt)
{
    // Makes a ReadOnly copy of change, ToArray so the caller cannot alter
    public IReadOnlyList<ChangeItem> Change { get; init; } = Array.AsReadOnly(Change.ToArray());


    public int ChangeCents => InsertedCents - CoffeePriceCents;
}
