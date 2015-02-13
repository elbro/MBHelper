using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BetfairAPI;
using BetfairAPI.BFExchange;
using MBHelper.Models;

namespace AutoUpdater
{
    public class BetfairRefresh
    {
        private Betfair _betfair;
        private static readonly OddsContext DbContext = new OddsContext();
        private bool _startUpdate;

        public BetfairRefresh(Betfair betfair)
        {
            _betfair = betfair;
        }

        ~BetfairRefresh()
        {
            _startUpdate = false;
        }

        // Must ensure we dont exceed throttle of 60 calls per minute
        public void StartUpdate()
        {
            _startUpdate = true;

            while (_startUpdate)
            {
                // Get all markets currently in the database
                //var markets = DbContext.Prices.Select(x => x.Runner.Market).Distinct().ToList();
                var markets = DbContext.Markets.ToList();
                var count = 0;
                var nextUpdateTime = DateTime.Now.AddMinutes(1);

                foreach (var mkt in markets)
                {          
                    if (count == 60)
                    {
                        try
                        {
                            DbContext.SaveChanges();
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            // Break to refresh market list
                            break;
                        }

                        var now = DateTime.Now;
                        // 60 calls made already, sleep for the rest of the minute
                        if (nextUpdateTime > now )
                            Thread.Sleep(nextUpdateTime.Subtract(now));

                        // Reset counter and next update time
                        nextUpdateTime = DateTime.Now.AddMinutes(1);
                        count = 0;
                    }

                    GetMarketPricesCompressedResp compressedPricesResp;

                    if (!_betfair.GetMarketPricesCompressed(mkt.BetfairID, out compressedPricesResp))
                    {
                        // Likely Exceeded Throttle for some reason
                        Thread.Sleep(5000);
                        continue;
                    }

                    // Update Prices for market object                
                    var success = mkt.UpdateMarketPricesCompressed(ref compressedPricesResp);
                    count++;

                    // Delete market if it has turned in play or expired
                    if (!success)
                    {                        
                        DbContext.Markets.Remove(mkt);
                        continue;
                    }

                    // Update the market
                    DbContext.Entry(mkt).State = EntityState.Modified;
                                                    
                }
            }
        }

    }
}
