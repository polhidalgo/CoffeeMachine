namespace CoffeeMachine.Maui.Services;

// Enum of outcome status
public enum PersistenceStatus
{
    Success,

    // No usable configuration; the request was never attempted
    Disabled,

    // Network failure, timeout or a transient HTTP status
    Unavailable,

    // Permissions, payload, endpoint or deserialization problem
    Error
}
