using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTraderApp.Models;

namespace CryptoTraderApp.Services
{
    /// <summary>
    /// 트레이딩뷰(TradingView)에서 사용하는 표준 기술 지표 연산 엔진
    /// - 이동평균선(SMA, EMA)
    /// - 볼린저 밴드(Bollinger Bands)
    /// - RSI (상대강도지수)
    /// - MACD (이동평균 수렴확산)
    /// </summary>
    public static class TechnicalAnalysisService
    {
        /// <summary>
        /// 단순이동평균(SMA) 계산
        /// </summary>
        public static List<double?> CalculateSMA(IReadOnlyList<double> closePrices, int period)
        {
            var result = new List<double?>(closePrices.Count);
            if (closePrices == null || closePrices.Count == 0 || period <= 0)
                return result;

            double sum = 0;
            for (int i = 0; i < closePrices.Count; i++)
            {
                sum += closePrices[i];
                if (i >= period)
                {
                    sum -= closePrices[i - period];
                }

                if (i >= period - 1)
                {
                    result.Add(Math.Round(sum / period, 4));
                }
                else
                {
                    result.Add(null); // 데이터 부족 구간
                }
            }

            return result;
        }

        /// <summary>
        /// 지수이동평균(EMA) 계산
        /// </summary>
        public static List<double?> CalculateEMA(IReadOnlyList<double> closePrices, int period)
        {
            var result = new List<double?>(closePrices.Count);
            if (closePrices == null || closePrices.Count == 0 || period <= 0)
                return result;

            double multiplier = 2.0 / (period + 1.0);
            double? prevEma = null;

            for (int i = 0; i < closePrices.Count; i++)
            {
                if (i < period - 1)
                {
                    result.Add(null);
                }
                else if (i == period - 1)
                {
                    // 첫 EMA는 앞선 period개의 SMA로 초기화
                    double sma = closePrices.Take(period).Average();
                    prevEma = sma;
                    result.Add(Math.Round(sma, 4));
                }
                else
                {
                    double currentEma = (closePrices[i] - prevEma!.Value) * multiplier + prevEma.Value;
                    prevEma = currentEma;
                    result.Add(Math.Round(currentEma, 4));
                }
            }

            return result;
        }

        /// <summary>
        /// 볼린저 밴드(Bollinger Bands: 상단선, 중심선(SMA), 하단선) 계산
        /// 기본 파라미터: period=20, multiplier=2.0
        /// </summary>
        public static (List<double?> Upper, List<double?> Middle, List<double?> Lower) CalculateBollingerBands(
            IReadOnlyList<double> closePrices, 
            int period = 20, 
            double multiplier = 2.0)
        {
            var upper = new List<double?>(closePrices.Count);
            var middle = CalculateSMA(closePrices, period);
            var lower = new List<double?>(closePrices.Count);

            for (int i = 0; i < closePrices.Count; i++)
            {
                if (i < period - 1 || middle[i] == null)
                {
                    upper.Add(null);
                    lower.Add(null);
                    continue;
                }

                double sma = middle[i]!.Value;
                double sumSquareDiff = 0;
                for (int j = i - period + 1; j <= i; j++)
                {
                    double diff = closePrices[j] - sma;
                    sumSquareDiff += diff * diff;
                }

                double stdDev = Math.Sqrt(sumSquareDiff / period);
                upper.Add(Math.Round(sma + (multiplier * stdDev), 4));
                lower.Add(Math.Round(sma - (multiplier * stdDev), 4));
            }

            return (upper, middle, lower);
        }

        /// <summary>
        /// RSI(Relative Strength Index: 상대강도지수, 0~100) 계산
        /// 기본 기간: 14
        /// </summary>
        public static List<double?> CalculateRSI(IReadOnlyList<double> closePrices, int period = 14)
        {
            var rsiList = new List<double?>(closePrices.Count);
            if (closePrices == null || closePrices.Count <= period)
            {
                if (closePrices != null)
                {
                    for (int i = 0; i < closePrices.Count; i++) rsiList.Add(null);
                }
                return rsiList;
            }

            rsiList.Add(null); // index 0 diff 없음

            double avgGain = 0;
            double avgLoss = 0;

            // 초기 14기간 평균 gain, loss
            for (int i = 1; i <= period; i++)
            {
                double change = closePrices[i] - closePrices[i - 1];
                if (change > 0) avgGain += change;
                else avgLoss += Math.Abs(change);
                rsiList.Add(null);
            }

            avgGain /= period;
            avgLoss /= period;

            double rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            double firstRsi = 100.0 - (100.0 / (1.0 + rs));
            rsiList[period] = Math.Round(firstRsi, 2);

            // Wilder's Smoothing
            for (int i = period + 1; i < closePrices.Count; i++)
            {
                double change = closePrices[i] - closePrices[i - 1];
                double gain = change > 0 ? change : 0;
                double loss = change < 0 ? Math.Abs(change) : 0;

                avgGain = ((avgGain * (period - 1)) + gain) / period;
                avgLoss = ((avgLoss * (period - 1)) + loss) / period;

                if (avgLoss == 0)
                {
                    rsiList.Add(100.0);
                }
                else
                {
                    rs = avgGain / avgLoss;
                    double rsi = 100.0 - (100.0 / (1.0 + rs));
                    rsiList.Add(Math.Round(rsi, 2));
                }
            }

            return rsiList;
        }

        /// <summary>
        /// MACD (Moving Average Convergence Divergence) 계산
        /// Fast: 12, Slow: 26, Signal: 9
        /// </summary>
        public static (List<double?> MacdLine, List<double?> SignalLine, List<double?> Histogram) CalculateMACD(
            IReadOnlyList<double> closePrices, 
            int fastPeriod = 12, 
            int slowPeriod = 26, 
            int signalPeriod = 9)
        {
            var macdLine = new List<double?>(closePrices.Count);
            var signalLine = new List<double?>(closePrices.Count);
            var histogram = new List<double?>(closePrices.Count);

            var fastEma = CalculateEMA(closePrices, fastPeriod);
            var slowEma = CalculateEMA(closePrices, slowPeriod);

            var validMacdValues = new List<double>();
            var macdIndexMap = new List<int>();

            for (int i = 0; i < closePrices.Count; i++)
            {
                if (fastEma[i].HasValue && slowEma[i].HasValue)
                {
                    double diff = fastEma[i]!.Value - slowEma[i]!.Value;
                    macdLine.Add(Math.Round(diff, 4));
                    validMacdValues.Add(diff);
                    macdIndexMap.Add(i);
                }
                else
                {
                    macdLine.Add(null);
                }
            }

            var signalEmaValues = CalculateEMA(validMacdValues, signalPeriod);

            for (int i = 0; i < closePrices.Count; i++)
            {
                signalLine.Add(null);
                histogram.Add(null);
            }

            for (int k = 0; k < validMacdValues.Count; k++)
            {
                int originalIndex = macdIndexMap[k];
                if (signalEmaValues[k].HasValue)
                {
                    double s = signalEmaValues[k]!.Value;
                    double m = validMacdValues[k];
                    signalLine[originalIndex] = Math.Round(s, 4);
                    histogram[originalIndex] = Math.Round(m - s, 4);
                }
            }

            return (macdLine, signalLine, histogram);
        }
    }
}
