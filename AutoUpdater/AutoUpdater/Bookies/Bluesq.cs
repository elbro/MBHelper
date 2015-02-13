using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using MBHelper.Models;
using NLog;

namespace AutoUpdater
{
    public class Bluesq : Bookmaker
    {        
        //private static Logger Logger = LogManager.GetCurrentClassLogger();

        public Bluesq()
        {
            BookieID = 2;
            Name = "Bluesq";

            Feeds = new List<XmlFeed>
                        {
                            new XmlFeed("Football", @"http://cubs.bluesq.com/cubs/cubs.php?action=getpage&thepage=385.xml"),
                            new XmlFeed("Horse Racing", @"http://cubs.bluesq.com/cubs/cubs.php?action=getpage&thepage=2.xml")                            
                        };
            
        }

        public override int GetOdds(XmlFeed feed)
        {
            var count = 0;
            var allMarkets = DbContext.Markets.AsEnumerable().ToList();

            var leagues = feed.Xml.Element("BSQCUBS").Element("Class");
            var horse = leagues.Element("Title").Value.Contains("Racing");
           
            foreach (var league in leagues.Elements("Type"))
            {
                var leagueName = league.Element("Title").Value.Trim().ToLower();

                foreach (var match in league.Elements("Event"))
                {
                    var matchName =  horse ? leagueName : match.Element("Description").Value.ToLower();

                    var market = match.Element("Market");

                    // Check market type, only 1x2 for now
                    if (!CheckMarketType(market.FirstAttribute.Value.ToLower())) continue;
                    count++;
                    // Find matching markets in db using name and date
                    var date = DateTime.Parse(match.Attribute("start_time").Value).ToUniversalTime();
                    if (date < DateTime.UtcNow) continue;

                    var dbMkts = allMarkets.Where(x => x.StartTime.Equals(date) && x.Name.ToLower().CompareMarket(matchName)).ToList();

                    // If market is not found on betfair move on
                    if (!dbMkts.Any()) continue;

                    var dbMkt = dbMkts.First();
                    // If there is more than one market for these teams (very possible!)
                    if (dbMkts.Count() > 1)
                    {
                        // Compare league to get the right one
                        dbMkt = dbMkts.SingleOrDefault(x => x.Name.ToLower().Contains(leagueName));
                        // may still be more than one, just skip. Log!
                        if (dbMkt == null)
                        {
                            Debug.WriteLine("Bluesq - Error matching market");

                            foreach (var mkt in dbMkts)
                            {
                                Debug.WriteLine("{0} - id: {1}", mkt.Name, mkt.BetfairID);
                            }
                            continue;
                        }
                    }

                    foreach (var runner in market.Elements("Occurrence"))
                    {
                        if (runner.Attribute("decimal") == null) continue;

                        var runnerName = runner.Element("Description").Value.ToLower();
                        UpdatePrice(dbMkt, runnerName, double.Parse(runner.Attribute("decimal").Value));               
                    }
                }
            }

            return count;
        }

        private bool CheckMarketType(string type)
        {
            if (type == "win/draw/win" || type == "win or each way")
                return true;

            return false;
        }


    }
}
