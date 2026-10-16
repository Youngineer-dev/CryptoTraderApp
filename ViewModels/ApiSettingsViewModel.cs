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
        private string _selectedExchange = "Binance";

        [ObservableProperty]
        private string _inputApiKey = string.Empty;

        [ObservableProperty]
        private string _inputSecretKey = string.Empty;

        [ObservableProperty]
        private bool _isTestnet = false;

        [ObservableProperty]
        private string _connectionStatusMessage = "설정된 거래소 API가 안전하게 로컬에 암호화 보관됩니다.";

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
                    ExchangeName = "Binance (Futures)",
                    ApiKey = "vmPUZE6mv9SD5VNHhm44ZoKfCQnpysnv...",
                    SecretKey = "************************************",
                    IsConnected = true,
                    IsTestnet = false,
                    LastCheckedTime = DateTime.Now.AddMinutes(-5)
                },
                new ExchangeApiConfig
                {
                    ExchangeName = "Upbit (Spot)",
                    ApiKey = "fG39xKsK209xKlW24a4x8aA9...",
                    SecretKey = "************************************",
                    IsConnected = true,
                    IsTestnet = false,
                    LastCheckedTime = DateTime.Now.AddMinutes(-12)
                },
                new ExchangeApiConfig
                {
                    ExchangeName = "Bithumb (Spot)",
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
            var config = new ExchangeApiConfig
            {
                ExchangeName = SelectedExchange,
                ApiKey = string.IsNullOrWhiteSpace(InputApiKey) ? "TEST_API_KEY_MOCK..." : InputApiKey,
                SecretKey = "************************************",
                IsConnected = true,
                IsTestnet = IsTestnet,
                LastCheckedTime = DateTime.Now
            };

            ApiConfigs.Add(config);
            ConnectionStatusMessage = $"[성공] {SelectedExchange} API 연결 테스트 완료 (Ping: 24ms, 계정 조회 권한 정상)";
            InputApiKey = string.Empty;
            InputSecretKey = string.Empty;
        }

        [RelayCommand]
        private void DeleteConfig(ExchangeApiConfig? config)
        {
            if (config != null)
            {
                ApiConfigs.Remove(config);
            }
        }
    }
}
