using Newtonsoft.Json;
using Avalonia.Media;

namespace CryptoTracker.Models;

public class CryptoInfo
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("current_price")]
    public decimal CurrentPrice { get; set; }

    [JsonProperty("price_change_percentage_24h")]
    public decimal PriceChangePercentage24h { get; set; }

    [JsonProperty("market_cap")]
    public decimal MarketCap { get; set; }

    [JsonProperty("total_volume")]
    public decimal TotalVolume { get; set; }

    [JsonProperty("last_updated")]
    public DateTime LastUpdated { get; set; }

    // Formatted properties for display
    public string FormattedPrice => $"${CurrentPrice:N2}";
    public string FormattedPriceChange => $"{PriceChangePercentage24h:+0.00;-0.00}%";
    public string FormattedMarketCap => $"${MarketCap:N0}";
    public string FormattedLastUpdated => TimeZoneInfo.ConvertTimeFromUtc(
        LastUpdated.ToUniversalTime(),
        TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time")).ToString("g");

    // Color property for price change
    public IBrush PriceChangeColor => PriceChangePercentage24h >= 0 
        ? new SolidColorBrush(Color.Parse("#32CD32"))  // Light green for positive
        : new SolidColorBrush(Color.Parse("#FF4500")); // Red-orange for negative
} 