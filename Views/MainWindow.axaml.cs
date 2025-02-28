using System.Net.Http;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CryptoTracker.Models;
using Newtonsoft.Json;

namespace CryptoTracker.Views;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient;
    private readonly string[] _cryptoIds = new[]
    {
        "internet-computer", "polkadot", "fetch", "solana", "near", "1inch", "tron",
        "uniswap", "the-sandbox", "polygon", "render-token", "ethfi", "pyth-network"
    };
    private readonly DispatcherTimer _refreshTimer;
    private readonly TimeZoneInfo _bulgarianTimeZone;

    public MainWindow()
    {
        InitializeComponent();
        
        // Get Bulgarian time zone
        try
        {
            _bulgarianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
        }
        catch
        {
            // Fallback to UTC+2 if the time zone is not found
            _bulgarianTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                "Bulgarian Time",
                TimeSpan.FromHours(2),
                "Bulgarian Time",
                "Bulgarian Time");
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.coingecko.com/api/v3/")
        };
        
        // Add API headers to avoid rate limiting
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoTracker");

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _refreshTimer.Tick += async (s, e) => await RefreshData();
        _refreshTimer.Start();

        this.Loaded += async (s, e) => await RefreshData();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async Task RefreshData()
    {
        try
        {
            string ids = string.Join(",", _cryptoIds);
            string endpoint = $"coins/markets?vs_currency=usd&ids={ids}&order=market_cap_desc&per_page=100&sparkline=false&price_change_percentage=24h";
            
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var cryptoList = JsonConvert.DeserializeObject<List<CryptoInfo>>(json);

            if (cryptoList != null)
            {
                var grid = this.FindControl<DataGrid>("CryptoGrid");
                var label = this.FindControl<TextBlock>("LastUpdateLabel");
                
                if (grid != null)
                {
                    grid.ItemsSource = cryptoList;
                    Console.WriteLine($"Found {cryptoList.Count} cryptocurrencies");
                    foreach (var crypto in cryptoList)
                    {
                        Console.WriteLine($"Name: {crypto.Name}, Price: {crypto.FormattedPrice}");
                    }
                }
                if (label != null)
                {
                    var bulgarianTime = TimeZoneInfo.ConvertTime(DateTime.Now, _bulgarianTimeZone);
                    label.Text = $"Last Updated: {bulgarianTime:g}";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data: {ex.Message}");
            var grid = this.FindControl<DataGrid>("CryptoGrid");
            if (grid != null)
                grid.ItemsSource = null;
        }
    }
} 