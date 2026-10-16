using System;

namespace CryptoTraderApp.Models
{
    public class MarketTicker
    {
        public string Symbol { get; set; } = string.Empty;
        public string BaseCurrency { get; set; } = string.Empty;
        public string QuoteCurrency { get; set; } = "KRW";
        public string Exchange { get; set; } = "Bithumb";
        public decimal LastPrice { get; set; }
        public double ChangePercentage { get; set; }
        public decimal High24h { get; set; }
        public decimal Low24h { get; set; }
        public decimal Volume24h { get; set; }
        public decimal TradeValue24h { get; set; }
        public bool IsPositive => ChangePercentage >= 0;
        public string FormattedPrice => QuoteCurrency == "KRW"
            ? (LastPrice >= 100 ? $"{LastPrice:N0}" : $"{LastPrice:N2}")
            : $"{LastPrice:N2}";
        public string FormattedChange => $"{(ChangePercentage >= 0 ? "+" : "")}{ChangePercentage:F2}%";
    }

    public class CandleDataModel
    {
        public DateTime Timestamp { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Volume { get; set; }
    }

    public class OrderBookEntry
    {
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public decimal Total { get; set; }
        public double DepthPercentage { get; set; }
        public bool IsBuy { get; set; }
        public string FormattedPrice => Price >= 100 ? $"{Price:N0}" : $"{Price:N2}";
        public string FormattedAmount => $"{Amount:F4}";
        public string FormattedTotal => $"{Total:F4}";
    }

    public class RecentTrade
    {
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsBuy { get; set; }
        public string FormattedTime => Timestamp.ToString("HH:mm:ss");
        public string FormattedPrice => Price >= 100 ? $"{Price:N0}" : $"{Price:N2}";
        public string FormattedAmount => $"{Amount:F4}";
    }
}
