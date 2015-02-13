using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdater.Bookies
{
    class Betclick : Bookmaker
    {
        public Betclick()
        {
            BookieID = 4;
            Name = "Betclick";
            Feeds = new List<XmlFeed>
                        {
                            new XmlFeed("Football", @"http://xml.cdn.betclic.com/odds_en.xml")
                        };
        }

        public override int GetOdds(XmlFeed feed)
        {
            var count = 0;
            var allMarkets = DbContext.Markets.AsEnumerable().ToList();

            var football = feed.Xml.Element("sports").Element("sport");


            foreach (var league in football.Elements())
            {
                var leagueName = league.Attribute("name").Value;
                var eventID = 1;

                foreach (var match in league.Elements())
                {
                    var matchName = match.Attribute("name").Value;

                    //2014-05-17T14:30:00
                    var dateString = match.Attribute("start_date").Value.Split('T');

                    var date = Helper.CombineDateTime(dateString[0], dateString[1]);


                    foreach (var market in match.Element("bets").Elements().Where(x => CheckMarketType(x.Attribute("name").Value)))
                    {
                        count++;

                        // Change market name to standard format of 'x v y'
                        matchName = matchName.Replace('-', 'v');

                        var dbMkts = allMarkets.Where(x => x.EventTypeID == eventID && x.StartTime.Equals(date)
                                                && x.Name.ToLower().CompareMarket(matchName)).ToList();

                        if (!dbMkts.Any()) continue;

                        var dbMkt = dbMkts.First();

                        // If there is more than one market for these teams
                        if (dbMkts.Count() > 1)
                        {
                            continue;
                        }

                        foreach (var runner in market.Elements("choice"))
                        {
                            if (runner.Attribute("odd").Value == "SP") continue;

                            var v = matchName.IndexOf(" v ", StringComparison.Ordinal);

                            var team1 = matchName.Substring(0, v).Trim();            
                            var team2 = matchName.Substring(v + 2).Trim();

                            var runnerName = runner.Attribute("name").Value.ToLower();

                            switch (runnerName)
                            {
                                case "%1%":
                                    runnerName = team1;
                                    break;
                                case "%2%":
                                    runnerName = team2;
                                    break;
                            }

                            UpdatePrice(dbMkt, runnerName, double.Parse(runner.Attribute("odd").Value));
                        }   
                    }
                }                                    

            }

            return count;
        }

        private bool CheckMarketType(string type)
        {
            if (type == "Match Result")
                return true;

            return false;
        }

    }
}
