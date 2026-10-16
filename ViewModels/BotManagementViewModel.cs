using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoTraderApp.Models;

namespace CryptoTraderApp.ViewModels
{
    public partial class BotManagementViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<TradingBotStrategy> _botList = new();

        [ObservableProperty]
        private string _newBotName = "BTC 무한 분할 그리드";

        [ObservableProperty]
        private string _newBotSymbol = "BTC/USDT";

        [ObservableProperty]
        private string _newStrategyType = "Grid Trading";

        [ObservableProperty]
        private decimal _lowerPrice = 60000m;

        [ObservableProperty]
        private decimal _upperPrice = 75000m;

        [ObservableProperty]
        private int _gridCount = 30;

        [ObservableProperty]
        private decimal _investAmount = 2000m;

        public BotManagementViewModel()
        {
            InitializeBots();
        }

        private void InitializeBots()
        {
            BotList = new ObservableCollection<TradingBotStrategy>
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
                },
                new TradingBotStrategy
                {
                    Name = "XRP 마틴게일 DCA 봇",
                    Symbol = "XRP/USDT",
                    StrategyType = "DCA Bot",
                    IsRunning = true,
                    AllocatedCapital = 1500.00m,
                    TotalProfit = 118.40m,
                    ProfitRate = 7.89,
                    RuntimeHours = 96,
                    ExecutedTrades = 31,
                    LastSignal = "3단계 추가 매수 포지션 유지 중"
                }
            };
        }

        [RelayCommand]
        private void CreateNewBot()
        {
            var bot = new TradingBotStrategy
            {
                Name = NewBotName,
                Symbol = NewBotSymbol,
                StrategyType = NewStrategyType,
                AllocatedCapital = InvestAmount,
                IsRunning = true,
                TotalProfit = 0,
                ProfitRate = 0,
                RuntimeHours = 0,
                ExecutedTrades = 0,
                LastSignal = "전략 초기화 완료 및 첫 격자 배치"
            };
            BotList.Insert(0, bot);
        }

        [RelayCommand]
        private void ToggleBot(TradingBotStrategy? bot)
        {
            if (bot != null)
            {
                bot.IsRunning = !bot.IsRunning;
                int idx = BotList.IndexOf(bot);
                if (idx >= 0)
                {
                    BotList[idx] = bot;
                }
            }
        }

        [RelayCommand]
        private void DeleteBot(TradingBotStrategy? bot)
        {
            if (bot != null)
            {
                BotList.Remove(bot);
            }
        }
    }
}
