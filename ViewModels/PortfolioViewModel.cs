using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoTraderApp.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CryptoTraderApp.ViewModels
{
    public partial class PortfolioViewModel : ObservableObject
    {
        [ObservableProperty]
        private decimal _totalBalanceUsdt = 48250.80m;

        [ObservableProperty]
        private decimal _totalProfitUsdt = 3420.50m;

        [ObservableProperty]
        private double _dailyReturnPercent = 4.25;

        [ObservableProperty]
        private ObservableCollection<AssetHolding> _assets = new();

        public ISeries[] AssetPieSeries { get; set; }

        public PortfolioViewModel()
        {
            InitializeAssets();
            AssetPieSeries = CreatePieChart();
        }

        private void InitializeAssets()
        {
            Assets = new ObservableCollection<AssetHolding>
            {
                new AssetHolding { Symbol = "BTC", Name = "Bitcoin", Balance = 0.45m, Available = 0.35m, InOrders = 0.10m, ValueUsdt = 30352.50m, AllocationPercentage = 62.9, IconColor = "#F7931A" },
                new AssetHolding { Symbol = "ETH", Name = "Ethereum", Balance = 2.80m, Available = 2.00m, InOrders = 0.80m, ValueUsdt = 9858.24m, AllocationPercentage = 20.4, IconColor = "#627EEA" },
                new AssetHolding { Symbol = "SOL", Name = "Solana", Balance = 25.0m, Available = 20.0m, InOrders = 5.0m, ValueUsdt = 4466.25m, AllocationPercentage = 9.3, IconColor = "#14F195" },
                new AssetHolding { Symbol = "USDT", Name = "Tether USD", Balance = 3573.81m, Available = 3573.81m, InOrders = 0m, ValueUsdt = 3573.81m, AllocationPercentage = 7.4, IconColor = "#26A17B" }
            };
        }

        private ISeries[] CreatePieChart()
        {
            return new ISeries[]
            {
                new PieSeries<double>
                {
                    Values = new double[] { 62.9 },
                    Name = "Bitcoin (BTC)",
                    Fill = new SolidColorPaint(SKColor.Parse("#F7931A")),
                    Pushout = 4
                },
                new PieSeries<double>
                {
                    Values = new double[] { 20.4 },
                    Name = "Ethereum (ETH)",
                    Fill = new SolidColorPaint(SKColor.Parse("#627EEA")),
                    Pushout = 4
                },
                new PieSeries<double>
                {
                    Values = new double[] { 9.3 },
                    Name = "Solana (SOL)",
                    Fill = new SolidColorPaint(SKColor.Parse("#14F195")),
                    Pushout = 4
                },
                new PieSeries<double>
                {
                    Values = new double[] { 7.4 },
                    Name = "Tether (USDT)",
                    Fill = new SolidColorPaint(SKColor.Parse("#26A17B")),
                    Pushout = 4
                }
            };
        }
    }
}
