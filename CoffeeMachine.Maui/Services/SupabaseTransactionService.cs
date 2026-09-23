using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoffeeMachine.Core;
using CoffeeMachine.Maui.Configuration;
using Microsoft.Extensions.Logging;

namespace CoffeeMachine.Maui.Services;
public sealed class SupabaseTransactionService(
    HttpClient httpClient,
    SupabaseOptions options,
    ILogger<SupabaseTransactionService> logger)
{
    private const string TablePath = "rest/v1/transactions";
    private const int HistoryLimit = 20;

    private const string SelectedColumns =
        "transaction_id,coffee_type,coffee_price_cents,inserted_cents,change_cents,created_at";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    //rue when the app was given a usable URL and publishable key
    public bool IsEnabled => options.IsConfigured;

    // Inserts one receipt. Called only after a successful purchase
    public async Task<PersistenceStatus> SaveAsync(PurchaseResult purchase)
    {
        ArgumentNullException.ThrowIfNull(purchase);

        if (!IsEnabled)
        {
            return PersistenceStatus.Disabled;
        }

        var record = new TransactionRecord(
            purchase.TransactionId,
            ToColumnValue(purchase.CoffeeType),
            purchase.CoffeePriceCents,
            purchase.InsertedCents,
            purchase.ChangeCents,
            purchase.CreatedAt.ToUniversalTime());

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(TablePath));
            ApplyHeaders(request);
            request.Headers.Add("Prefer", "return=minimal");
            request.Content = JsonContent.Create(record, options: JsonOptions);

            using var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return PersistenceStatus.Success;
            }

            logger.LogError(
                "Saving transaction {TransactionId} failed with status {StatusCode}.",
                purchase.TransactionId,
                (int)response.StatusCode);

            return StatusFor(response.StatusCode);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Saving transaction {TransactionId} timed out.", purchase.TransactionId);
            return PersistenceStatus.Unavailable;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Saving transaction {TransactionId} could not reach Supabase.", purchase.TransactionId);
            return PersistenceStatus.Unavailable;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected failure saving transaction {TransactionId}.", purchase.TransactionId);
            return PersistenceStatus.Error;
        }
    }

//Get the last 20 transactions
    public async Task<(PersistenceStatus Status, IReadOnlyList<TransactionRecord> Records)> LoadRecentAsync()
    {
        if (!IsEnabled)
        {
            return (PersistenceStatus.Disabled, []);
        }

        var query =
            $"{TablePath}?select={SelectedColumns}" +
            $"&order=created_at.desc,transaction_id.desc&limit={HistoryLimit}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(query));
            ApplyHeaders(request);

            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Loading transactions failed with status {StatusCode}.",
                    (int)response.StatusCode);

                return (StatusFor(response.StatusCode), []);
            }

            var records = await response.Content.ReadFromJsonAsync<List<TransactionRecord>>(JsonOptions);
            return (PersistenceStatus.Success, records ?? []);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Loading transactions timed out.");
            return (PersistenceStatus.Unavailable, []);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Loading transactions could not reach Supabase.");
            return (PersistenceStatus.Unavailable, []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected failure loading transactions.");
            return (PersistenceStatus.Error, []);
        }
    }

    // Column value expected by the coffee_type check constraint, avoid to insert unknown enums in database
    private static string ToColumnValue(CoffeeType coffeeType) => coffeeType switch
    {
        CoffeeType.Cappuccino => "cappuccino",
        CoffeeType.Latte => "latte",
        CoffeeType.Decaf => "decaf",
        _ => throw new ArgumentOutOfRangeException(
            nameof(coffeeType), coffeeType, "Unknown coffee type.")
    };

    private Uri BuildUri(string relativePath) =>
        new(new Uri(options.Url.TrimEnd('/') + "/"), relativePath);

    private void ApplyHeaders(HttpRequestMessage request)
    {
        // Applied per request
        request.Headers.Add("apikey", options.PublishableKey);
    }

    //Returns status code
    private static PersistenceStatus StatusFor(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            || (int)statusCode >= 500
            ? PersistenceStatus.Unavailable
            : PersistenceStatus.Error;
}
