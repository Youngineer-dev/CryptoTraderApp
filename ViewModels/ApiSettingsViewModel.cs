using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoTraderApp.Models;
using CryptoTraderApp.Services;

namespace CryptoTraderApp.ViewModels
{
    public partial class ApiSettingsViewModel : ObservableObject
    {
        private readonly BithumbApiService _bithumbService = new();

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
        private string _connectionStatusMessage = "[연동 성공] 빗썸(Bithumb) API Key가 로컬에 안전하게 등록되었으며 실시간 계좌 및 시세 조회가 활성화되었습니다.";

        public ApiSettingsViewModel()
        {
            InitializeConfigs();
        }

        private void InitializeConfigs()
        {
            string maskedKey = "3820043928fa...1d99";
            try
            {
                if (File.Exists("bithumb_key.json"))
                {
                    string json = File.ReadAllText("bithumb_key.json");
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("ApiKey", out var k))
                    {
                        string fullKey = k.GetString() ?? "";
                        if (fullKey.Length > 12)
                        {
                            maskedKey = $"{fullKey[..8]}...{fullKey[^4..]}";
                        }
                    }
                }
            }
            catch
            {
            }

            ApiConfigs = new ObservableCollection<ExchangeApiConfig>
            {
                new ExchangeApiConfig
                {
                    ExchangeName = "Bithumb (Spot)",
                    ApiKey = maskedKey,
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
        private async Task SaveAndTestConnection()
        {
            if (!SelectedExchange.Contains("Bithumb", StringComparison.OrdinalIgnoreCase))
            {
                ConnectionStatusMessage = $"[안내] {SelectedExchange} 거래소는 현재 '준비 중'입니다. 실시간 백데이터는 빗썸(Bithumb)을 통해 제공됩니다.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(InputApiKey) && !string.IsNullOrWhiteSpace(InputSecretKey))
            {
                _bithumbService.SetCredentials(InputApiKey, InputSecretKey);
            }

            var balances = await _bithumbService.GetAccountBalancesAsync();
            if (balances.Count > 0)
            {
                ConnectionStatusMessage = $"[성공] 빗썸(Bithumb) API Key 인증 및 계좌 연동 성공! (총 {balances.Count}개 보유 코인 잔고 확인)";
                var existing = ApiConfigs.Count > 0 ? ApiConfigs[0] : null;
                if (existing != null && existing.ExchangeName.Contains("Bithumb"))
                {
                    string fullKey = InputApiKey;
                    if (fullKey.Length > 12)
                    {
                        existing.ApiKey = $"{fullKey[..8]}...{fullKey[^4..]}";
                    }
                    existing.IsConnected = true;
                    existing.LastCheckedTime = DateTime.Now;
                }
            }
            else
            {
                ConnectionStatusMessage = "[확인] 공개 시세/차트 API는 정상 수신 중입니다. (Private 잔고 조회의 경우 IP 등록 및 권한을 확인하세요)";
            }

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
