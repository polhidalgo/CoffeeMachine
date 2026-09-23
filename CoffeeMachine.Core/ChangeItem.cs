namespace CoffeeMachine.Core;

// Calculates the change, gets coin in cents and quantity of this coins
public sealed record ChangeItem(int DenominationCents, int Quantity);
