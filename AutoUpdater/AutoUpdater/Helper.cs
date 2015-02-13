using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MBHelper.Models;

namespace AutoUpdater
{
    public static class Helper
    {
        /// <summary>
        /// Finds the correct market in database
        /// </summary>
        /// <param name="name">Market name in collection</param>
        /// <param name="compareWith">Name to look for</param>
        /// <returns> True if market is found</returns>
        public static bool CompareMarket(this string name, string compareWith)
        {
            if (name.Contains(compareWith) || compareWith.Contains(name))
                return true;

            // Football 1 = A, 2 = B
            var v = name.IndexOf(" v ", StringComparison.Ordinal);

            if (v == -1) return false;

            var team1 = name.Substring(0, v).Trim();            
            var team2 = name.Substring(v + 2).Trim();

            v = compareWith.IndexOf(" v ", StringComparison.Ordinal);
            if (v == -1) return false;

            var teamA = CheckName(compareWith.Substring(0, v).Trim());
            var teamB = CheckName(compareWith.Substring(v + 2).Trim());

            if ( (team1.Contains(teamA) && team2.Contains(teamB)) || (teamA.Contains(team1) && teamB.Contains(team2)) )
                return true;
         
            // Teams must have spaces in name
            var teamAsplit = teamA.Split(' ');
            var teamBsplit = teamB.Split(' ');

            var countA = teamAsplit.Where(team => team.Length > 2 && team != "utd").Count(team1.Contains);
            var countB = teamBsplit.Where(team => team.Length > 2 && team != "utd").Count(team2.Contains);

            if (team1 == teamA) countA++;
            if (team2 == teamB) countB++;

            // Value Needs testing
            if (countA < 1 || countB < 1) return false;

            
            return countA + countB > 2;
        }


        public static bool CompareLeague(this string name, string compareWith)
        {
            var split = compareWith.Split(' ');

            var count = split.Count(s => name.ToLower().Contains(s));

            return (count > 1);
        }


        public static bool CompareRunner(this string name, string compareWith)
        {
            if (name.Equals(compareWith) || name.Contains(compareWith) || compareWith.Contains(name))
            {
                return true;
            }

            var split = compareWith.Split(' ');

            var count = split.Where(s => s.Length > 2 && s != "the" && s != "u21" && s != "utd").Count(name.Contains);

            return count > 0;
        }


        public static double GetRunnerSimilarity(string runnerName, string compareWith)
        {
            var splitB = compareWith.Split(' ');

            double rating = splitB.Where(s => s.Length > 2 && s != "the" && s != "u21" && s != "utd").Count(runnerName.Contains);

            // Not a great match
            if (rating.Equals(1))
            {
                // More rating if exact match
                foreach (var part in runnerName.Split(' '))
                {
                    var equalParts = splitB.Where(s => s.Length > 2 && s != "the" && s != "u21" && s != "utd").Count(part.Equals);

                    if (equalParts > 0) rating += equalParts/2.0;

                }
            }

            return rating;
        }

        //TODO:: Try/catch and skip on exception but record details!
        public static Runner GetRunner(Market dbMkt, string runnerName)
        {
            runnerName = runnerName.ToLower();
            var runners = dbMkt.Runners.ToList();
            // Dundee and Dundee Utd are diff teams !
            if (runnerName.Contains("dundee"))
            {
                return runners.SingleOrDefault(x => x.Name.ToLower().Equals(runnerName));
            }

            Runner runner = null;
            try
            {
                runner = runners.SingleOrDefault(x => x.Name.ToLower().Contains(runnerName) || runnerName.Contains(x.Name.ToLower()));
            }
            catch (InvalidOperationException e)
            {
               // More than one found, match using similarity rating
            }
            
            if (runner == null)
            {
                var zero = 0.0;
                foreach (var runn in runners)
                {                  
                    var sim = GetRunnerSimilarity(runn.Name.ToLower(), runnerName);

                    if (zero.Equals(1) && sim.Equals(1))
                    {
                        //

                    }

                    if (sim > zero)
                    {
                        runner = runn;
                        zero = sim;
                    }                       
                        
                }
            }

            return runner;
        }

        // Manual list for team names for betfair matching
        public static string CheckName(string name)
        {
            name = name.ToLower().Trim();

            // Betfair always uses Utd instead of United
            name = name.Replace(" united", " utd");

            name = name.Replace("atletico ", "atl ");

            // Remove apostraphy's
            name = name.Replace("&apos;", "");
            name = name.Replace("'", "");

            name = name.Replace("-", " ");

            switch (name)
            {
                case "wolverhampton wanderers":
                    name = "wolves";
                    break;
                case "atletico madrid":
                    name = "atl madrid";
                    break;
                case "zurich":
                    name = "fc zurich";
                    break;
                case "dynamo moscow":
                    name = "dinamo moscow";
                    break;
                case "beira mar":
                    name = "beira-mar";
                    break; 
                case "atletico":
                    name = "atl";
                    break;
                case "athletic":
                    name = "ath";
                    break;   
                case "sparta":
                    name = "sarpsborg";
                    break;

            }

            return name;

        }

        // Use a WebClient object to download the data as a string
        public static string GetWebPage(this Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            using (var request = new WebClient())
            {
                //Download the data
                //var requestData = request.DownloadData(uri);
                //Return the data by encoding it back to text!
                //return Encoding.ASCII.GetString(requestData);

                // Return as a string, removing illegal characters.
                try
                {
                    return request.DownloadString(uri).Replace((char)(0x1F), ' ');
                }
                catch (WebException e)
                {
                    Thread.Sleep(6000);
                    return e.Message;                    
                }
            }
        }

        // Combine a date and time in to one Universal DateTime object
        public static DateTime CombineDateTime(string sDate, string sTime)
        {
            var date = DateTime.Parse(sDate);
            var time = TimeSpan.Parse(sTime);

            return (date + time).ToUniversalTime();
        }
    }
}
