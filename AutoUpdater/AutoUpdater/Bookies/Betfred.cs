using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdater.Bookies
{
    class Betfred : Bookmaker
    {
        public Betfred()
        {
            BookieID = 3;
            Name = "Betfred";
            Feeds = new List<XmlFeed>
                        {
                            new XmlFeed("Horse Racing", @"http://xml.betfred.com/Horse-Racing-Daily.xml"),
                            new XmlFeed("Premiership", @"http://xml.betfred.com/Football-Premiership.xml")
                        };
        }

        private DateTime GetDatetime(string dateString, string timeString)
        {            
            var date =  DateTime.ParseExact(dateString,"yyyyMMdd",
                                       System.Globalization.CultureInfo.InvariantCulture);

            var time = TimeSpan.ParseExact(timeString, "hhmm", System.Globalization.CultureInfo.InvariantCulture);

            return (date + time).ToUniversalTime();
        }

        public override int GetOdds(XmlFeed feed)
        {
            var count = 0;

            var allMarkets = DbContext.Markets.AsEnumerable().ToList();

            var allEvents = feed.Xml.Element("category");
            var horse = allEvents.Attribute("name").Value.Contains("Horse");

            foreach (var match in allEvents.Elements())
            {
                var marketName = horse ? match.Attribute("meeting").Value.ToLower() : match.Attribute("name").Value.ToLower();

                var eventID = horse ? 7 : 1;

                if (!horse)
                {
                    if (!marketName.Contains(" v ")) continue;
                }

                var dateString = match.Attribute("date").Value;
                if (String.IsNullOrEmpty(dateString)) continue;

                var date = GetDatetime(dateString, match.Attribute("time").Value);
                if (date < DateTime.UtcNow) continue;

                foreach (var market in match.Elements("bettype"))
                {
                    if (!horse)
                        if (!CheckMarketType(market.Attribute("name").Value.ToLower())) continue;

                    count++;
                    // Get Market
                    var dbMkts = allMarkets.Where(x => x.EventTypeID == eventID && x.StartTime.Equals(date)
                                                    && x.Name.ToLower().CompareMarket(marketName)).ToList();

                    if (!dbMkts.Any()) continue;

                    var dbMkt = dbMkts.First();

                    // If there is more than one market for these teams (very possible!)
                    if (dbMkts.Count() > 1)
                    {
                        continue;
                    }   

                    foreach (var runner in market.Elements("bet"))
                    {
                        if (runner.Attribute("price").Value == "SP") continue;                        

                        var runnerName = runner.Attribute("name").Value.ToLower();
                        UpdatePrice(dbMkt, runnerName, double.Parse(runner.Attribute("priceDecimal").Value));    
                    }                       
                }
            }

            return count;
        }

        private bool CheckMarketType(string type)
        {
            if (type == "match betting" || type == "win or each way")
                return true;

            return false;
        }

    }
}
