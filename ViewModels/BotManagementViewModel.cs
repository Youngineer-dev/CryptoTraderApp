using System.Collections.ObjectModel;
using System.Linq;
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
        private string _newBotName = "빗썸 BTC 자동 그리드 봇 #1";

        [ObservableProperty]
        private string _newBotSymbol = "BTC/KRW";

        [ObservableProperty]
        private string _newStrategyType = "Grid Trading";

        [ObservableProperty]
        private decimal _lowerPrice = 105000000m;

        [ObservableProperty]
        private decimal _upperPrice = 115000000m;

        [ObservableProperty]
        private int _gridCount = 20;

        [ObservableProperty]
        private decimal _investAmount = 1000000m; // 1,000,000 KRW

        public int ActiveBotsCount => BotList.Count(b => b.IsRunning);
        public int TotalBotsCount => BotList.Count;
        public decimal TotalBotProfitKrw => BotList.Sum(b => b.TotalProfit);
        public int TotalTradesCount => BotList.Sum(b => b.ExecutedTrades);
        public decimal TotalBotCapitalKrw => BotList.Sum(b => b.AllocatedCapital);

        public BotManagementViewModel()
        {
            // 실제 사용자가 직접 배포/설정한 봇만 관리하도록 초기화 (더미 예시 데이터 배제)
            BotList = new ObservableCollection<TradingBotStrategy>();
            NotifyStats();
        }

        private void NotifyStats()
        {
            OnPropertyChanged(nameof(ActiveBotsCount));
            OnPropertyChanged(nameof(TotalBotsCount));
            OnPropertyChanged(nameof(TotalBotProfitKrw));
            OnPropertyChanged(nameof(TotalTradesCount));
            OnPropertyChanged(nameof(TotalBotCapitalKrw));
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
                LastSignal = $"빗썸 {NewBotSymbol} 호가 감시 및 첫 매수 주문 대기"
            };
            BotList.Insert(0, bot);
            NotifyStats();
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
                NotifyStats();
            }
        }

        [RelayCommand]
        private void DeleteBot(TradingBotStrategy? bot)
        {
            if (bot != null)
            {
                BotList.Remove(bot);
                NotifyStats();
            }
        }
    }
}
