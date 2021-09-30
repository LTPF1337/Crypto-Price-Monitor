using Binance.Net;
using Binance.Net.Objects;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PriceMonitor
{
    class Program
    {
        // function to calculate percent difference between two numbers
        static double percDiff(double v1, double v2)
        {
            return -((v1 - v2) / ((v1 + v2) / 2) * 100);
        }

        // function to send custom embed to a discord webhook
        static void sendCryptoEmbed(string cryptoName, double previousPrice, double newPrice, double diff)
        {
            var client = new RestClient("discord webhook URL here");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);

            string color = ((newPrice < previousPrice) ? 16735053 : 2287194).ToString();
            string priceChange = newPrice > previousPrice ? "up" : "down";

            var body = $"{{\"content\":null,\"embeds\":[{{\"title\":\"{cryptoName} is {priceChange} {Math.Abs(diff).ToString("0.000")}%\",\"color\":{color},\"fields\":[{{\"name\":\"Current Price\",\"value\":\"${newPrice}\",\"inline\":true}},{{\"name\":\"Previous Price\",\"value\":\"${previousPrice}\",\"inline\":true}}],\"footer\":{{\"text\":\"Crypto Alerts ~ LTPF#2410\"}}}}]}}";
            request.AddHeader("accept", " application/json");
            request.AddHeader("content-length", body.Length.ToString());
            request.AddHeader("content-type", " application/json");
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
        }

        // monitor thread
        async static void monitorThread()
        {
            Console.WriteLine("Thread started!");

            while(true)
            {
                // read all stored prices to compare later on
                string[] fileLines = File.ReadAllLines("coins/prices.txt");
                var client = new BinanceClient(new BinanceClientOptions());
                var callResult = await client.Spot.Market.GetPricesAsync();

                if (callResult.Success)
                {
                    // loop through all coins
                    foreach(var coin in callResult.Data)
                    {
                        // loop through all lines to check if coin is being monitored
                        for(int i = 0; i < fileLines.Length; i++)
                        {
                            // sanity check on line
                            if (fileLines[i].Length < 2)
                                continue;

                            // split the coin symbol and price from file, symbol:price
                            string symbol = fileLines[i].Split(':')[0];
                            double price = double.Parse(fileLines[i].Split(':')[1]);

                            // compare current coin from api to symbol in our file
                            if(coin.Symbol == symbol)
                            {
                                // calculate difference between current price and previous price from file
                                double diff = percDiff(price, (double)coin.Price);
                                // check if difference is greater-than or equal to 5%
                                if(Math.Abs(diff) >= 5)
                                {
                                    // send embed
                                    sendCryptoEmbed(coin.Symbol.Replace("USDT", ""), price, (double)coin.Price, diff);
                                    // update price on line
                                    fileLines[i] = $"{coin.Symbol}:{coin.Price}";
                                }
                            }
                        }
                    }
                }
                // update file with new prices
                File.WriteAllLines("coins/prices.txt", fileLines);
                // wait 5 seconds before looping back
                Thread.Sleep(5000);
            }
        }

        static void Main(string[] args)
        {
            Thread thr = new Thread(() => monitorThread());
            thr.Start();

            while(true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                    return;
            }
        }
    }
}
