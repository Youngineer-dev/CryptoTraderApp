using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CryptoTraderApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object? _currentViewModel;

        [ObservableProperty]
        private string _selectedNavTag = "Trading";

        [ObservableProperty]
        private string _serverTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";

        [ObservableProperty]
        private int _networkPing = 18; // ms

        [ObservableProperty]
        private string _connectedExchangeName = "Bithumb KRW (Live API 연동중)";

        public TradingViewModel TradingVM { get; } = new();
        public BotManagementViewModel BotVM { get; } = new();
        public PortfolioViewModel PortfolioVM { get; } = new();
        public ApiSettingsViewModel ApiSettingsVM { get; } = new();

        public MainViewModel()
        {
            CurrentViewModel = TradingVM;
        }

        [RelayCommand]
        private void Navigate(string tag)
        {
            SelectedNavTag = tag;
            CurrentViewModel = tag switch
            {
                "Trading" => TradingVM,
                "Bot" => BotVM,
                "Portfolio" => PortfolioVM,
                "Settings" => ApiSettingsVM,
                _ => TradingVM
            };
        }
    }
}
