using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BetfairAPI.BFExchange;
using BetfairAPI;

namespace MBHelper.Models
{
    public class Market
    {
        public int ID { get; set; }
        public int BetfairID { get; set; }
        public int EventTypeID { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Details { get; set; }        
        public DateTime StartTime { get; set; }
        public DateTime LastUpdated { get; set; }

        public virtual List<Runner> Runners { get; set; }

        public void LoadMarket(ref GetMarketResp marketResp, Dictionary<string,string> abbreviations )
        {
            Runners = new List<Runner>();

            BetfairID = marketResp.market.marketId;
            EventTypeID = marketResp.market.eventTypeId;
            Type = marketResp.market.name;

            var menuPath = marketResp.market.menuPath;

            var menuParts = menuPath.Split('\\');
            Name = menuParts.Last();

            // Football
            if (EventTypeID == 1)
            {
                if (menuParts.Length > 2)
                    Details = menuParts[1] + " - " + menuParts[2];
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (EventTypeID == 7) // Horse Racing
            {
                // Get full course name
                var abbrv = Name.Split(' ').First();
                try
                {
                    Name = Name.Replace(abbrv, abbreviations[abbrv]);
                }
                catch { } // Obscure market likely and not important               
                

                Details = marketResp.market.name;
                Type = "Winner";
            }

            
            StartTime = marketResp.market.marketTime.ToUniversalTime();       

            // Ensure we dont continue if there are no runners
            if (marketResp.market.runners == null)
                return;

            // Load runner data
            var runners = marketResp.market.runners.Select(selection => new Runner
                                                                    { 
                                                                        Name = selection.name, 
                                                                        MarketID = ID ,
                                                                        SelectionID = selection.selectionId
                                                                    }).ToList();
            
            Runners.AddRange(runners);
        }

        public bool UpdateMarketPricesCompressed(ref GetMarketPricesCompressedResp compressedPricesResp)
        {
                LastUpdated = compressedPricesResp.header.timestamp;
                var responseString = compressedPricesResp.marketPrices.Replace("\\:", ";");
                var allData = responseString.Split(':');
                var marketData = allData[0].Split('~');

                // Double check we have the same market ID
                if (int.Parse(marketData[0]) != BetfairID)
                    return false;

                //marketData[1];  // string Currency
                var status = marketData[2];
                var delay = int.Parse(marketData[3]);

                // Market is in Play/Expired
                if (delay > 0 || status == "CLOSED") return false;

                //Winners = int.Parse(marketData[4]);
                //marketData[6];  // bool Discount Allowed;
                //marketData[7];  // string Market Base Rate;
                //marketData[8];  // Long Refresh Time in MilliSeconds;
                
                //var removedRunners = marketData[9];
                //marketData[10];  // string BSP Market = Y or N;

                // If there are no runners we have no need to continue
                if (!Runners.Any()) return false;

                // For each runner in the market
                for (int r = 1; r < allData.Count(); r++)
                {
                    var runnerSplit = allData[r].Split('|');
                    var runnerData = runnerSplit[0].Split('~');

                    var selectionID = int.Parse(runnerData[0]);
                    // runnerData[1] - int Order Index
                    // runnerData[2] - double Total Ammount Matched
                    // runnerData[3] - last price Matched
                    // runnerData[4] - double Handicap
                    // runnerData[5] - double Reduction Factor
                    // runnerData[6] == "true"); // Vacant trap for greyhounds
                    // runnerData[7] - double FAR SP Price
                    // runnerData[8] - double NEAR SP Price
                    // runnerData[9] - double Actual SP Price  

                    var runner = Runners.SingleOrDefault(x => x.SelectionID == selectionID);

                    if (runner == null)
                    {
                        Debug.WriteLine("ERROR: UpdateMarketPricesCompressed - ID: " + selectionID + " Could not find matching SELECTION ID");
                        continue;                        
                    }
                 
                    var layPricesArr = runnerSplit[2].Split('~');
                    // PricesArr[0] - double Odds
                    // PricesArr[1] - double Ammount Available
                    // PricesArr[2] - string 'L' = available to back, 'B' = available to Lay
                    // PricesArr[3] - int Depth, 1 = Best, 2,3

                    if (layPricesArr.Length < 2) continue;

                    runner.LayOdds = layPricesArr[0].Length > 0 ? double.Parse(layPricesArr[0]) : 0;
                    runner.Liquidity = layPricesArr[1].Length > 0 ? double.Parse(layPricesArr[1]) : 0;
                }            

            // If we reach here market has successfully been updated
            return true;
        }
    }

    public class Runner
    {
        public int ID { get; set; }
        public int SelectionID { get; set; }
        public string Name { get; set; }
        public double LayOdds { get; set; }
        public double Liquidity { get; set; }
        public DateTime? LastUpdated { get; set; }

        public int MarketID { get; set; }
        public virtual Market Market { get; set; }

        public virtual ICollection<Price> Prices  { get; set; }
    }

    public class Price
    {
        public int ID { get; set; }
        public double Odds { get; set; }
        public DateTime LastUpdated { get; set; }

        public int BookmakerID { get; set; }
        public virtual Bookmaker Bookmaker { get; set; }

        public int RunnerID { get; set; }
        public virtual Runner Runner { get; set; }
    }
}