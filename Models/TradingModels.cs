using System;

namespace CryptoTraderApp.Models
{
    public class PositionModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string Side { get; set; } = "Long"; // Long or Short
        public int Leverage { get; set; } = 10;
        public decimal Size { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal MarkPrice { get; set; }
        public decimal LiquidationPrice { get; set; }
        public decimal Margin { get; set; }
        public decimal PnL { get; set; }
        public double RoePercentage { get; set; }

        public bool IsProfit => PnL >= 0;
        public bool IsLong => Side.Equals("Long", StringComparison.OrdinalIgnoreCase);
        public string FormattedPnL => $"{(PnL >= 0 ? "+" : "")}{PnL:N2} USDT ({(RoePercentage >= 0 ? "+" : "")}{RoePercentage:F2}%)";
    }

    public class OpenOrder
    {
        public string OrderId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Side { get; set; } = "Buy";
        public string OrderType { get; set; } = "Limit";
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public decimal FilledAmount { get; set; }
        public double FilledPercent => Amount > 0 ? (double)(FilledAmount / Amount) * 100 : 0;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = "Open";

        public bool IsBuy => Side.Equals("Buy", StringComparison.OrdinalIgnoreCase);
        public string FormattedTime => Timestamp.ToString("MM/dd HH:mm:ss");
    }

    public class TradingBotStrategy
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = "BTC/USDT";
        public string StrategyType { get; set; } = "Grid Trading"; // Grid, DCA, RSI Scalping
        public bool IsRunning { get; set; } = true;
        public decimal AllocatedCapital { get; set; }
        public decimal TotalProfit { get; set; }
        public double ProfitRate { get; set; }
        public int RuntimeHours { get; set; }
        public int ExecutedTrades { get; set; }
        public string LastSignal { get; set; } = "Monitoring";

        public bool IsPositive => TotalProfit >= 0;
        public string StatusColor => IsRunning ? "#0ECB81" : "#848E9C";
        public string FormattedProfit => $"{(TotalProfit >= 0 ? "+" : "")}{TotalProfit:N2} USDT ({(ProfitRate >= 0 ? "+" : "")}{ProfitRate:F2}%)";
    }

    public class AssetHolding
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal Available { get; set; }
        public decimal InOrders { get; set; }
        public decimal ValueUsdt { get; set; }
        public decimal ValueKrw { get; set; }
        public double AllocationPercentage { get; set; }
        public string IconColor { get; set; } = "#F0B90B";
        public string FormattedBalance => Balance < 0.001m && Balance > 0 ? $"{Balance:F8}" : $"{Balance:F4}";
        public string FormattedValueKrw => $"{ValueKrw:N0} KRW";
    }

    public class ExchangeApiConfig
    {
        public string ExchangeName { get; set; } = "Bithumb (Spot)";
        public string ApiKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Passphrase { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public bool IsTestnet { get; set; } = false;
        public DateTime LastCheckedTime { get; set; }
        public string StatusText => IsConnected ? "연결됨 (실시간 백데이터 활성)" : (ExchangeName.Contains("Bithumb") ? "공개 API 활성" : "준비 중 (Coming Soon)");
        public bool IsAvailable => ExchangeName.Contains("Bithumb");
    }
}
