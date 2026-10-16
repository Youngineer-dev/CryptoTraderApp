using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CryptoTraderApp.Models;

namespace CryptoTraderApp.Services
{
    public class BithumbAccountBalance
    {
        public string Currency { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal Locked { get; set; }
        public decimal AvgBuyPrice { get; set; }
        public string UnitCurrency { get; set; } = "KRW";
        public decimal TotalAmount => Balance + Locked;
    }

    public class BithumbApiService
    {
        private static readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://api.bithumb.com"),
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static readonly string[] DefaultSymbols = 
        { 
            "BTC", "ETH", "SOL", "XRP", "DOGE", "ADA", "AVAX", "DOT", "LINK", "ETC" 
        };

        private string _apiKey = string.Empty;
        private string _secretKey = string.Empty;

        public bool HasValidCredentials => !string.IsNullOrWhiteSpace(_apiKey) && !string.IsNullOrWhiteSpace(_secretKey);

        public BithumbApiService()
        {
            LoadLocalCredentials();
        }

        public void SetCredentials(string apiKey, string secretKey)
        {
            _apiKey = apiKey?.Trim() ?? string.Empty;
            _secretKey = secretKey?.Trim() ?? string.Empty;

            try
            {
                var json = JsonSerializer.Serialize(new
                {
                    ApiKey = _apiKey,
                    SecretKey = _secretKey
                }, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText("bithumb_key.json", json);
            }
            catch
            {
                // Ignore local file write errors
            }
        }

        private void LoadLocalCredentials()
        {
            try
            {
                string[] possiblePaths = 
                {
                    "bithumb_key.json",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bithumb_key.json")
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("ApiKey", out var k)) _apiKey = k.GetString() ?? "";
                        if (doc.RootElement.TryGetProperty("SecretKey", out var s)) _secretKey = s.GetString() ?? "";
                        if (!string.IsNullOrWhiteSpace(_apiKey)) break;
                    }
                }
            }
            catch
            {
                // Ignore read errors
            }
        }

        /// <summary>
        /// 빗썸 V1 API 전체 보유 자산(계좌 잔고)을 조회합니다 (JWT 인증).
        /// </summary>
        public async Task<List<BithumbAccountBalance>> GetAccountBalancesAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<BithumbAccountBalance>();
            if (!HasValidCredentials)
                return list;

            try
            {
                string jwtToken = CreateJwtToken(_apiKey, _secretKey);
                using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/accounts");
                request.Headers.Add("Authorization", $"Bearer {jwtToken}");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return list;

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        string curr = item.TryGetProperty("currency", out var cProp) ? cProp.GetString() ?? "" : "";
                        decimal bal = ParseDecimal(item, "balance");
                        decimal locked = ParseDecimal(item, "locked");
                        decimal avgP = ParseDecimal(item, "avg_buy_price");
                        string unit = item.TryGetProperty("unit_currency", out var uProp) ? uProp.GetString() ?? "KRW" : "KRW";

                        list.Add(new BithumbAccountBalance
                        {
                            Currency = curr,
                            Balance = bal,
                            Locked = locked,
                            AvgBuyPrice = avgP,
                            UnitCurrency = unit
                        });
                    }
                }
            }
            catch
            {
                // return what was parsed
            }

            return list;
        }

        private static string CreateJwtToken(string accessKey, string secretKey)
        {
            string headerJson = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
            string header = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));

            string payloadJson = JsonSerializer.Serialize(new
            {
                access_key = accessKey,
                nonce = Guid.NewGuid().ToString(),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            string payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

            string dataToSign = $"{header}.{payload}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
            string signature = Base64UrlEncode(signatureBytes);

            return $"{dataToSign}.{signature}";
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        /// <summary>
        /// 빗썸 KRW 마켓 전체/주요 코인 실시간 시세 목록을 조회합니다.
        /// </summary>
        public async Task<List<MarketTicker>> GetKRWTickersAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<MarketTicker>();

            try
            {
                using var response = await _httpClient.GetAsync("/public/ticker/ALL_KRW", cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return GetFallbackTickers();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                var root = doc.RootElement;
                if (!root.TryGetProperty("status", out var status) || status.GetString() != "0000")
                    return GetFallbackTickers();

                if (!root.TryGetProperty("data", out var data))
                    return GetFallbackTickers();

                foreach (var symbol in DefaultSymbols)
                {
                    if (data.TryGetProperty(symbol, out var coinData))
                    {
                        decimal closingPrice = ParseDecimal(coinData, "closing_price");
                        double rate = ParseDouble(coinData, "fluctate_rate_24H");
                        decimal high24h = ParseDecimal(coinData, "max_price");
                        decimal low24h = ParseDecimal(coinData, "min_price");
                        decimal volume24h = ParseDecimal(coinData, "units_traded_24H");
                        decimal tradeValue = ParseDecimal(coinData, "acc_trade_value_24H");

                        result.Add(new MarketTicker
                        {
                            Symbol = $"{symbol}/KRW",
                            BaseCurrency = symbol,
                            QuoteCurrency = "KRW",
                            Exchange = "Bithumb",
                            LastPrice = closingPrice,
                            ChangePercentage = rate,
                            High24h = high24h,
                            Low24h = low24h,
                            Volume24h = volume24h,
                            TradeValue24h = tradeValue
                        });
                    }
                }
            }
            catch
            {
                return GetFallbackTickers();
            }

            return result.Count > 0 ? result : GetFallbackTickers();
        }

        /// <summary>
        /// 빗썸 캔들스틱 차트 데이터를 조회합니다.
        /// interval: 1m, 3m, 5m, 10m, 15m, 30m, 1h, 4h, 6h, 12h, 24h
        /// </summary>
        public async Task<List<CandleDataModel>> GetCandlestickAsync(
            string orderCurrency, 
            string interval = "15m", 
            int limit = 60,
            CancellationToken cancellationToken = default)
        {
            var candles = new List<CandleDataModel>();

            try
            {
                var cleanSymbol = orderCurrency.Replace("/KRW", "").Replace("/USDT", "").Trim();
                var endpoint = $"/public/candlestick/{cleanSymbol}_KRW/{interval}";

                using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return candles;

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                var root = doc.RootElement;
                if (!root.TryGetProperty("status", out var status) || status.GetString() != "0000")
                    return candles;

                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                    return candles;

                int totalCount = data.GetArrayLength();
                int startIndex = Math.Max(0, totalCount - limit);

                int index = 0;
                foreach (var item in data.EnumerateArray())
                {
                    if (index >= startIndex && item.GetArrayLength() >= 6)
                    {
                        long timestampMs = item[0].GetInt64();
                        double open = ParseDoubleValue(item[1]);
                        double close = ParseDoubleValue(item[2]);
                        double high = ParseDoubleValue(item[3]);
                        double low = ParseDoubleValue(item[4]);
                        double volume = ParseDoubleValue(item[5]);

                        var time = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).LocalDateTime;
                        candles.Add(new CandleDataModel
                        {
                            Timestamp = time,
                            Open = open,
                            Close = close,
                            High = high,
                            Low = low,
                            Volume = volume
                        });
                    }
                    index++;
                }
            }
            catch
            {
                // return what was gathered or empty
            }

            return candles;
        }

        /// <summary>
        /// 빗썸 실시간 호가(오더북) 데이터를 조회합니다.
        /// </summary>
        public async Task<(List<OrderBookEntry> Asks, List<OrderBookEntry> Bids)> GetOrderBookAsync(
            string orderCurrency, 
            int depth = 7, 
            CancellationToken cancellationToken = default)
        {
            var asks = new List<OrderBookEntry>();
            var bids = new List<OrderBookEntry>();

            try
            {
                var cleanSymbol = orderCurrency.Replace("/KRW", "").Replace("/USDT", "").Trim();
                var endpoint = $"/public/orderbook/{cleanSymbol}_KRW";

                using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return (asks, bids);

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                var root = doc.RootElement;
                if (!root.TryGetProperty("status", out var status) || status.GetString() != "0000")
                    return (asks, bids);

                if (!root.TryGetProperty("data", out var data))
                    return (asks, bids);

                // Asks (매도 호가)
                if (data.TryGetProperty("asks", out var asksArray))
                {
                    decimal maxAskQty = 0;
                    var tempAsks = new List<(decimal price, decimal qty)>();
                    foreach (var item in asksArray.EnumerateArray().Take(depth))
                    {
                        decimal p = ParseDecimal(item, "price");
                        decimal q = ParseDecimal(item, "quantity");
                        if (q > maxAskQty) maxAskQty = q;
                        tempAsks.Add((p, q));
                    }

                    foreach (var (p, q) in tempAsks.OrderByDescending(x => x.price))
                    {
                        double depthPct = maxAskQty > 0 ? (double)(q / maxAskQty * 100) : 20.0;
                        asks.Add(new OrderBookEntry
                        {
                            Price = p,
                            Amount = q,
                            Total = q,
                            DepthPercentage = Math.Max(5.0, Math.Min(100.0, depthPct)),
                            IsBuy = false
                        });
                    }
                }

                // Bids (매수 호가)
                if (data.TryGetProperty("bids", out var bidsArray))
                {
                    decimal maxBidQty = 0;
                    var tempBids = new List<(decimal price, decimal qty)>();
                    foreach (var item in bidsArray.EnumerateArray().Take(depth))
                    {
                        decimal p = ParseDecimal(item, "price");
                        decimal q = ParseDecimal(item, "quantity");
                        if (q > maxBidQty) maxBidQty = q;
                        tempBids.Add((p, q));
                    }

                    foreach (var (p, q) in tempBids.OrderByDescending(x => x.price))
                    {
                        double depthPct = maxBidQty > 0 ? (double)(q / maxBidQty * 100) : 20.0;
                        bids.Add(new OrderBookEntry
                        {
                            Price = p,
                            Amount = q,
                            Total = q,
                            DepthPercentage = Math.Max(5.0, Math.Min(100.0, depthPct)),
                            IsBuy = true
                        });
                    }
                }
            }
            catch
            {
                // graceful return
            }

            return (asks, bids);
        }

        /// <summary>
        /// 빗썸 최근 체결 내역을 조회합니다.
        /// </summary>
        public async Task<List<RecentTrade>> GetRecentTransactionsAsync(
            string orderCurrency, 
            int count = 12, 
            CancellationToken cancellationToken = default)
        {
            var trades = new List<RecentTrade>();

            try
            {
                var cleanSymbol = orderCurrency.Replace("/KRW", "").Replace("/USDT", "").Trim();
                var endpoint = $"/public/transaction_history/{cleanSymbol}_KRW";

                using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return trades;

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                var root = doc.RootElement;
                if (!root.TryGetProperty("status", out var status) || status.GetString() != "0000")
                    return trades;

                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                    return trades;

                foreach (var item in data.EnumerateArray().Take(count))
                {
                    decimal price = ParseDecimal(item, "price");
                    decimal qty = ParseDecimal(item, "units_traded");
                    string type = item.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? "bid" : "bid";
                    string dateStr = item.TryGetProperty("transaction_date", out var dateProp) ? dateProp.GetString() ?? "" : "";

                    DateTime ts = DateTime.TryParse(dateStr, out var parsedDt) ? parsedDt : DateTime.Now;

                    trades.Add(new RecentTrade
                    {
                        Price = price,
                        Amount = qty,
                        Timestamp = ts,
                        IsBuy = type.Equals("bid", StringComparison.OrdinalIgnoreCase)
                    });
                }
            }
            catch
            {
                // graceful return
            }

            return trades;
        }

        private static decimal ParseDecimal(JsonElement element, string propName)
        {
            if (element.TryGetProperty(propName, out var p))
            {
                if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                    return d;
                if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var nd))
                    return nd;
            }
            return 0m;
        }

        private static double ParseDouble(JsonElement element, string propName)
        {
            if (element.TryGetProperty(propName, out var p))
            {
                if (p.ValueKind == JsonValueKind.String && double.TryParse(p.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                    return d;
                if (p.ValueKind == JsonValueKind.Number && p.TryGetDouble(out var nd))
                    return nd;
            }
            return 0.0;
        }

        private static double ParseDoubleValue(JsonElement item)
        {
            if (item.ValueKind == JsonValueKind.String && double.TryParse(item.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            if (item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out var nd))
                return nd;
            return 0.0;
        }

        private static List<MarketTicker> GetFallbackTickers()
        {
            return new List<MarketTicker>
            {
                new MarketTicker { Symbol = "BTC/KRW", BaseCurrency = "BTC", QuoteCurrency = "KRW", Exchange = "Bithumb", LastPrice = 110400000m, ChangePercentage = 0.85, High24h = 111600000m, Low24h = 110100000m, Volume24h = 425.2m },
                new MarketTicker { Symbol = "ETH/KRW", BaseCurrency = "ETH", QuoteCurrency = "KRW", Exchange = "Bithumb", LastPrice = 3310000m, ChangePercentage = 3.40, High24h = 3350000m, Low24h = 3200000m, Volume24h = 2450.0m },
                new MarketTicker { Symbol = "SOL/KRW", BaseCurrency = "SOL", QuoteCurrency = "KRW", Exchange = "Bithumb", LastPrice = 143300m, ChangePercentage = 3.47, High24h = 1460000m, Low24h = 138000m, Volume24h = 18900.0m },
                new MarketTicker { Symbol = "XRP/KRW", BaseCurrency = "XRP", QuoteCurrency = "KRW", Exchange = "Bithumb", LastPrice = 1801m, ChangePercentage = 1.81, High24h = 1850m, Low24h = 1760m, Volume24h = 1520000m },
                new MarketTicker { Symbol = "DOGE/KRW", BaseCurrency = "DOGE", QuoteCurrency = "KRW", Exchange = "Bithumb", LastPrice = 111m, ChangePercentage = 1.83, High24h = 115m, Low24h = 108m, Volume24h = 8400000m }
            };
        }
    }
}
