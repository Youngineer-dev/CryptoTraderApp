using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoTraderApp.Services;

namespace CryptoTraderApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly BithumbApiService _bithumbService = new();
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _pingTimer;

        [ObservableProperty]
        private object? _currentViewModel;

        [ObservableProperty]
        private string _selectedNavTag = "Trading";

        [ObservableProperty]
        private string _serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " KST";

        [ObservableProperty]
        private int _networkPing = 24; // ms

        [ObservableProperty]
        private string _connectedExchangeName = "Bithumb KRW (Live API 연동중)";

        public TradingViewModel TradingVM { get; } = new();
        public BotManagementViewModel BotVM { get; } = new();
        public PortfolioViewModel PortfolioVM { get; } = new();
        public ApiSettingsViewModel ApiSettingsVM { get; } = new();

        public MainViewModel()
        {
            CurrentViewModel = TradingVM;

            // 1초 단위 실시간 시계 타이머
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) =>
            {
                ServerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " KST";
            };
            _clockTimer.Start();

            // 10초 단위 실제 빗썸 서버 네트워크 핑 측정
            _pingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _pingTimer.Tick += async (s, e) =>
            {
                NetworkPing = await _bithumbService.MeasurePingAsync();
            };
            _pingTimer.Start();

            _ = InitialPingCheckAsync();
        }

        private async System.Threading.Tasks.Task InitialPingCheckAsync()
        {
            NetworkPing = await _bithumbService.MeasurePingAsync();
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
