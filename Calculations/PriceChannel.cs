﻿using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Nessos.LinqOptimizer.CSharp;
using OoplesFinance.StockIndicators.Models;
using static OoplesFinance.StockIndicators.Enums.SignalsClass;
using static OoplesFinance.StockIndicators.Helpers.SignalHelper;
using static OoplesFinance.StockIndicators.Helpers.CalculationsHelper;
using static OoplesFinance.StockIndicators.Helpers.MathHelper;
using OoplesFinance.StockIndicators.Enums;

namespace OoplesFinance.StockIndicators
{
    public static partial class Calculations
    {
        /// <summary>
        /// Calculates the price channel.
        /// </summary>
        /// <param name="stockData">The stock data.</param>
        /// <param name="maType">Type of the ma.</param>
        /// <param name="length">The length.</param>
        /// <param name="pct">The PCT.</param>
        /// <returns></returns>
        public static StockData CalculatePriceChannel(this StockData stockData, MovingAvgType maType, int length = 21, decimal pct = 0.06m)
        {
            List<decimal> upperPriceChannelList = new();
            List<decimal> lowerPriceChannelList = new();
            List<decimal> midPriceChannelList = new();
            List<Signal> signalsList = new();
            var (inputList, _, _, _, _) = GetInputValuesList(stockData);

            var emaList = GetMovingAverageList(stockData, maType, length, inputList);

            for (int i = 0; i < stockData.Count; i++)
            {
                decimal currentValue = inputList.ElementAtOrDefault(i);
                decimal currentEma = emaList.ElementAtOrDefault(i);
                decimal prevValue = i >= 1 ? inputList.ElementAtOrDefault(i - 1) : 0;

                decimal upperPriceChannel = currentEma * (1 + pct);
                upperPriceChannelList.Add(upperPriceChannel);

                decimal lowerPriceChannel = currentEma * (1 - pct);
                lowerPriceChannelList.Add(lowerPriceChannel);

                decimal prevMidPriceChannel = midPriceChannelList.LastOrDefault();
                decimal midPriceChannel = (upperPriceChannel + lowerPriceChannel) / 2;
                midPriceChannelList.Add(midPriceChannel);

                var signal = GetCompareSignal(currentValue - midPriceChannel, prevValue - prevMidPriceChannel);
                signalsList.Add(signal);
            }

            stockData.OutputValues = new()
            {
                { "UpperChannel", upperPriceChannelList },
                { "LowerChannel", lowerPriceChannelList },
                { "MiddleChannel", midPriceChannelList }
            };
            stockData.SignalsList = signalsList;
            stockData.CustomValuesList = new List<decimal>();
            stockData.IndicatorName = IndicatorName.PriceChannel;

            return stockData;
        }

        /// <summary>
        /// Calculates the donchian channels.
        /// </summary>
        /// <param name="stockData">The stock data.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public static StockData CalculateDonchianChannels(this StockData stockData, int length = 20)
        {
            List<decimal> upperChannelList = new();
            List<decimal> lowerChannelList = new();
            List<decimal> middleChannelList = new();
            List<Signal> signalsList = new();
            var (inputList, highList, lowList, _, _) = GetInputValuesList(stockData);
            var (highestList, lowestList) = GetMaxAndMinValuesList(highList, lowList, length);

            for (int i = 0; i < stockData.Count; i++)
            {
                decimal currentValue = inputList.ElementAtOrDefault(i);
                decimal prevValue = i >= 1 ? inputList.ElementAtOrDefault(i - 1) : 0;

                decimal upperChannel = highestList.ElementAtOrDefault(i);
                upperChannelList.Add(upperChannel);

                decimal lowerChannel = lowestList.ElementAtOrDefault(i);
                lowerChannelList.Add(lowerChannel);

                decimal prevMiddleChannel = middleChannelList.LastOrDefault();
                decimal middleChannel = (upperChannel + lowerChannel) / 2;
                middleChannelList.Add(middleChannel);

                var signal = GetCompareSignal(currentValue - middleChannel, prevValue - prevMiddleChannel);
                signalsList.Add(signal);
            }

            stockData.OutputValues = new()
            {
                { "UpperChannel", upperChannelList },
                { "LowerChannel", lowerChannelList },
                { "MiddleChannel", middleChannelList }
            };
            stockData.SignalsList = signalsList;
            stockData.CustomValuesList = new List<decimal>();
            stockData.IndicatorName = IndicatorName.DonchianChannels;

            return stockData;
        }

        /// <summary>
        /// Calculates the standard deviation channel.
        /// </summary>
        /// <param name="stockData">The stock data.</param>
        /// <param name="length">The length.</param>
        /// <param name="stdDevMult">The standard dev mult.</param>
        /// <returns></returns>
        public static StockData CalculateStandardDeviationChannel(this StockData stockData, int length = 40, decimal stdDevMult = 2)
        {
            List<decimal> upperBandList = new();
            List<decimal> lowerBandList = new();
            List<Signal> signalsList = new();
            var (inputList, _, _, _, _) = GetInputValuesList(stockData);

            var stdDeviationList = CalculateStandardDeviationVolatility(stockData, length).CustomValuesList;
            var regressionList = CalculateLinearRegression(stockData, length).CustomValuesList;

            for (int i = 0; i < stockData.Count; i++)
            {
                decimal middleBand = regressionList.ElementAtOrDefault(i);
                decimal currentValue = inputList.ElementAtOrDefault(i);
                decimal currentStdDev = stdDeviationList.ElementAtOrDefault(i);
                decimal prevValue = i >= 1 ? inputList.ElementAtOrDefault(i - 1) : 0;
                decimal prevMiddleBand = i >= 1 ? regressionList.ElementAtOrDefault(i - 1) : 0;

                decimal prevUpperBand = upperBandList.LastOrDefault();
                decimal upperBand = middleBand + (currentStdDev * stdDevMult);
                upperBandList.AddRounded(upperBand);

                decimal prevLowerBand = lowerBandList.LastOrDefault();
                decimal lowerBand = middleBand - (currentStdDev * stdDevMult);
                lowerBandList.AddRounded(lowerBand);

                Signal signal = GetBollingerBandsSignal(currentValue - middleBand, prevValue - prevMiddleBand, currentValue, prevValue, upperBand, prevUpperBand, lowerBand, prevLowerBand);
                signalsList.Add(signal);
            }

            stockData.OutputValues = new()
            {
                { "UpperBand", upperBandList },
                { "MiddleBand", regressionList },
                { "LowerBand", lowerBandList }
            };
            stockData.SignalsList = signalsList;
            stockData.CustomValuesList = new List<decimal>();
            stockData.IndicatorName = IndicatorName.StandardDeviationChannel;

            return stockData;
        }

        /// <summary>
        /// Calculates the stoller average range channels.
        /// </summary>
        /// <param name="stockData">The stock data.</param>
        /// <param name="maType">Type of the ma.</param>
        /// <param name="length">The length.</param>
        /// <param name="atrMult">The atr mult.</param>
        /// <returns></returns>
        public static StockData CalculateStollerAverageRangeChannels(this StockData stockData, MovingAvgType maType, int length = 14, decimal atrMult = 2)
        {
            List<decimal> upperBandList = new();
            List<decimal> lowerBandList = new();
            List<Signal> signalsList = new();
            var (inputList, _, _, _, _) = GetInputValuesList(stockData);

            var smaList = GetMovingAverageList(stockData, maType, length, inputList);
            var atrList = CalculateAverageTrueRange(stockData, maType, length).CustomValuesList;

            for (int i = 0; i < stockData.Count; i++)
            {
                decimal middleBand = smaList.ElementAtOrDefault(i);
                decimal currentValue = inputList.ElementAtOrDefault(i);
                decimal currentAtr = atrList.ElementAtOrDefault(i);
                decimal prevValue = i >= 1 ? inputList.ElementAtOrDefault(i - 1) : 0;
                decimal prevMiddleBand = i >= 1 ? smaList.ElementAtOrDefault(i - 1) : 0;

                decimal prevUpperBand = upperBandList.LastOrDefault();
                decimal upperBand = middleBand + (currentAtr * atrMult);
                upperBandList.AddRounded(upperBand);

                decimal prevLowerBand = lowerBandList.LastOrDefault();
                decimal lowerBand = middleBand - (currentAtr * atrMult);
                lowerBandList.AddRounded(lowerBand);

                Signal signal = GetBollingerBandsSignal(currentValue - middleBand, prevValue - prevMiddleBand, currentValue, prevValue, upperBand, prevUpperBand, lowerBand, prevLowerBand);
                signalsList.Add(signal);
            }

            stockData.OutputValues = new()
            {
                { "UpperBand", upperBandList },
                { "MiddleBand", smaList },
                { "LowerBand", lowerBandList }
            };
            stockData.SignalsList = signalsList;
            stockData.CustomValuesList = new List<decimal>();
            stockData.IndicatorName = IndicatorName.StollerAverageRangeChannels;

            return stockData;
        }

        /// <summary>
        /// Calculates the moving average channel.
        /// </summary>
        /// <param name="stockData">The stock data.</param>
        /// <param name="maType">Type of the ma.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public static StockData CalculateMovingAverageChannel(this StockData stockData, MovingAvgType maType, int length = 20)
        {
            List<decimal> midChannelList = new();
            List<Signal> signalsList = new();
            var (inputList, highList, lowList, _, _) = GetInputValuesList(stockData);

            var highMaList = GetMovingAverageList(stockData, maType, length, highList);
            var lowMaList = GetMovingAverageList(stockData, maType, length, lowList);

            for (int i = 0; i < stockData.Count; i++)
            {
                decimal currentValue = inputList.ElementAtOrDefault(i);
                decimal upperChannel = highMaList.ElementAtOrDefault(i);
                decimal lowerChannel = lowMaList.ElementAtOrDefault(i);
                decimal prevValue = i >= 1 ? inputList.ElementAtOrDefault(i - 1) : 0;

                decimal prevMidChannel = midChannelList.LastOrDefault();
                decimal midChannel = (upperChannel + lowerChannel) / 2;
                midChannelList.Add(midChannel);

                var signal = GetCompareSignal(currentValue - midChannel, prevValue - prevMidChannel);
                signalsList.Add(signal);
            }

            stockData.OutputValues = new()
            {
                { "UpperBand", highMaList },
                { "MiddleBand", midChannelList },
                { "LowerBand", lowMaList }
            };
            stockData.SignalsList = signalsList;
            stockData.CustomValuesList = new List<decimal>();
            stockData.IndicatorName = IndicatorName.MovingAverageChannel;

            return stockData;
        }

        /// <summary>
        /// Calculates the moving average envelope.
        /// </summary>
        /// <param name="stockData">The stock data.</param>
        /// <param name="maType">Type of the ma.</param>
        /// <param name="length">The length.</param>
        /// <param name="mult">The mult.</param>
        /// <returns></returns>
        public static StockData CalculateMovingAverageEnvelope(this StockData stockData, MovingAvgType maType, int length = 20, decimal mult = 0.025m)
        {
            List<decimal> upperEnvelopeList = new();
            List<decimal> lowerEnvelopeList = new();
            List<Signal> signalsList = new();
            var (inputList, _, _, _, _) = GetInputValuesList(stockData);

            var smaList = GetMovingAverageList(stockData, maType, length, inputList);

            for (int i = 0; i < stockData.Count; i++)
            {
                decimal currentSma20 = smaList.ElementAtOrDefault(i);
                decimal currentValue = inputList.ElementAtOrDefault(i);
                decimal prevValue = i >= 1 ? inputList.ElementAtOrDefault(i - 1) : 0;
                decimal prevSma20 = i >= 1 ? smaList.ElementAtOrDefault(i - 1) : 0;
                decimal factor = currentSma20 * mult;

                decimal upperEnvelope = currentSma20 + factor;
                upperEnvelopeList.Add(upperEnvelope);

                decimal lowerEnvelope = currentSma20 - factor;
                lowerEnvelopeList.Add(lowerEnvelope);

                var signal = GetCompareSignal(currentValue - currentSma20, prevValue - prevSma20);
                signalsList.Add(signal);
            }

            stockData.OutputValues = new()
            {
                { "UpperBand", upperEnvelopeList },
                { "MiddleBand", smaList },
                { "LowerBand", lowerEnvelopeList }
            };
            stockData.SignalsList = signalsList;
            stockData.CustomValuesList = new List<decimal>();
            stockData.IndicatorName = IndicatorName.MovingAverageEnvelope;

            return stockData;
        }

        /// <summary>
        /// Calculates the fractal chaos bands.
        /// </summary>
        /// <param name="stockData">The stock data.</param>
        /// <returns></returns>
        public static StockData CalculateFractalChaosBands(this StockData stockData)
        {
            List<decimal> upperBandList = new();
            List<decimal> lowerBandList = new();
            List<decimal> middleBandList = new();
            List<Signal> signalsList = new();
            var (inputList, highList, lowList, _, _) = GetInputValuesList(stockData);

            for (int i = 0; i < stockData.Count; i++)
            {
                decimal prevHigh1 = i >= 1 ? highList.ElementAtOrDefault(i - 1) : 0;
                decimal prevHigh2 = i >= 2 ? highList.ElementAtOrDefault(i - 2) : 0;
                decimal prevHigh3 = i >= 3 ? highList.ElementAtOrDefault(i - 3) : 0;
                decimal prevLow1 = i >= 1 ? lowList.ElementAtOrDefault(i - 1) : 0;
                decimal prevLow2 = i >= 2 ? lowList.ElementAtOrDefault(i - 2) : 0;
                decimal prevLow3 = i >= 3 ? lowList.ElementAtOrDefault(i - 3) : 0;
                decimal currentClose = inputList.ElementAtOrDefault(i);
                decimal prevClose = i >= 1 ? inputList.ElementAtOrDefault(i - 1) : 0;
                decimal oklUpper = prevHigh1 < prevHigh2 ? 1 : 0;
                decimal okrUpper = prevHigh3 < prevHigh2 ? 1 : 0;
                decimal oklLower = prevLow1 > prevLow2 ? 1 : 0;
                decimal okrLower = prevLow3 > prevLow2 ? 1 : 0;

                decimal prevUpperBand = upperBandList.LastOrDefault();
                decimal upperBand = oklUpper == 1 && okrUpper == 1 ? prevHigh2 : prevUpperBand;
                upperBandList.Add(upperBand);

                decimal prevLowerBand = lowerBandList.LastOrDefault();
                decimal lowerBand = oklLower == 1 && okrLower == 1 ? prevLow2 : prevLowerBand;
                lowerBandList.Add(lowerBand);

                decimal prevMiddleBand = middleBandList.LastOrDefault();
                decimal middleBand = (upperBand + lowerBand) / 2;
                middleBandList.Add(middleBand);

                var signal = GetBollingerBandsSignal(currentClose - middleBand, prevClose - prevMiddleBand, currentClose, prevClose, upperBand, prevUpperBand, lowerBand, prevLowerBand);
                signalsList.Add(signal);
            }

            stockData.OutputValues = new()
            {
                { "UpperBand", upperBandList },
                { "MiddleBand", middleBandList },
                { "LowerBand", lowerBandList }
            };
            stockData.SignalsList = signalsList;
            stockData.CustomValuesList = new List<decimal>();
            stockData.IndicatorName = IndicatorName.FractalChaosBands;

            return stockData;
        }

        /// <summary>
        /// Calculates the average true range channel.
        /// </summary>
        /// <param name="stockData">The stock data.</param>
        /// <param name="maType">Type of the ma.</param>
        /// <param name="length">The length.</param>
        /// <param name="mult">The mult.</param>
        /// <returns></returns>
        public static StockData CalculateAverageTrueRangeChannel(this StockData stockData, MovingAvgType maType, int length = 14, decimal mult = 2.5m)
        {
            List<decimal> innerTopAtrChannelList = new();
            List<decimal> innerBottomAtrChannelList = new();
            List<Signal> signalsList = new();
            var (inputList, _, _, _, _) = GetInputValuesList(stockData);

            var atrList = CalculateAverageTrueRange(stockData, maType, length).CustomValuesList;
            var smaList = GetMovingAverageList(stockData, maType, length, inputList);

            for (int i = 0; i < stockData.Count; i++)
            {
                decimal currentValue = inputList.ElementAtOrDefault(i);
                decimal atr = atrList.ElementAtOrDefault(i);
                decimal prevValue = i >= 1 ? inputList.ElementAtOrDefault(i - 1) : 0;
                decimal sma = smaList.ElementAtOrDefault(i);
                decimal prevSma = i >= 1 ? smaList.ElementAtOrDefault(i - 1) : 0;

                decimal prevTopInner = innerTopAtrChannelList.LastOrDefault();
                decimal topInner = Math.Round(currentValue + (atr * mult));
                innerTopAtrChannelList.Add(topInner);

                decimal prevBottomInner = innerBottomAtrChannelList.LastOrDefault();
                decimal bottomInner = Math.Round(currentValue - (atr * mult));
                innerBottomAtrChannelList.Add(bottomInner);

                var signal = GetBollingerBandsSignal(currentValue - sma, prevValue - prevSma, currentValue, prevValue, topInner, 
                    prevTopInner, bottomInner, prevBottomInner);
                signalsList.Add(signal);
            }

            stockData.OutputValues = new()
            {
                { "UpperBand", innerTopAtrChannelList },
                { "MiddleBand", smaList },
                { "LowerBand", innerBottomAtrChannelList }
            };
            stockData.SignalsList = signalsList;
            stockData.CustomValuesList = new List<decimal>();
            stockData.IndicatorName = IndicatorName.AverageTrueRangeChannel;

            return stockData;
        }
    }
}