using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebSocket4Net;

namespace MexLiq
{
    public class Program
    {
        private static WebSocket ws = new WebSocket("wss://www.bitmex.com/realtime?subscribe=liquidation");
        private static Dictionary<string, Liquidation> liqs = new Dictionary<string, Liquidation>();
        private static readonly string[] Prefixes = new string[] { "YIKES!", "GG!", "REKT!", "DESTROYED!", "SAVAGE!" };
        private static readonly string[] Suffixes = new string[] { "" /* zero case */, "DOUBLE KILL ⚡", "TRIPLE KILL ⚡", "QUADRUPLE KILL ⚡", "QUINTUPLE KILL ⚡", "SEXTUPLE KILL ⚡", "SEPTUPLE KILL ⚡" };
        private static DateTime recent;
        private static Random r = new Random();

        static void Main(string[] args)
        {
            Start();
            Console.ReadKey();
        }

        public static void Start()
        {
            ws.MessageReceived += (send, m) => ParseMessage(m.Message);
            ws.Closed += async (send, m) =>
            {
                Console.WriteLine("Connection lost!");
                Console.WriteLine("Attempting to reconnect in 2.5s...");
                await Task.Delay(2500);
                ws.Open();
            };
            ws.Opened += (send, m) =>
            {
                Console.WriteLine("Connection established!");
                Console.WriteLine("Listening...");
            };

            ws.Open();
        }

        private static void ParseMessage(string m)
        {
            dynamic x = JsonConvert.DeserializeObject<dynamic>(m);
            if (x.table == "liquidation" && x.action == "insert")
            {
                string orderID = x.data[0].orderID;
                if (!liqs.ContainsKey(orderID))
                {
                    string symbol = x.data[0].symbol;
                    string side = x.data[0].side;
                    double price = x.data[0].price;
                    int qty = x.data[0].leavesQty;
                    Liquidation l = new Liquidation(side, price, qty);
                    int recentcount = liqs.Where(y => y.Value.Timestamp > l.Timestamp - 15).ToList().Count;
                    liqs.Add(orderID, l);

                    string LongShort = (l.Side == "Buy") ? "short" : "long";
                    string BoughtSold = (l.Side == "Buy") ? "bought" : "sold";

                    string msg = $"{Prefixes[r.Next(0, Prefixes.Length - 1)]} Liquidated {LongShort} on {symbol}, {l.Size} {BoughtSold} @ {l.Price} ⚡ {Suffixes[recentcount]}";

                    Console.WriteLine(msg);

                    recent = DateTime.Now;
                }
                // PROBLEM: liqs will get infinitely large as time goes on
                //
                // SOLUTION: implement a system that periodically removes all stored values in liqs with Timestamp >15s ago
            }
        }

        private class Liquidation
        {
            public string Side { get; set; }
            public double Price { get; set; }
            public int Size { get; set; }
            public int Timestamp { get; set; }

            public Liquidation(string side, double price, int size)
            {
                Side = side;
                Price = price;
                Size = size;
                Timestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            }
        }
    }
}