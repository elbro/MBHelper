using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using MBHelper.Models;
using NLog;

namespace AutoUpdater
{
    public class WilliamHill : Bookmaker
    {
        private readonly List<string> _marketTypes;

        public WilliamHill()
        {
            BookieID = 1;
            Name = "WilliamHill";

            Feeds = new List<XmlFeed>
                        {
                            new XmlFeed("Uk Football", @"http://pricefeeds.williamhill.com/oxipubserver?action=template&template=getHierarchyByMarketType&classId=1&marketSort=MR&filterBIR=N"),
                            new XmlFeed("Euro Football", @"http://pricefeeds.williamhill.com/oxipubserver?action=template&template=getHierarchyByMarketType&classId=46&marketSort=MR&filterBIR=N"),
                            new XmlFeed("Horse Racing", @"http://pricefeeds.williamhill.com/oxipubserver?action=template&template=getHierarchyByMarketType&classId=2&marketSort=--&filterBIR=N")
                        };

            _marketTypes = new List<string> {"Match Betting", "Win"};
        }
        
        public override int GetOdds(XmlFeed feed)
        {
            var count = 0;

            var events = feed.Xml.Element("oxip").Element("response").Element("williamhill").Element("class");
            var horse = events.Attribute("name").Value.Contains("Horse");
            var allMarkets = DbContext.Markets.AsEnumerable().ToList();
            
            foreach (var league in events.Elements())
            {
                var leagueName = league.Attribute("name").Value.Trim().ToLower();

                var markets = league.Elements().Where(x => _marketTypes.Any(type => x.Attribute("name").Value.Contains(type)));

                foreach (var market in markets)
                {
                    count++;
                    var marketName = market.Attribute("name").Value.ToLower();
                    marketName =  horse ? leagueName: marketName.Substring(0, marketName.IndexOf(" - ", System.StringComparison.Ordinal));

                    var eventID = horse ? 7 : 1;

                    // just use date for now, utc issues!
                    var date = Helper.CombineDateTime(market.Attribute("date").Value, market.Attribute("time").Value);                    

                    var dbMkts = allMarkets.Where(x => x.EventTypeID == eventID && x.StartTime.Equals(date)
                                                    && x.Name.ToLower().CompareMarket(marketName)).ToList();
                    // If market is not found on betfair move on
                    if (!dbMkts.Any())
                        continue;

                    var dbMkt = dbMkts.First();

                    // if there is more than one market for these teams (very possible!)
                    if (dbMkts.Count() > 1)
                    {
                        // Compare league to get the right one
                        dbMkt = dbMkts.FirstOrDefault(x => x.Details.ToLower().CompareLeague(leagueName));
                        // may still be more than one, just skip. Log!
                        if (dbMkt == null)
                        {
                            Message("Error matching market");

                            foreach (var mkt in dbMkts)
                            {
                                Message(string.Format("{0} - id: {1}", mkt.Name, mkt.BetfairID));
                            }
                            continue;
                        }
                    }

                    foreach (var runner in market.Elements("participant"))
                    {
                        if (runner.Attribute("odds").Value == "SP") continue;

                        var runnerName = runner.Attribute("name").Value.ToLower();
                        var newOdds = (double)runner.Attribute("oddsDecimal");
                        UpdatePrice(dbMkt, runnerName, newOdds);
                    }
                }
            }

            return count;
        }

    }
}