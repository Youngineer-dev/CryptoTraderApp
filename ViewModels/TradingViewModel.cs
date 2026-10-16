using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoTraderApp.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CryptoTraderApp.ViewModels
{
    public partial class TradingViewModel : ObservableObject
    {
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

        // Manual Order Properties
        [ObservableProperty]
        private string _orderSide = "Buy"; // Buy or Sell

        [ObservableProperty]
        private string _orderType = "Limit"; // Limit, Market, Stop-Limit

        [ObservableProperty]
        private decimal _orderPrice;

        [ObservableProperty]
        private decimal _orderAmount;

        [ObservableProperty]
        private int _leverage = 20;

        [ObservableProperty]
        private bool _isTakeProfitStopLossEnabled = false;

        [ObservableProperty]
        private decimal _takeProfitPrice;

        [ObservableProperty]
        private decimal _stopLossPrice;

        [ObservableProperty]
        private decimal _availableBalance = 24580.50m; // USDT

        [ObservableProperty]
        private string _selectedInterval = "15m";

        public static double Create25 => 25.0;
        public static double Create50 => 50.0;
        public static double Create75 => 75.0;
        public static double Create100 => 100.0;

        // Chart series
        public ISeries[] CandleSeries { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        public TradingViewModel()
        {
            InitializeMarkets();
            InitializeOrderBook();
            InitializeRecentTrades();
            InitializePositionsAndOrders();
            InitializeBots();
            (CandleSeries, XAxes, YAxes) = CreateChartData();

            if (SelectedMarket != null)
            {
                OrderPrice = SelectedMarket.LastPrice;
                OrderAmount = 0.25m;
            }
        }

        private void InitializeMarkets()
        {
            MarketList = new ObservableCollection<MarketTicker>
            {
                new MarketTicker { Symbol = "BTC/USDT", BaseCurrency = "BTC", QuoteCurrency = "USDT", LastPrice = 67450.00m, ChangePercentage = 3.42, High24h = 68200.00m, Low24h = 65120.00m, Volume24h = 142850.5m },
                new MarketTicker { Symbol = "ETH/USDT", BaseCurrency = "ETH", QuoteCurrency = "USDT", LastPrice = 3520.80m, ChangePercentage = -1.15, High24h = 3610.00m, Low24h = 3480.20m, Volume24h = 89200.2m },
                new MarketTicker { Symbol = "SOL/USDT", BaseCurrency = "SOL", QuoteCurrency = "USDT", LastPrice = 178.65m, ChangePercentage = 7.82, High24h = 182.40m, Low24h = 164.50m, Volume24h = 563400.0m },
                new MarketTicker { Symbol = "XRP/USDT", BaseCurrency = "XRP", QuoteCurrency = "USDT", LastPrice = 0.5840m, ChangePercentage = 0.45, High24h = 0.5980m, Low24h = 0.5750m, Volume24h = 3450000.0m },
                new MarketTicker { Symbol = "DOGE/USDT", BaseCurrency = "DOGE", QuoteCurrency = "USDT", LastPrice = 0.1425m, ChangePercentage = -2.30, High24h = 0.1510m, Low24h = 0.1390m, Volume24h = 8720000.0m }
            };

            SelectedMarket = MarketList.FirstOrDefault();
        }

        partial void OnSelectedMarketChanged(MarketTicker? value)
        {
            if (value != null)
            {
                OrderPrice = value.LastPrice;
                InitializeOrderBook();
                InitializeRecentTrades();
                (CandleSeries, XAxes, YAxes) = CreateChartData();
                OnPropertyChanged(nameof(CandleSeries));
            }
        }

        private void InitializeOrderBook()
        {
            var basePrice = SelectedMarket?.LastPrice ?? 67450m;
            var rand = new Random(42);

            // Asks (Sells) - Above current price
            var asksList = new ObservableCollection<OrderBookEntry>();
            decimal askP = basePrice;
            for (int i = 0; i < 7; i++)
            {
                askP += Math.Round((decimal)(rand.NextDouble() * 15 + 5), 2);
                decimal amt = Math.Round((decimal)(rand.NextDouble() * 1.5 + 0.1), 4);
                asksList.Add(new OrderBookEntry
                {
                    Price = askP,
                    Amount = amt,
                    Total = amt,
                    DepthPercentage = Math.Min(100, (double)(amt * 50)),
                    IsBuy = false
                });
            }
            Asks = new ObservableCollection<OrderBookEntry>(asksList.OrderByDescending(x => x.Price));

            // Bids (Buys) - Below current price
            var bidsList = new ObservableCollection<OrderBookEntry>();
            decimal bidP = basePrice;
            for (int i = 0; i < 7; i++)
            {
                bidP -= Math.Round((decimal)(rand.NextDouble() * 15 + 5), 2);
                decimal amt = Math.Round((decimal)(rand.NextDouble() * 1.8 + 0.15), 4);
                bidsList.Add(new OrderBookEntry
                {
                    Price = bidP,
                    Amount = amt,
                    Total = amt,
                    DepthPercentage = Math.Min(100, (double)(amt * 45)),
                    IsBuy = true
                });
            }
            Bids = bidsList;
        }

        private void InitializeRecentTrades()
        {
            var basePrice = SelectedMarket?.LastPrice ?? 67450m;
            var rand = new Random();
            var list = new ObservableCollection<RecentTrade>();
            var now = DateTime.Now;

            for (int i = 0; i < 12; i++)
            {
                bool isBuy = rand.Next(2) == 0;
                decimal diff = (decimal)(rand.NextDouble() * 10 - 5);
                list.Add(new RecentTrade
                {
                    Price = basePrice + diff,
                    Amount = Math.Round((decimal)(rand.NextDouble() * 1.2 + 0.05), 4),
                    Timestamp = now.AddSeconds(-i * 4),
                    IsBuy = isBuy
                });
            }

            RecentTrades = list;
        }

        private void InitializePositionsAndOrders()
        {
            Positions = new ObservableCollection<PositionModel>
            {
                new PositionModel
                {
                    Symbol = "BTC/USDT",
                    Side = "Long",
                    Leverage = 20,
                    Size = 0.50m,
                    EntryPrice = 66200.00m,
                    MarkPrice = 67450.00m,
                    LiquidationPrice = 63100.00m,
                    Margin = 1655.00m,
                    PnL = 625.00m,
                    RoePercentage = 37.76
                },
                new PositionModel
                {
                    Symbol = "SOL/USDT",
                    Side = "Long",
                    Leverage = 10,
                    Size = 45.0m,
                    EntryPrice = 168.20m,
                    MarkPrice = 178.65m,
                    LiquidationPrice = 152.00m,
                    Margin = 756.90m,
                    PnL = 470.25m,
                    RoePercentage = 62.13
                }
            };

            OpenOrders = new ObservableCollection<OpenOrder>
            {
                new OpenOrder
                {
                    OrderId = "ORD-98214",
                    Symbol = "BTC/USDT",
                    Side = "Buy",
                    OrderType = "Limit",
                    Price = 65500.00m,
                    Amount = 0.30m,
                    FilledAmount = 0.0m,
                    Timestamp = DateTime.Now.AddMinutes(-34),
                    Status = "Open"
                },
                new OpenOrder
                {
                    OrderId = "ORD-98205",
                    Symbol = "ETH/USDT",
                    Side = "Buy",
                    OrderType = "Limit",
                    Price = 3450.00m,
                    Amount = 2.50m,
                    FilledAmount = 0.50m,
                    Timestamp = DateTime.Now.AddHours(-1),
                    Status = "Partially Filled"
                }
            };
        }

        private void InitializeBots()
        {
            ActiveBots = new ObservableCollection<TradingBotStrategy>
            {
                new TradingBotStrategy
                {
                    Name = "BTC 무한 그리드봇 #1",
                    Symbol = "BTC/USDT",
                    StrategyType = "Grid Trading",
                    IsRunning = true,
                    AllocatedCapital = 5000.00m,
                    TotalProfit = 428.50m,
                    ProfitRate = 8.57,
                    RuntimeHours = 142,
                    ExecutedTrades = 86,
                    LastSignal = "격자 매수 체결완료 (66,950 USDT)"
                },
                new TradingBotStrategy
                {
                    Name = "SOL 볼린저밴드 단타봇",
                    Symbol = "SOL/USDT",
                    StrategyType = "Bollinger Band",
                    IsRunning = true,
                    AllocatedCapital = 2500.00m,
                    TotalProfit = 312.80m,
                    ProfitRate = 12.51,
                    RuntimeHours = 68,
                    ExecutedTrades = 42,
                    LastSignal = "밴드 하단 터치 매수 대기"
                },
                new TradingBotStrategy
                {
                    Name = "ETH RSI 모멘텀 돌파봇",
                    Symbol = "ETH/USDT",
                    StrategyType = "RSI Momentum",
                    IsRunning = false,
                    AllocatedCapital = 3000.00m,
                    TotalProfit = 95.20m,
                    ProfitRate = 3.17,
                    RuntimeHours = 24,
                    ExecutedTrades = 12,
                    LastSignal = "RSI 과매도 대기 (일시정지됨)"
                }
            };
        }

        private (ISeries[], Axis[], Axis[]) CreateChartData()
        {
            var basePrice = (double)(SelectedMarket?.LastPrice ?? 67450m);
            var rand = new Random(101);
            var points = new ObservableCollection<FinancialPoint>();
            var now = DateTime.Now.AddMinutes(-30 * 15);

            double current = basePrice - 1200;
            for (int i = 0; i < 35; i++)
            {
                double open = current;
                double change = (rand.NextDouble() - 0.48) * (basePrice * 0.015);
                double close = open + change;
                double high = Math.Max(open, close) + rand.NextDouble() * (basePrice * 0.008);
                double low = Math.Min(open, close) - rand.NextDouble() * (basePrice * 0.008);
                current = close;

                points.Add(new FinancialPoint(now.AddMinutes(i * 15), high, open, close, low));
            }

            var series = new ISeries[]
            {
                new CandlesticksSeries<FinancialPoint>
                {
                    Name = SelectedMarket?.Symbol ?? "Candle",
                    Values = points,
                    UpFill = new SolidColorPaint(SKColor.Parse("#0ECB81")),
                    UpStroke = new SolidColorPaint(SKColor.Parse("#0ECB81"), 1.5f),
                    DownFill = new SolidColorPaint(SKColor.Parse("#F6465D")),
                    DownStroke = new SolidColorPaint(SKColor.Parse("#F6465D"), 1.5f)
                }
            };

            var xAxes = new Axis[]
            {
                new Axis
                {
                    LabelsRotation = 0,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#848E9C")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1F242C"), 1)
                }
            };

            var yAxes = new Axis[]
            {
                new Axis
                {
                    Position = LiveChartsCore.Measure.AxisPosition.End,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#848E9C")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1F242C"), 1)
                }
            };

            return (series, xAxes, yAxes);
        }

        [RelayCommand]
        private void SetAmountPercent(double percent)
        {
            if (OrderPrice <= 0) return;
            decimal totalCapital = AvailableBalance * (decimal)(percent / 100.0);
            OrderAmount = Math.Round((totalCapital * Leverage) / OrderPrice, 4);
        }

        [RelayCommand]
        private void ExecuteOrder()
        {
            if (OrderAmount <= 0 || OrderPrice <= 0 || SelectedMarket == null) return;

            if (OrderType == "Limit")
            {
                var newOrder = new OpenOrder
                {
                    OrderId = $"ORD-{new Random().Next(10000, 99999)}",
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
            else
            {
                // Market Order executes into position immediately
                var newPos = new PositionModel
                {
                    Symbol = SelectedMarket.Symbol,
                    Side = OrderSide == "Buy" ? "Long" : "Short",
                    Leverage = Leverage,
                    Size = OrderAmount,
                    EntryPrice = OrderPrice,
                    MarkPrice = OrderPrice,
                    LiquidationPrice = OrderSide == "Buy" ? OrderPrice * 0.92m : OrderPrice * 1.08m,
                    Margin = (OrderAmount * OrderPrice) / Leverage,
                    PnL = 0,
                    RoePercentage = 0
                };
                Positions.Insert(0, newPos);
            }
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
                    ActiveBots[index] = bot; // Trigger collection change
                }
            }
        }
    }
}
