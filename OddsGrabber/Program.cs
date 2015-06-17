using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BetfairAPI.BFExchange;
using BetfairAPI.BFGlobal;
using MBHelper.Models;
using BetfairAPI;
using System.Threading;
using NLog;

namespace OddsGrabber
{
    class Program
    {       
        private static readonly OddsContext dbContext = new OddsContext();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();                               

        public const string MatchOdds = "Match Odds";
        public const string HTFT = "HT/FT";
        public const string OverUnder = "Over Under";

        public static Betfair Betfair;
        public static bool LoggedOn = false;

        public static void Main()
        {           
            Console.WriteLine("{0} - Beginning odds ingestion..", DateTime.Now);     
                        
            var stopwatch = Stopwatch.StartNew();

            var uuu = "username";  
            var ppp = "password";

            Betfair = new Betfair();
            Betfair.MessageRedirect(UserMsg);
            LoggedOn = Betfair.Login(uuu, ppp);
            
            Console.WriteLine(LoggedOn ? "{0} - Connected to Betfair" : "{0} - Failed to login to Betfair", DateTime.Now);

            try
            {
                IngestBetfairMarkets();   
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Trace(e.StackTrace);
            }
            
            stopwatch.Stop();                        
            Logger.Info("Completed. Time elapsed: {0}", stopwatch.Elapsed);
        }

        /// <summary>
        /// Ingest all markets and their initial prices we want from betfair
        /// Only loads markets which are not already in the database
        /// </summary>
        public static void IngestBetfairMarkets()
        {
            var today = DateTime.Today;

            // Get Soccer Markets for the next 2 months
            GetAllMarketsResp response;
            var success = Betfair.GetAllMarkets(1, today.AddMonths(2), out response);
            Console.WriteLine(success ? "{0} - Soccer markets retrieved" : "{0} - Failed to retrieve soccer markets", DateTime.Now);

            // Each market split by a ':'
            var markets = response.marketData.Split(':');

            // Each marketId for "Match Odds" markets
            var marketIds = (from market in markets
                             where !string.IsNullOrEmpty(market)
                             select market.Split('~') into split
                             where split.ElementAt(1).Equals("Match Odds")
                             select split.First()).ToList().ConvertAll(int.Parse);

            // Get Horse racing markets for the next 4 days
            GetAllMarketsResp horseMarkets;
            success = Betfair.GetAllMarkets(7, today.AddDays(2), out horseMarkets);
            Console.WriteLine(success ? "{0} - Horse Racing markets retrieved" : "{0} - Failed to retrieve horse racing markets", DateTime.Now);

            markets = horseMarkets.marketData.Split(':');

            marketIds.AddRange(markets.Where(mkt => !string.IsNullOrEmpty(mkt))
                                .Select(mkt => mkt.Split('~'))
                                .Where(x => CheckHorseMarket(x.ElementAt(3), x.ElementAt(1), x.ElementAt(5)))
                                .Select(x => x.First())
                                .ToList().ConvertAll(int.Parse));

            var marketsInDb = dbContext.Markets.Select(x => x.BetfairID).ToList();

            // Check for duplicates
            var duplicates = marketsInDb.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            if (duplicates.Any())
            {
                // Just Remove them all to be safe
                foreach (var id in duplicates)
                {
                    var mkts = dbContext.Markets.Where(x => x.BetfairID == id).ToList();

                    if (mkts.Count > 1)
                        mkts.ForEach(x => dbContext.Markets.Remove(x));
                }
                dbContext.SaveChanges();

                // Reload market list
                marketsInDb = dbContext.Markets.Select(x => x.BetfairID).ToList();
            }

            // Only load Markets which aren't already in the database, Remove any expired ones
            var newMarketIds = marketIds.Except(marketsInDb).ToList();            
            var oldMarketIds = marketsInDb.Except(marketIds).ToList();


            if (oldMarketIds.Any())
            {
                Console.WriteLine("{0} - Removing {1} old markets..", DateTime.Now, oldMarketIds.Count);

                foreach (var mkt in oldMarketIds.Select(id => dbContext.Markets.SingleOrDefault(x => x.BetfairID == id)))
                {
                    dbContext.Markets.Remove(mkt);
                    dbContext.SaveChanges();
                }
            }
            
            if (!newMarketIds.Any())
            {
                Console.WriteLine("{0} - No new markets to ingest!", DateTime.Now);
                Thread.Sleep(5000);
                return;
            }

            Console.Write("{0} - Loading " + newMarketIds.Count() + " new markets... ", DateTime.Now);
            var left = Console.CursorLeft;
            var top = Console.CursorTop;

            int count = 0, added = 0, inplay = 0;

            // Grab market data and initial compressed prices for each market id
            foreach (var marketId in newMarketIds)
            {
                count++;
                GetMarketResp marketResp;
                GetMarketPricesCompressedResp compressedPricesResp;

                if (!Betfair.GetMarket(marketId, out marketResp))
                {
                    // Likely Exceeded the free API Throttle - 5 p/m. Wait for a minute and try again
                    var timeNow = DateTime.UtcNow;
                    var almostNextUpdateTime = timeNow.AddMinutes(1);

                    // Set seconds of the next update time to 0
                    var nextUpdateTime = new DateTime(almostNextUpdateTime.Year, almostNextUpdateTime.Month, almostNextUpdateTime.Day, almostNextUpdateTime.Hour, almostNextUpdateTime.Minute, 0);

                    // sleep for the difference between now and nextUpdateTime
                    Thread.Sleep(nextUpdateTime.Subtract(timeNow));

                    // Try again
                    Betfair.GetMarket(marketId, out marketResp);
                }

                var percent = (double)(count * 100) / newMarketIds.Count;
                Console.SetCursorPosition(left, top);
                Console.Write(Math.Round(percent, 1) + "%   ");

                // Check if market is in play
                if (marketResp.market.marketTime.ToUniversalTime() < DateTime.UtcNow)
                {
                    inplay++;
                    Thread.Sleep(11000);
                    continue;
                }

                if (!Betfair.GetMarketPricesCompressed(marketId, out compressedPricesResp))
                {
                    continue;
                }

                var market = new MBHelper.Models.Market();
                
                market.LoadMarket(ref marketResp, Betfair.Abbreviations);
                    
                // Dont save if market has turned in play or has no runners for some reason
                if (!market.UpdateMarketPricesCompressed(ref compressedPricesResp))
                    continue;

                // Save new market to database
                dbContext.Markets.Add(market);
                dbContext.SaveChanges();
                added++;                      

                //TODO: Catch errors properly and log for debugging - 1 in 780
                       
                if (count < newMarketIds.Count)
                    Thread.Sleep(11000);
            }            

            Console.WriteLine("\n{0} - Betfair Ingestion Complete", DateTime.Now);
            Logger.Info("{0} Expired markets removed from database", oldMarketIds.Count);
            Logger.Info("{0} New markets added to database", added);
            Logger.Info("{0} In-play markets skipped", inplay);
        }

        // Filter out to be placed, forecast markets etc.
        private static bool CheckHorseMarket(string status, string name, string menuParts)
        {
            name = name.ToLower();
            menuParts = menuParts.ToLower();

            if (!status.Contains("ACTIVE")) 
                return false;

            if (menuParts.Contains("antepost") || menuParts.Contains("(dist)") || menuParts.Contains("daily win") || menuParts.Contains("(double)") || menuParts.Contains("avb")) 
            {
                return false;
            }
            

            if (name.Contains("place") || name.Contains(" tbp"))
            {
                return false;
            }                                     
            if (name.Contains("forecast") || name.Contains("reverse") || name.Contains(" v ") ||
                name.Contains("without ") || name.Contains("winning stall") || name.Contains(" vs ") ||
                name.Contains(" rfc ") || name.Contains(" fc ") || name.Contains("less than") || 
                name.Contains("more than") || name.Contains("lengths") || name.Contains("winning dist") ||
                name.Contains("top jockey") || name.Contains("dist") || name.Contains("finish") ||
                name.Contains("isp %") || name.Contains("irish") || name.Contains("french") ||
                name.Contains("welsh") || name.Contains("australian") || name.Contains("italian") ||
                name.Contains("winbsp") || name.Contains("fav sp") || name.Contains("the field"))
            {
                return false;
            }

            return true;        
        }

        // Delegate to handle messages from the Betfair wrapper
        private static void UserMsg(string msg)
        {
            Logger.Debug(msg);
        }
    }
}
