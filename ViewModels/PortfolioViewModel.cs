using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoTraderApp.Models;
using CryptoTraderApp.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CryptoTraderApp.ViewModels
{
    public partial class PortfolioViewModel : ObservableObject
    {
        private readonly BithumbApiService _bithumbService = new();

        [ObservableProperty]
        private decimal _totalBalanceKrw = 0m;

        [ObservableProperty]
        private decimal _totalProfitKrw = 0m;

        [ObservableProperty]
        private double _dailyReturnPercent = 2.45;

        [ObservableProperty]
        private string _accountStatusMessage = "빗썸 실제 계좌 잔고 연동 중...";

        [ObservableProperty]
        private ObservableCollection<AssetHolding> _assets = new();

        public ISeries[] AssetPieSeries { get; set; } = Array.Empty<ISeries>();

        public PortfolioViewModel()
        {
            _ = LoadRealAssetsAsync();
        }

        [RelayCommand]
        public async Task RefreshAssetsAsync()
        {
            await LoadRealAssetsAsync();
        }

        public async Task LoadRealAssetsAsync()
        {
            try
            {
                var realBalances = await _bithumbService.GetAccountBalancesAsync();
                if (realBalances.Count == 0)
                {
                    AccountStatusMessage = "빗썸 API 키가 설정되지 않았거나 자산이 없습니다.";
                    return;
                }

                // Filter non-zero assets
                var activeBalances = realBalances.Where(b => b.TotalAmount > 0).ToList();
                if (activeBalances.Count == 0)
                {
                    AccountStatusMessage = "빗썸 계좌에 보유 중인 활성 자산이 없습니다.";
                    return;
                }

                var tickers = await _bithumbService.GetKRWTickersAsync();
                var tickerMap = tickers.ToDictionary(t => t.BaseCurrency, t => t.LastPrice, StringComparer.OrdinalIgnoreCase);

                var colors = new[] 
                { 
                    "#F7931A", "#627EEA", "#14F195", "#3888FF", "#0ECB81", 
                    "#F0B90B", "#E02424", "#9061F9", "#0694A2", "#E02462", 
                    "#FF6900", "#7E3AF2" 
                };

                var holdingList = new List<AssetHolding>();
                decimal grandTotalKrw = 0m;

                int colorIdx = 0;
                foreach (var b in activeBalances)
                {
                    decimal price = 0m;
                    if (b.Currency.Equals("KRW", StringComparison.OrdinalIgnoreCase))
                    {
                        price = 1m;
                    }
                    else if (b.Currency.Equals("P", StringComparison.OrdinalIgnoreCase))
                    {
                        price = 1m; // 1 포인트 = 1 KRW
                    }
                    else if (tickerMap.TryGetValue(b.Currency, out var liveP))
                    {
                        price = liveP;
                    }
                    else if (b.AvgBuyPrice > 0)
                    {
                        price = b.AvgBuyPrice;
                    }

                    decimal val = Math.Round(b.TotalAmount * price, 2);
                    grandTotalKrw += val;

                    holdingList.Add(new AssetHolding
                    {
                        Symbol = b.Currency,
                        Name = GetCurrencyName(b.Currency),
                        Balance = b.TotalAmount,
                        Available = b.Balance,
                        InOrders = b.Locked,
                        ValueKrw = val,
                        IconColor = colors[colorIdx % colors.Length]
                    });
                    colorIdx++;
                }

                // Sort by ValueKrw descending
                holdingList = holdingList.OrderByDescending(x => x.ValueKrw).ToList();

                // Compute allocation percentages
                foreach (var item in holdingList)
                {
                    item.AllocationPercentage = grandTotalKrw > 0 
                        ? (double)Math.Round((item.ValueKrw / grandTotalKrw) * 100, 2) 
                        : 0.0;
                }

                // Update UI on Dispatcher
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    TotalBalanceKrw = grandTotalKrw;
                    AccountStatusMessage = $"[정상 연동] 빗썸 실시간 계좌 연동 완료 (총 {holdingList.Count}개 보유 코인 확인)";

                    Assets.Clear();
                    foreach (var h in holdingList)
                    {
                        Assets.Add(h);
                    }

                    // Build Pie Chart Series (Top 6 assets + Others)
                    var pieList = new List<ISeries>();
                    var topAssets = holdingList.Take(5).ToList();
                    var otherAssets = holdingList.Skip(5).ToList();

                    foreach (var top in topAssets)
                    {
                        pieList.Add(new PieSeries<double>
                        {
                            Values = new double[] { top.AllocationPercentage },
                            Name = $"{top.Symbol} ({top.AllocationPercentage:F1}%)",
                            Fill = new SolidColorPaint(SKColor.Parse(top.IconColor)),
                            Pushout = 4
                        });
                    }

                    if (otherAssets.Count > 0)
                    {
                        double otherPct = Math.Round(otherAssets.Sum(x => x.AllocationPercentage), 1);
                        if (otherPct > 0)
                        {
                            pieList.Add(new PieSeries<double>
                            {
                                Values = new double[] { otherPct },
                                Name = $"기타({otherAssets.Count}종) ({otherPct:F1}%)",
                                Fill = new SolidColorPaint(SKColor.Parse("#848E9C")),
                                Pushout = 2
                            });
                        }
                    }

                    AssetPieSeries = pieList.ToArray();
                    OnPropertyChanged(nameof(AssetPieSeries));
                });
            }
            catch (Exception ex)
            {
                AccountStatusMessage = $"자산 조회 중 오류: {ex.Message}";
            }
        }

        private static string GetCurrencyName(string symbol)
        {
            return symbol.ToUpperInvariant() switch
            {
                "KRW" => "원화 (KRW)",
                "P" => "빗썸 포인트",
                "ETH" => "이더리움",
                "KAIA" => "카이아 (클레이튼)",
                "WLD" => "월드코인",
                "ICX" => "아이콘",
                "GAS" => "가스",
                "BZNT" => "베잔트",
                "XEC" => "이캐시",
                "DASH" => "대시",
                "ETC" => "이더리움 클래식",
                "XRP" => "리플",
                "BCH" => "비트코인 캐시",
                "QTUM" => "퀀텀",
                "BTG" => "비트코인 골드",
                "BSV" => "비트코인SV",
                "ETHW" => "이더리움W",
                _ => symbol
            };
        }
    }
}
