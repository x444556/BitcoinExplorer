using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace BitcoinExplorer
{
    class Program
    {
        public static List<char[]> TransactionHashes = new List<char[]>();
        public static List<char[]> BlockHashes = new List<char[]>();
        static void Main(string[] args)
        {
            if (args.Length >= 1 && (args[0] == "-?" || args[0] == "/?" || args[0] == "help"))
            {
                Console.WriteLine("Syntax: <minBitcoins> <backtime in hours> <options>");
                Console.WriteLine("Options:");
                Console.WriteLine(" -? | /? | help  -  show help");
                Console.WriteLine(" -nT             -  hide Transactions");
                Console.WriteLine(" -nB             -  hide Blocks");
                return;
            }
            string title = Console.Title;
            Task t = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        string url = "https://blockchain.info/q/bcperblock";
                        WebRequest request = WebRequest.Create(url);
                        WebResponse response = request.GetResponse();
                        if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                        {
                            Stream stream = response.GetResponseStream();
                            string d = new StreamReader(stream).ReadToEnd();
                            Console.Title = title + "   |   BTC/Block: " + d;
                            stream.Dispose();
                        }
                        request.Abort();
                        response.Close();
                        response.Dispose();
                    }
                    catch { }
                    Thread.Sleep(15000);
                }
            });
            RunExplorer(!args.Contains("-nT"), !args.Contains("-nB"), (args.Length >= 1 ? double.Parse(args[0]) : 0),
                (args.Length >= 2 ? int.Parse(args[1]) : 1));
        }
        public static DateTime UnixTimeToDateTime(ulong unixtime)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixtime).ToLocalTime();
            return dtDateTime;
        }
        public static string DateTimeToString(DateTime dateTime)
        {
            string s = dateTime.Day.ToString().PadLeft(2, '0') + "." + dateTime.Month.ToString().PadLeft(2, '0') + "." + 
                dateTime.Year.ToString().PadLeft(2, '0') + " - " + dateTime.Hour.ToString().PadLeft(2, '0') + ":" + 
                dateTime.Minute.ToString().PadLeft(2, '0') + ":" + dateTime.Second.ToString().PadLeft(2, '0');
            return s;
        }

        /// <summary>
        /// This function contains the code to run the explorer
        /// </summary>
        /// <param name="showTransactions">If set to true, transactions are displayed</param>
        /// <param name="showBlocks">If set to true, blocks are displayed</param>
        /// <param name="minBitcoin">The minimum amount in bitcoin that a transaction or block must be worth to be displayed.</param>
        /// <param name="backtime">specifies how many hours transactions can be old to be displayed</param>
        static void RunExplorer(bool showTransactions=true, bool showBlocks=true, double minBitcoin=0, int backtime=1)
        {
            try
            {
                if (showBlocks)
                {
                    string url = "https://blockchain.info/blocks/" + (long)DateTime.Now.ToUniversalTime().Subtract(
                        new DateTime(1970, 1, 2, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds + "?format=json";
                    WebRequest request = WebRequest.Create(url);
                    WebResponse response = request.GetResponse();
                    if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                    {
                        Stream stream = response.GetResponseStream();
                        string d = new StreamReader(stream).ReadToEnd();
                        Block[] data = JsonConvert.DeserializeObject<Block[]>(d);
                        Array.Reverse(data);
                        foreach (Block b in data)
                        {
                            bool found = false;
                            foreach (char[] bhca in BlockHashes)
                            {
                                if (b.hash.ToCharArray().SequenceEqual(bhca))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found && UnixTimeToDateTime(b.time) >= DateTime.Now - new TimeSpan(backtime, 0, 0))
                            {
                                BlockHashes.Add(b.hash.ToCharArray());
                                Console.WriteLine("[Block] " + DateTimeToString(UnixTimeToDateTime(b.time)) + ": " + b.hash);
                            }
                        }
                        stream.Dispose();
                    }
                    request.Abort();
                    response.Close();
                    response.Dispose();
                }
            }
            catch (Exception e)
            {
                // only used for debugging once
                // Console.Error.WriteLine(e);
            }
            while (true)
            {
                try
                {
                    if (showTransactions)
                    {
                        string url = "https://blockchain.info/unconfirmed-transactions?format=json";
                        WebRequest request = WebRequest.Create(url);
                        WebResponse response = request.GetResponse();
                        if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                        {
                            Stream stream = response.GetResponseStream();
                            string d = new StreamReader(stream).ReadToEnd();
                            Dictionary<string, Transaction[]> data = JsonConvert.DeserializeObject<Dictionary<string, Transaction[]>>(d);
                            foreach (Transaction t in data["txs"])
                            {
                                if (double.Parse(t.GetValueString(), CultureInfo.InvariantCulture) >= minBitcoin)
                                {
                                    bool found = false;
                                    foreach (char[] thca in TransactionHashes)
                                    {
                                        if (t.hash.ToCharArray().SequenceEqual(thca))
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (!found && UnixTimeToDateTime(t.time) >= DateTime.Now - new TimeSpan(backtime, 0, 0))
                                    {
                                        TransactionHashes.Add(t.hash.ToCharArray());
                                        Console.WriteLine("[Transaction] " + DateTimeToString(UnixTimeToDateTime(t.time)) + ": " + t.hash + " : " +
                                            t.GetValueString());
                                    }
                                }
                            }
                            data.Clear();
                            stream.Dispose();
                        }
                        request.Abort();
                        response.Close();
                        response.Dispose();
                    }
                    if (showBlocks)
                    {
                        string url = "https://blockchain.info/blocks/" + (long)DateTime.Now.ToUniversalTime().Subtract(
                            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds + "?format=json";
                        WebRequest request = WebRequest.Create(url);
                        WebResponse response = request.GetResponse();
                        if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                        {
                            Stream stream = response.GetResponseStream();
                            string d = new StreamReader(stream).ReadToEnd();
                            Block[] data = JsonConvert.DeserializeObject<Block[]>(d);
                            Array.Reverse(data);
                            foreach (Block b in data)
                            {
                                bool found = false;
                                foreach (char[] bhca in BlockHashes)
                                {
                                    if (b.hash.ToCharArray().SequenceEqual(bhca))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found && UnixTimeToDateTime(b.time) >= DateTime.Now - new TimeSpan(backtime, 0, 0))
                                {
                                    BlockHashes.Add(b.hash.ToCharArray());
                                    Console.WriteLine("[Block] " + DateTimeToString(UnixTimeToDateTime(b.time)) + ": " + b.hash);
                                }
                            }
                            stream.Dispose();
                        }
                        request.Abort();
                        response.Close();
                        response.Dispose();
                    }
                }
                catch(Exception e)
                {
                    // only used for debugging once
                    // Console.Error.WriteLine(e);
                }
                Thread.Sleep(500);
            }
        }
    }
    class Transaction
    {
        public int lock_time = 0;
        public int ver = 0;
        public int size = 0;
        public object[] inputs;
        public int weight = 0;
        public ulong time = 0;
        public long tx_index = 0;
        public int vin_sz = 0;
        public string hash = "";
        public int vout_sz = 0;
        public string relayed_by = "";
        public Output[] @out;

        public Transaction()
        {
            
        }

        public string GetValueString()
        {
            BigInteger tempValue = 0;
            foreach(Output o in @out) { tempValue += o.value; }
            string s = tempValue.ToString().PadLeft(9, '0');
            string result = s[0..^8].PadLeft(2, ' ');
            result += "." + s.Substring(s.Length - 8);
            return result.TrimEnd('0');
        }
    }
    class Output
    {
        public bool spent = false;
        public long tx_index = 0;
        public int type = 0;
        public BigInteger value = 0;
        public int n = 0;
        public string script = "";
    }
    class Block
    {
        public int height = 0;
        public string hash = "";
        public ulong time = 0;
        public long block_index = 0;
    }
}
