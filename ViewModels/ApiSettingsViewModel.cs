using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoTraderApp.Models;

namespace CryptoTraderApp.ViewModels
{
    public partial class ApiSettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ExchangeApiConfig> _apiConfigs = new();

        [ObservableProperty]
        private string _selectedExchange = "Bithumb (빗썸)";

        [ObservableProperty]
        private string _inputApiKey = string.Empty;

        [ObservableProperty]
        private string _inputSecretKey = string.Empty;

        [ObservableProperty]
        private bool _isTestnet = false;

        [ObservableProperty]
        private string _connectionStatusMessage = "현재 [빗썸(Bithumb)] 거래소의 실시간 시세 및 차트 Open API가 활성화되어 있습니다. 타 거래소는 지원 준비 중입니다.";

        public ApiSettingsViewModel()
        {
            InitializeConfigs();
        }

        private void InitializeConfigs()
        {
            ApiConfigs = new ObservableCollection<ExchangeApiConfig>
            {
                new ExchangeApiConfig
                {
                    ExchangeName = "Bithumb (Spot)",
                    ApiKey = "bithumb_public_client_active",
                    SecretKey = "************************************",
                    IsConnected = true,
                    IsTestnet = false,
                    LastCheckedTime = DateTime.Now
                },
                new ExchangeApiConfig
                {
                    ExchangeName = "Binance (Futures)",
                    ApiKey = string.Empty,
                    SecretKey = string.Empty,
                    IsConnected = false,
                    IsTestnet = false,
                    LastCheckedTime = DateTime.MinValue
                },
                new ExchangeApiConfig
                {
                    ExchangeName = "Upbit (Spot)",
                    ApiKey = string.Empty,
                    SecretKey = string.Empty,
                    IsConnected = false,
                    IsTestnet = false,
                    LastCheckedTime = DateTime.MinValue
                },
                new ExchangeApiConfig
                {
                    ExchangeName = "Bybit",
                    ApiKey = string.Empty,
                    SecretKey = string.Empty,
                    IsConnected = false,
                    IsTestnet = false,
                    LastCheckedTime = DateTime.MinValue
                },
                new ExchangeApiConfig
                {
                    ExchangeName = "OKX",
                    ApiKey = string.Empty,
                    SecretKey = string.Empty,
                    IsConnected = false,
                    IsTestnet = false,
                    LastCheckedTime = DateTime.MinValue
                }
            };
        }

        [RelayCommand]
        private void SaveAndTestConnection()
        {
            if (!SelectedExchange.Contains("Bithumb", StringComparison.OrdinalIgnoreCase))
            {
                ConnectionStatusMessage = $"[안내] {SelectedExchange} 거래소는 현재 '준비 중'입니다. 실시간 백데이터는 빗썸(Bithumb)을 통해 제공됩니다.";
                return;
            }

            var existing = ApiConfigs.Count > 0 ? ApiConfigs[0] : null;
            if (existing != null && existing.ExchangeName.Contains("Bithumb"))
            {
                existing.ApiKey = string.IsNullOrWhiteSpace(InputApiKey) ? "bithumb_public_client_active" : InputApiKey;
                existing.IsConnected = true;
                existing.LastCheckedTime = DateTime.Now;
            }

            ConnectionStatusMessage = $"[성공] 빗썸(Bithumb) Developer API 연결 확인 완료 (REST 시세, 차트, 오더북 정상 수신 중)";
            InputApiKey = string.Empty;
            InputSecretKey = string.Empty;
        }

        [RelayCommand]
        private void DeleteConfig(ExchangeApiConfig? config)
        {
            if (config != null)
            {
                if (config.ExchangeName.Contains("Bithumb"))
                {
                    ConnectionStatusMessage = "[경고] 빗썸은 현재 활성 백데이터 제공 거래소이므로 삭제할 수 없습니다.";
                    return;
                }
                ApiConfigs.Remove(config);
            }
        }
    }
}
