using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Media;
using CryptoTracker.Models;
using Newtonsoft.Json;
using Avalonia.Media.TextFormatting;

namespace CryptoTracker.Views;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient;
    private List<string> _cryptoIds;
    private readonly DispatcherTimer _refreshTimer;
    private readonly TimeZoneInfo _bulgarianTimeZone;
    private readonly string _settingsPath = "cryptolist.json";

    public MainWindow()
    {
        InitializeComponent();
        
        // Load saved cryptocurrency list or use default
        _cryptoIds = LoadCryptoList();

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

    private List<string> LoadCryptoList()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var savedList = JsonConvert.DeserializeObject<List<string>>(json);
                if (savedList != null && savedList.Count > 0)
                {
                    return savedList;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading crypto list: {ex.Message}");
        }

        // Return default list if file doesn't exist or is empty
        return new List<string>
        {
            "internet-computer", "polkadot", "fetch", "solana", "near", "1inch", "tron",
            "uniswap", "the-sandbox", "polygon", "render-token", "ethfi", "pyth-network"
        };
    }

    private void SaveCryptoList()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_cryptoIds, Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving crypto list: {ex.Message}");
        }
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

    private async void OnAddButtonClick(object sender, RoutedEventArgs e)
    {
        var dialog = new TextBox
        {
            Watermark = "Enter cryptocurrency ID (e.g., bitcoin, ethereum)",
            Width = 300,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var dialogWindow = new Window
        {
            Title = "Add Cryptocurrency",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var okButton = new Button { Content = "OK", Width = 100, Margin = new Thickness(10, 0) };
        var cancelButton = new Button { Content = "Cancel", Width = 100, Margin = new Thickness(10, 0) };
        var result = new TaskCompletionSource<string>();

        okButton.Click += (_, _) =>
        {
            result.SetResult(dialog.Text ?? string.Empty);
            dialogWindow.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            result.SetResult(string.Empty);
            dialogWindow.Close();
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0),
            Children = { cancelButton, okButton }
        };

        var mainPanel = new StackPanel
        {
            Margin = new Thickness(20),
            Children =
            {
                new TextBlock 
                { 
                    Text = "Enter the cryptocurrency ID from CoinGecko:", 
                    Margin = new Thickness(0, 0, 0, 10),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                dialog,
                buttonPanel
            }
        };

        dialogWindow.Content = mainPanel;

        await dialogWindow.ShowDialog(this);
        var dialogResult = await result.Task;

        if (!string.IsNullOrWhiteSpace(dialogResult))
        {
            var cryptoId = dialogResult.Trim().ToLower();
            if (!_cryptoIds.Contains(cryptoId))
            {
                try
                {
                    // Verify if the cryptocurrency exists
                    var response = await _httpClient.GetAsync($"coins/{cryptoId}");
                    if (response.IsSuccessStatusCode)
                    {
                        _cryptoIds.Add(cryptoId);
                        SaveCryptoList();
                        await RefreshData();
                    }
                    else
                    {
                        var errorWindow = new Window
                        {
                            Title = "Error",
                            Content = new TextBlock { Text = "Cryptocurrency not found!", Margin = new Thickness(20) },
                            Width = 250,
                            Height = 100,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        };
                        await errorWindow.ShowDialog(this);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding cryptocurrency: {ex.Message}");
                }
            }
        }
    }

    private async void OnRemoveButtonClick(object sender, RoutedEventArgs e)
    {
        var grid = this.FindControl<DataGrid>("CryptoGrid");
        if (grid?.SelectedItem is CryptoInfo selectedCrypto)
        {
            var confirmWindow = new Window
            {
                Title = "Confirm Remove",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var message = new TextBlock
            {
                Text = $"Are you sure you want to remove {selectedCrypto.Name}?",
                Margin = new Thickness(20, 20, 20, 0),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var yesButton = new Button { Content = "Yes", Width = 100, Margin = new Thickness(10, 0) };
            var noButton = new Button { Content = "No", Width = 100, Margin = new Thickness(10, 0) };
            var result = new TaskCompletionSource<bool>();

            yesButton.Click += (_, _) =>
            {
                result.SetResult(true);
                confirmWindow.Close();
            };

            noButton.Click += (_, _) =>
            {
                result.SetResult(false);
                confirmWindow.Close();
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0),
                Children = { noButton, yesButton }
            };

            var mainPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    message,
                    buttonPanel
                }
            };

            confirmWindow.Content = mainPanel;

            await confirmWindow.ShowDialog(this);
            if (await result.Task)
            {
                _cryptoIds.Remove(selectedCrypto.Id);
                SaveCryptoList();
                await RefreshData();
            }
        }
        else
        {
            var selectWindow = new Window
            {
                Title = "Select Cryptocurrency",
                Content = new TextBlock 
                { 
                    Text = "Please select a cryptocurrency from the list to remove.", 
                    Margin = new Thickness(20),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                Width = 300,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            await selectWindow.ShowDialog(this);
        }
    }
} 