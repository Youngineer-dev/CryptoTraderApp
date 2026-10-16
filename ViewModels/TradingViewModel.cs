using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoTraderApp.Models;
using CryptoTraderApp.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CryptoTraderApp.ViewModels
{
    public partial class TradingViewModel : ObservableObject
    {
        private readonly BithumbApiService _bithumbService = new();
        private readonly DispatcherTimer _refreshTimer;
        private List<CandleDataModel> _cachedCandles = new();

        [ObservableProperty]
        private string _exchangeName = "Bithumb (빗썸 KRW 마켓)";

        [ObservableProperty]
        private ObservableCollection<MarketTicker> _marketList = new();

        [ObservableProperty]
        private MarketTicker? _selectedMarket;

        [ObservableProperty]
        private ObservableCollection<OrderBookEntry> _asks = new();

        [ObservableProperty]
        private ObservableCollection<OrderBookEntry> _bids = new();

        [ObservableProperty]
        private ObservableCollection<RecentTrade> _recentTrades = new();

        [ObservableProperty]
        private ObservableCollection<PositionModel> _positions = new();

        [ObservableProperty]
        private ObservableCollection<OpenOrder> _openOrders = new();

        [ObservableProperty]
        private ObservableCollection<TradingBotStrategy> _activeBots = new();

        // Technical Indicator & Chart Controls
        [ObservableProperty]
        private bool _showSMA = true;

        [ObservableProperty]
        private bool _showBollinger = false;

        [ObservableProperty]
        private string _selectedInterval = "15m";

        [ObservableProperty]
        private string _rsiStatus = "RSI: 로딩 중...";

        [ObservableProperty]
        private string _macdStatus = "MACD: 로딩 중...";

        [ObservableProperty]
        private string _bollingerStatus = "BB: 로딩 중...";

        [ObservableProperty]
        private bool _isLoadingData = false;

        // Manual Order Properties
        [ObservableProperty]
        private string _orderSide = "Buy";

        [ObservableProperty]
        private string _orderType = "Limit";

        [ObservableProperty]
        private decimal _orderPrice;

        [ObservableProperty]
        private decimal _orderAmount;

        [ObservableProperty]
        private int _leverage = 1; // 빗썸 현물 기준 1x

        [ObservableProperty]
        private bool _isTakeProfitStopLossEnabled = false;

        [ObservableProperty]
        private decimal _takeProfitPrice;

        [ObservableProperty]
        private decimal _stopLossPrice;

        [ObservableProperty]
        private decimal _availableBalance = 35000000m; // 35,000,000 KRW

        public static double Create25 => 25.0;
        public static double Create50 => 50.0;
        public static double Create75 => 75.0;
        public static double Create100 => 100.0;

        // Chart series
        public ISeries[] CandleSeries { get; set; } = Array.Empty<ISeries>();
        public Axis[] XAxes { get; set; } = Array.Empty<Axis>();
        public Axis[] YAxes { get; set; } = Array.Empty<Axis>();

        public TradingViewModel()
        {
            InitializeDefaultMarkets();
            InitializeDefaultPositionsAndOrders();
            InitializeBots();
            SetupAxes();

            // Setup real-time background refresh timer (every 4 seconds)
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            _refreshTimer.Tick += async (s, e) => await OnTimerTickAsync();

            // Load initial real live data from Bithumb API
            _ = LoadInitialBithumbDataAsync();
        }

        private void SetupAxes()
        {
            XAxes = new Axis[]
            {
                new Axis
                {
                    LabelsRotation = 0,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#848E9C")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1F242C"), 1),
                    Labeler = value =>
                    {
                        try
                        {
                            return new DateTime((long)value).ToString("MM/dd HH:mm");
                        }
                        catch
                        {
                            return string.Empty;
                        }
                    }
                }
            };

            YAxes = new Axis[]
            {
                new Axis
                {
                    Position = LiveChartsCore.Measure.AxisPosition.End,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#848E9C")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1F242C"), 1),
                    Labeler = value => value >= 1000 ? $"{value:N0}" : $"{value:N2}"
                }
            };
        }

        private void InitializeDefaultMarkets()
        {
            MarketList = new ObservableCollection<MarketTicker>
            {
                new MarketTicker { Symbol = "BTC/KRW", BaseCurrency = "BTC", QuoteCurrency = "KRW", Exchange = "Bithumb", LastPrice = 110400000m, ChangePercentage = 0.85, High24h = 111600000m, Low24h = 110100000m, Volume24h = 425.2m },
                new MarketTicker { Symbol = "ETH/KRW", BaseCurrency = "ETH", QuoteCurrency = "KRW", Exchange = "Bithumb", LastPrice = 3310000m, ChangePercentage = 3.40, High24h = 3350000m, Low24h = 3200000m, Volume24h = 2450.0m },
                new MarketTicker { Symbol = "SOL/KRW", BaseCurrency = "SOL", QuoteCurrency = "KRW", Exchange = "Bithumb", LastPrice = 143300m, ChangePercentage = 3.47, High24h = 146000m, Low24h = 138000m, Volume24h = 18900.0m },
                new MarketTicker { Symbol = "XRP/KRW", BaseCurrency = "XRP", QuoteCurrency = "KRW", Exchange = "Bithumb", LastPrice = 1801m, ChangePercentage = 1.81, High24h = 1850m, Low24h = 1760m, Volume24h = 1520000m },
                new MarketTicker { Symbol = "DOGE/KRW", BaseCurrency = "DOGE", QuoteCurrency = "KRW", Exchange = "Bithumb", LastPrice = 111m, ChangePercentage = 1.83, High24h = 115m, Low24h = 108m, Volume24h = 8400000m }
            };

            SelectedMarket = MarketList.FirstOrDefault();
            if (SelectedMarket != null)
            {
                OrderPrice = SelectedMarket.LastPrice;
                OrderAmount = 0.05m;
            }
        }

        private async Task LoadInitialBithumbDataAsync()
        {
            IsLoadingData = true;
            try
            {
                // 1. Load real tickers
                var liveTickers = await _bithumbService.GetKRWTickersAsync();
                if (liveTickers.Count > 0)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        var prevSelected = SelectedMarket?.BaseCurrency;
                        MarketList.Clear();
                        foreach (var t in liveTickers)
                        {
                            MarketList.Add(t);
                        }
                        SelectedMarket = MarketList.FirstOrDefault(x => x.BaseCurrency == prevSelected) ?? MarketList.FirstOrDefault();
                    });
                }

                // 2. Load chart, orderbook, trades for selected market
                await RefreshSelectedMarketDataAsync();

                _refreshTimer.Start();
            }
            finally
            {
                IsLoadingData = false;
            }
        }

        private async Task RefreshSelectedMarketDataAsync()
        {
            if (SelectedMarket == null) return;
            string symbol = SelectedMarket.BaseCurrency;

            // Load Candlesticks
            var candles = await _bithumbService.GetCandlestickAsync(symbol, SelectedInterval, 80);
            if (candles.Count > 0)
            {
                _cachedCandles = candles;
                UpdateChartAndIndicators(_cachedCandles);
            }

            // Load Orderbook
            var (asks, bids) = await _bithumbService.GetOrderBookAsync(symbol, 8);
            if (asks.Count > 0 || bids.Count > 0)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Asks = new ObservableCollection<OrderBookEntry>(asks);
                    Bids = new ObservableCollection<OrderBookEntry>(bids);
                });
            }

            // Load Recent Trades
            var trades = await _bithumbService.GetRecentTransactionsAsync(symbol, 14);
            if (trades.Count > 0)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    RecentTrades = new ObservableCollection<RecentTrade>(trades);
                });
            }
        }

        private async Task OnTimerTickAsync()
        {
            if (SelectedMarket == null) return;

            try
            {
                // Refresh orderbook & trades in background
                var (asks, bids) = await _bithumbService.GetOrderBookAsync(SelectedMarket.BaseCurrency, 8);
                if (asks.Count > 0) Asks = new ObservableCollection<OrderBookEntry>(asks);
                if (bids.Count > 0) Bids = new ObservableCollection<OrderBookEntry>(bids);

                var trades = await _bithumbService.GetRecentTransactionsAsync(SelectedMarket.BaseCurrency, 14);
                if (trades.Count > 0) RecentTrades = new ObservableCollection<RecentTrade>(trades);
            }
            catch
            {
                // Suppress timer background errors
            }
        }

        partial void OnSelectedMarketChanged(MarketTicker? value)
        {
            if (value != null)
            {
                OrderPrice = value.LastPrice;
                _ = RefreshSelectedMarketDataAsync();
            }
        }

        [RelayCommand]
        public async Task ChangeInterval(string interval)
        {
            SelectedInterval = interval;
            IsLoadingData = true;
            try
            {
                if (SelectedMarket != null)
                {
                    var candles = await _bithumbService.GetCandlestickAsync(SelectedMarket.BaseCurrency, interval, 80);
                    if (candles.Count > 0)
                    {
                        _cachedCandles = candles;
                        UpdateChartAndIndicators(_cachedCandles);
                    }
                }
            }
            finally
            {
                IsLoadingData = false;
            }
        }

        [RelayCommand]
        public void ToggleSMA()
        {
            ShowSMA = !ShowSMA;
            if (_cachedCandles.Count > 0)
            {
                UpdateChartAndIndicators(_cachedCandles);
            }
        }

        [RelayCommand]
        public void ToggleBollinger()
        {
            ShowBollinger = !ShowBollinger;
            if (_cachedCandles.Count > 0)
            {
                UpdateChartAndIndicators(_cachedCandles);
            }
        }

        [RelayCommand]
        public async Task RefreshAllData()
        {
            IsLoadingData = true;
            try
            {
                await RefreshSelectedMarketDataAsync();
            }
            finally
            {
                IsLoadingData = false;
            }
        }

        private void UpdateChartAndIndicators(List<CandleDataModel> candles)
        {
            if (candles == null || candles.Count == 0) return;

            var closePrices = candles.Select(c => c.Close).ToList();

            // 1. Calculate Technical Indicators
            var sma20 = TechnicalAnalysisService.CalculateSMA(closePrices, 20);
            var sma60 = TechnicalAnalysisService.CalculateSMA(closePrices, 60);
            var (bbUpper, bbMiddle, bbLower) = TechnicalAnalysisService.CalculateBollingerBands(closePrices, 20, 2.0);
            var rsiList = TechnicalAnalysisService.CalculateRSI(closePrices, 14);
            var (macdLine, signalLine, histogram) = TechnicalAnalysisService.CalculateMACD(closePrices, 12, 26, 9);

            // Update Status Strings for Badges
            var lastRsi = rsiList.LastOrDefault(r => r.HasValue);
            if (lastRsi.HasValue)
            {
                string state = lastRsi.Value >= 70 ? "과매수(매도유의)" : (lastRsi.Value <= 30 ? "과매도(반등기대)" : "중립");
                RsiStatus = $"RSI(14): {lastRsi.Value:F1} [{state}]";
            }

            var lastMacd = macdLine.LastOrDefault(m => m.HasValue);
            var lastSignal = signalLine.LastOrDefault(s => s.HasValue);
            if (lastMacd.HasValue && lastSignal.HasValue)
            {
                string signal = lastMacd.Value > lastSignal.Value ? "골든크로스(상승)" : "데드크로스(조정)";
                MacdStatus = $"MACD: {lastMacd.Value:F1} [{signal}]";
            }

            var lastUpper = bbUpper.LastOrDefault(u => u.HasValue);
            var lastLower = bbLower.LastOrDefault(l => l.HasValue);
            var lastClose = closePrices.LastOrDefault();
            if (lastUpper.HasValue && lastLower.HasValue)
            {
                string bbState = lastClose > lastUpper.Value ? "상단 돌파" : (lastClose < lastLower.Value ? "하단 지지" : "밴드 내 순항");
                BollingerStatus = $"BB(20,2): [{bbState}]";
            }

            // 2. Prepare LiveCharts Points
            var candlePoints = new ObservableCollection<FinancialPoint>();
            var sma20Points = new ObservableCollection<DateTimePoint>();
            var sma60Points = new ObservableCollection<DateTimePoint>();
            var bbUpperPoints = new ObservableCollection<DateTimePoint>();
            var bbLowerPoints = new ObservableCollection<DateTimePoint>();

            for (int i = 0; i < candles.Count; i++)
            {
                var c = candles[i];
                candlePoints.Add(new FinancialPoint(c.Timestamp, c.High, c.Open, c.Close, c.Low));

                if (sma20[i].HasValue)
                    sma20Points.Add(new DateTimePoint(c.Timestamp, sma20[i]!.Value));

                if (sma60[i].HasValue)
                    sma60Points.Add(new DateTimePoint(c.Timestamp, sma60[i]!.Value));

                if (bbUpper[i].HasValue)
                    bbUpperPoints.Add(new DateTimePoint(c.Timestamp, bbUpper[i]!.Value));

                if (bbLower[i].HasValue)
                    bbLowerPoints.Add(new DateTimePoint(c.Timestamp, bbLower[i]!.Value));
            }

            var seriesList = new List<ISeries>
            {
                new CandlesticksSeries<FinancialPoint>
                {
                    Name = SelectedMarket?.Symbol ?? "Candlestick",
                    Values = candlePoints,
                    UpFill = new SolidColorPaint(SKColor.Parse("#0ECB81")),
                    UpStroke = new SolidColorPaint(SKColor.Parse("#0ECB81"), 1.6f),
                    DownFill = new SolidColorPaint(SKColor.Parse("#F6465D")),
                    DownStroke = new SolidColorPaint(SKColor.Parse("#F6465D"), 1.6f)
                }
            };

            // Overlay SMA if enabled
            if (ShowSMA)
            {
                seriesList.Add(new LineSeries<DateTimePoint>
                {
                    Name = "SMA 20 (단기)",
                    Values = sma20Points,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColor.Parse("#F0B90B"), 1.8f),
                    GeometrySize = 0,
                    LineSmoothness = 0.4
                });

                seriesList.Add(new LineSeries<DateTimePoint>
                {
                    Name = "SMA 60 (중기)",
                    Values = sma60Points,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColor.Parse("#3888FF"), 1.8f),
                    GeometrySize = 0,
                    LineSmoothness = 0.4
                });
            }

            // Overlay Bollinger Bands if enabled
            if (ShowBollinger)
            {
                seriesList.Add(new LineSeries<DateTimePoint>
                {
                    Name = "BB 상단 (2.0σ)",
                    Values = bbUpperPoints,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColor.Parse("#A020F0"), 1.4f),
                    GeometrySize = 0,
                    LineSmoothness = 0.3
                });

                seriesList.Add(new LineSeries<DateTimePoint>
                {
                    Name = "BB 하단 (2.0σ)",
                    Values = bbLowerPoints,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColor.Parse("#A020F0"), 1.4f),
                    GeometrySize = 0,
                    LineSmoothness = 0.3
                });
            }

            CandleSeries = seriesList.ToArray();
            OnPropertyChanged(nameof(CandleSeries));
        }

        private void InitializeDefaultPositionsAndOrders()
        {
            Positions = new ObservableCollection<PositionModel>
            {
                new PositionModel
                {
                    Symbol = "BTC/KRW",
                    Side = "Long",
                    Leverage = 1,
                    Size = 0.15m,
                    EntryPrice = 108500000m,
                    MarkPrice = 110400000m,
                    LiquidationPrice = 0m,
                    Margin = 16275000m,
                    PnL = 285000m,
                    RoePercentage = 1.75
                },
                new PositionModel
                {
                    Symbol = "ETH/KRW",
                    Side = "Long",
                    Leverage = 1,
                    Size = 2.5m,
                    EntryPrice = 3200000m,
                    MarkPrice = 3310000m,
                    LiquidationPrice = 0m,
                    Margin = 8000000m,
                    PnL = 275000m,
                    RoePercentage = 3.44
                }
            };

            OpenOrders = new ObservableCollection<OpenOrder>
            {
                new OpenOrder
                {
                    OrderId = "BTH-81920",
                    Symbol = "BTC/KRW",
                    Side = "Buy",
                    OrderType = "Limit",
                    Price = 109500000m,
                    Amount = 0.05m,
                    FilledAmount = 0.0m,
                    Timestamp = DateTime.Now.AddMinutes(-20),
                    Status = "Open"
                }
            };
        }

        private void InitializeBots()
        {
            ActiveBots = new ObservableCollection<TradingBotStrategy>
            {
                new TradingBotStrategy
                {
                    Name = "빗썸 BTC 그리드 분할매매 #1",
                    Symbol = "BTC/KRW",
                    StrategyType = "Grid Trading",
                    IsRunning = true,
                    AllocatedCapital = 10000000m,
                    TotalProfit = 345000m,
                    ProfitRate = 3.45,
                    RuntimeHours = 96,
                    ExecutedTrades = 48,
                    LastSignal = "빗썸 호가 110,100,000 KRW 매수 대기"
                },
                new TradingBotStrategy
                {
                    Name = "빗썸 ETH 볼린저 하단반등봇",
                    Symbol = "ETH/KRW",
                    StrategyType = "Bollinger Band",
                    IsRunning = true,
                    AllocatedCapital = 5000000m,
                    TotalProfit = 185000m,
                    ProfitRate = 3.70,
                    RuntimeHours = 44,
                    ExecutedTrades = 22,
                    LastSignal = "BB(20) 하단 터치 감시 중"
                }
            };
        }

        [RelayCommand]
        private void SetAmountPercent(double percent)
        {
            if (OrderPrice <= 0) return;
            decimal totalCapital = AvailableBalance * (decimal)(percent / 100.0);
            OrderAmount = Math.Round(totalCapital / OrderPrice, 4);
        }

        [RelayCommand]
        private void ExecuteOrder()
        {
            if (OrderAmount <= 0 || OrderPrice <= 0 || SelectedMarket == null) return;

            var newOrder = new OpenOrder
            {
                OrderId = $"BTH-{new Random().Next(10000, 99999)}",
                Symbol = SelectedMarket.Symbol,
                Side = OrderSide,
                OrderType = OrderType,
                Price = OrderPrice,
                Amount = OrderAmount,
                FilledAmount = 0,
                Timestamp = DateTime.Now,
                Status = "Open"
            };
            OpenOrders.Insert(0, newOrder);
        }

        [RelayCommand]
        private void CancelOrder(OpenOrder? order)
        {
            if (order != null)
            {
                OpenOrders.Remove(order);
            }
        }

        [RelayCommand]
        private void ClosePosition(PositionModel? position)
        {
            if (position != null)
            {
                Positions.Remove(position);
            }
        }

        [RelayCommand]
        private void ToggleBot(TradingBotStrategy? bot)
        {
            if (bot != null)
            {
                bot.IsRunning = !bot.IsRunning;
                int index = ActiveBots.IndexOf(bot);
                if (index >= 0)
                {
                    ActiveBots[index] = bot;
                }
            }
        }
    }
}
