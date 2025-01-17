﻿using CryptoBotCore.API;
using CryptoBotCore.CosmosDB;
using CryptoBotCore.CosmosDB.Model;
using CryptoBotCore.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace CryptoBotCore.BotStrategies
{
    public class AccumulationBot
    {

        [NonSerialized]
        private Dictionary<string, CoinmateAPI> coinmateAPIs;

        TelegramBotClient TelegramBot { get; set; }

        private CosmosDbContext _cosmosDbContext;

        ILogger Log;

        public AccumulationBot(ILogger log)
        {
            Log = log;
        }


        public async Task Tick()
        {

            try
            {

                StringBuilder sbInformationMessage = new StringBuilder();
                Log.LogInformation("Start Tick: " + DateTime.Now);

                if (coinmateAPIs == null)
                {
                    inicializeAPI(BotConfiguration.Currency);
                }

                var pair = $"{BotConfiguration.Currency}_{BotConfiguration.Fiat}";

                var initBalance = coinmateAPIs[BotConfiguration.Currency].getBalances();

                double FiatBalance = initBalance.Where(x => x.currency == BotConfiguration.Fiat).Sum(x => x.available);



                Dictionary<string, StringBuilder> sb_actions = new Dictionary<string, StringBuilder>();


                if (FiatBalance > BotConfiguration.ChunkSize)
                {
                    var response = coinmateAPIs[BotConfiguration.Currency].buy(BotConfiguration.ChunkSize, 0, true, OrderType.ExchangeMarket);

                    Log.LogInformation($"Market buy {BotConfiguration.Currency} for {BotConfiguration.ChunkSize} {BotConfiguration.Fiat}");

                    //Serializer.SendEmail("Accumulation Bot - Buy", "You just spent " + schedule.FiatChunk + " CZK on " + schedule.Currency + ".", configuration.userId, forceEmail);
                }
                else
                {
                    await SendMessageAsync($"Not enough money ({FiatBalance} {BotConfiguration.Fiat})", MessageTypeEnum.Warning);
                    return;
                }
      

                var afterBalance = coinmateAPIs[BotConfiguration.Currency].getBalances();
                double FiatAfterBuy = afterBalance.Where(x => x.currency == BotConfiguration.Fiat).Sum(x => x.available);

                Dictionary<string, double> fees = new Dictionary<string, double>();

                if(BotConfiguration.Currency == "BTC")
                {
                    fees["BTC"] = coinmateAPIs["BTC"].getBTCWithdrawalFee();
                }

                fees["LTC"] = 0.0004;
                fees["ETH"] = 0.01;
                fees["DSH"] = 0.00001;

                
                var price = coinmateAPIs[BotConfiguration.Currency].getActualExchangeRate().Item1;


                double available = afterBalance.Where(x => x.currency == BotConfiguration.Currency).Sum(x => x.available);
                double init = initBalance.Where(x => x.currency == BotConfiguration.Currency).Sum(x => x.available);

                double fee_cost = (fees[BotConfiguration.Currency] / available);

                double TAKER_FEE = 1.0035;
                double buyPrice = ((FiatBalance - FiatAfterBuy) / TAKER_FEE) / (available - init);

                sbInformationMessage.Append("<b>Accumulation:</b> " + (available - init).ToString("N8") + " " + BotConfiguration.Currency + " for " + BotConfiguration.ChunkSize.ToString("N2") + $" {BotConfiguration.Fiat} @ " + (buyPrice).ToString("N2") + $" {BotConfiguration.Fiat}").Append("\r\n");

                //Send them home
                if (BotConfiguration.WithdrawalEnabled && 
                    !String.IsNullOrEmpty(BotConfiguration.WithdrawalAddress) &&
                    fee_cost <= BotConfiguration.MaxWithdrawalPercentageFee &&
                    (BotConfiguration.MaxWithdrawalAbsoluteFee == -1 || (fees[BotConfiguration.Currency] * price) <= BotConfiguration.MaxWithdrawalAbsoluteFee)
                    )
                {
                    coinmateAPIs[BotConfiguration.Currency].Withdraw(available, BotConfiguration.WithdrawalAddress);

                    sbInformationMessage.Append("<b>Withdrawal:</b> " + available.ToString("N8") + " " + BotConfiguration.Currency + " to " + BotConfiguration.WithdrawalAddress + " with " + (fee_cost * 100).ToString("N2") + " % fee").Append("\r\n");
                    //Serializer.SendEmail("Accumulation Bot - Withdraw", "Withdraw of " + + " " + increase_string + " to " + schedule.WithdrawalAddress + ".", configuration.userId, forceEmail);
                }
                else
                {
                    List<string> reason = new List<string>();
                    if (fee_cost > BotConfiguration.MaxWithdrawalPercentageFee)
                        reason.Add("Limit exceeded");
                    if (String.IsNullOrEmpty(BotConfiguration.WithdrawalAddress))
                        reason.Add("No address");
                    if (!BotConfiguration.WithdrawalEnabled)
                        reason.Add("Turned off");

                    sbInformationMessage.Append("<b>Withdrawal:</b> Denied - [" + String.Join(", ", reason) + "] - fee cost " + (fee_cost * 100).ToString("N2") + " %, limit " + (BotConfiguration.MaxWithdrawalPercentageFee * 100).ToString("N2") + " %").Append("\r\n");
                }

                _cosmosDbContext = new CosmosDbContext();

                var accumulationSummary = await _cosmosDbContext.GetAccumulationSummary(pair);

                AccumulationSummary accSumOLD = null;

                //Aktualizace záznamu v CosmosDB na novou strukturu PartitionKey
                if (accumulationSummary.Buys == 0)
                {
                    accSumOLD = await _cosmosDbContext.GetAccumulationSummary(BotConfiguration.Currency);
                    accumulationSummary.AccumulatedCryptoAmount = accSumOLD.AccumulatedCryptoAmount;
                    accumulationSummary.Buys = accSumOLD.Buys;
                    accumulationSummary.InvestedFiatAmount = accSumOLD.InvestedFiatAmount;
                }

                

                accumulationSummary.Buys += 1;
                accumulationSummary.InvestedFiatAmount += (FiatBalance - FiatAfterBuy);
                accumulationSummary.AccumulatedCryptoAmount += (available - init);

                await _cosmosDbContext.UpdateItemAsync(accumulationSummary);

                if(accSumOLD != null)
                {
                    //Smazání starého záznamu z předchozí verze bota
                    await _cosmosDbContext.DeleteItemAsync(accSumOLD.Id.ToString(), accSumOLD.CryptoName);
                }


                var profit = ((accumulationSummary.AccumulatedCryptoAmount * price) / accumulationSummary.InvestedFiatAmount) - 1;

                StringBuilder sb = new StringBuilder();
                sb.Append("🛒 <b>[ACTIONS]</b>").Append("\r\n");
                sb.Append(sbInformationMessage.ToString());
                sb.Append("").Append("\r\n");
                sb.Append("ℹ️ <b>[SUMMARY]</b>").Append("\r\n");
                sb.Append("<b>Total accumulation</b>: " + accumulationSummary.AccumulatedCryptoAmount.ToString("N8") + " " + BotConfiguration.Currency + " (" + accumulationSummary.InvestedFiatAmount.ToString("N2") + $" {BotConfiguration.Fiat})").Append("\r\n");
                sb.Append("<b>Avg Accumulated Price</b>: " + (accumulationSummary.InvestedFiatAmount/accumulationSummary.AccumulatedCryptoAmount).ToString("N2") + $" {BotConfiguration.Fiat}/" + BotConfiguration.Currency).Append("\r\n");
                sb.Append("<b>Current Price</b>: " + price.ToString("N2") + $" {BotConfiguration.Fiat}/" + BotConfiguration.Currency).Append("\r\n");
                sb.Append("<b>Current Profit</b>: " + (profit * 100).ToString("N2") + " % (" + (profit * accumulationSummary.InvestedFiatAmount).ToString("N2") + $" {BotConfiguration.Fiat})").Append("\r\n");

                sb.Append("<b>Fiat balance</b>: " + FiatAfterBuy.ToString("N2") + $" {BotConfiguration.Fiat}").Append("\r\n");
                sb.Append("<b>Current balance</b>: " + afterBalance.Where(x => x.currency == BotConfiguration.Currency).Sum(x => x.available).ToString("N8") + " " + BotConfiguration.Currency).Append("\r\n");

                await SendMessageAsync(sb.ToString());
            }
            catch (Exception ex)
            {
                await SendMessageAsync(ex.ToString(), MessageTypeEnum.Error);
                return;
            }

        }

        private void inicializeAPI(string crypto)
        {
            this.coinmateAPIs = new Dictionary<string, CoinmateAPI>();
            this.coinmateAPIs[crypto] = new CoinmateAPI($"{crypto}_{BotConfiguration.Fiat}", BotConfiguration.CoinMateCredentials, Log);
        }

        public async Task<string> SendMessageAsync(string message, MessageTypeEnum messageType = MessageTypeEnum.Information, int attempt = 0)
        {
            switch (messageType)
            {
                case MessageTypeEnum.Information:
                    Log.LogInformation(message);
                    message = $"#AccuBot - #{BotConfiguration.Currency} - #{BotConfiguration.UserName}{Environment.NewLine}{message}";
                    break;
                case MessageTypeEnum.Warning:
                    Log.LogWarning(message);
                    message = $"⚠️ #AccuBot - #{BotConfiguration.Currency} - #{BotConfiguration.UserName}{Environment.NewLine}{message}";
                    break;
                case MessageTypeEnum.Error:
                    Log.LogError(message);
                    message = $"❌ #AccuBot - #{BotConfiguration.Currency} - #{BotConfiguration.UserName}{Environment.NewLine}{message}";
                    break;
            }

            try
            {
                TelegramBot = new TelegramBotClient(BotConfiguration.TelegramBot);
                await TelegramBot.SendTextMessageAsync(BotConfiguration.TelegramChannel, message, Telegram.Bot.Types.Enums.ParseMode.Html);
            }
            catch (Exception e)
            {
                if (attempt >= 2)
                {
                    Log.LogError(e, "SendTextMessageAsync error");
                    return e.ToString();
                }
                //Repeat if exception (i.e. too many requests) occured
                Thread.Sleep(300);
                return await SendMessageAsync(message, MessageTypeEnum.Error , ++attempt);
            }

            return null;
        }
    }
}
