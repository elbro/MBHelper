using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MBHelper.Models;

namespace AutoUpdater
{
    public abstract class Bookmaker
    {
        protected List<XmlFeed> Feeds;
        protected int BookieID;
        protected string Name;
        protected OddsContext DbContext = new OddsContext();

        public int MarketsRetrieved, PricesMatched, PricesUpdated;

        public virtual void StartParsing()
        {
            MarketsRetrieved = PricesMatched = PricesUpdated = 0;

            // Parse each feed
            foreach (var feed in Feeds)
            {
                feed.ParseFeed();

                if (feed.Loaded)
                {                    
                    MarketsRetrieved += GetOdds(feed);
                }
                else
                {                    
                    Message("Error loading feed: " + feed.Name);
                }
            }

            Message(MarketsRetrieved + " Markets Retrieved");
            Message(PricesMatched + " prices successfully matched, " + PricesUpdated + " of which updated");
        }

        public abstract int GetOdds(XmlFeed feed);


        public void UpdatePrice(Market market, string runnerName, double newPrice)
        {
            runnerName = Helper.CheckName(runnerName);

            var runnerData = Helper.GetRunner(market, runnerName);

            if (runnerData == null)
            {
                Message("Error matching runner data for: " + runnerName);
                return;
            }

            //TODO: Should do a dupcliate check here just in case
            // Get price object or create new one
            var price = runnerData.Prices.SingleOrDefault(x => x.BookmakerID == BookieID) ??
                        new Price { BookmakerID = BookieID, Runner = runnerData };

            // For records
            if (!price.Odds.Equals(newPrice)) PricesUpdated++;

            price.Odds = newPrice;
            price.LastUpdated = DateTime.UtcNow;

            if (price.ID == 0)
                DbContext.Prices.Add(price);
            else
                DbContext.Entry(price).State = EntityState.Modified;

            PricesMatched++;

            try
            {
                DbContext.SaveChanges();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public void Message(string msg)
        {
            Debug.WriteLine("{0} - {1}", Name, msg);
        }
    }
}
