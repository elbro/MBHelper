using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MBHelper.Models;

namespace MBHelper.Controllers
{
    public class HomeController : Controller
    {
        private OddsContext db = new OddsContext();
        private double comm = 0.05;
        
        public ActionResult Index(int[] bookieIds, float? minArb, float? maxArb, 
                                    float? minOdds, float? maxOdds, float? minLiquidity,
                                        DateTime? fromDate, DateTime? toDate, float? commRate)
        {
            ViewBag.Message = "Beta";                                 
                                   
            var arbList = new List<ArbitrageViewModel>();
            
            var prices = db.Prices.Include("Runner.Market").AsQueryable();

            if (commRate.HasValue) comm = commRate.Value / 100;

            if (bookieIds != null)
            {
                prices = prices.Where(x => bookieIds.Contains(x.BookmakerID));
            }

            if (minOdds.HasValue)
            {
                prices = prices.Where(x => x.Odds >= minOdds.Value && x.Odds <= maxOdds.Value);
            }

            if (minLiquidity.HasValue)
            {
                prices = prices.Where(x => x.Runner.Liquidity >= minLiquidity.Value);
            }
                       
            foreach (var price in prices)
            {               
                var runner = price.Runner;
                var date = runner.Market.StartTime;

                if (date < DateTime.UtcNow) continue;

                if (toDate.HasValue)
                {
                    
                    if (date.Date < fromDate.Value.Date || date.Date > toDate.Value.Date)
                        continue;
                    //prices = prices.Where(x => x.Runner.Market.StartTime.Date >= fromDate.Value.Date
                    //    && x.Runner.Market.StartTime.Date <= toDate.Value.Date);
                }

                var rating = CalcRating(price.Odds, runner.LayOdds);

                // Either both are null or neither
                if (minArb.HasValue)
                {
                    if (rating < minArb.Value || rating > maxArb.Value)
                        continue;
                }
                else // Cap at 10% default                                  
                    if (rating < 10) continue;                

                var backAge = Math.Round(DateTime.UtcNow.Subtract(price.LastUpdated).TotalMinutes, 1);

                #if !DEBUG   
                    if (backAge > 90) continue;
                #endif

                var arb = new ArbitrageViewModel
                {
                    Exchange = "Betfair",
                    Bookie = price.Bookmaker.Name,
                    Type = runner.Market.Type,
                    Name = runner.Market.Name,
                    Details = runner.Market.Details,
                    Sport = GetEventName(runner.Market.EventTypeID),
                    Bet = runner.Name,
                    BackOdds = Math.Round(price.Odds, 3),
                    Rating = rating,
                    LayOdds = runner.LayOdds,
                    Liquidity = (int)runner.Liquidity,
                    BackAge = backAge,
                    LayAge = Math.Round(DateTime.UtcNow.Subtract(runner.Market.LastUpdated).TotalMinutes, 1)
                };

                var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
                arb.Date = TimeZoneInfo.ConvertTimeFromUtc(runner.Market.StartTime, ukTimeZone);

                arb.ExchangeUrl = GetExchangeURL(arb.Sport, runner.Market.BetfairID);

                arbList.Add(arb);
            }

            if (Request.IsAjaxRequest())
            {
                return PartialView("_OddsTable", arbList);
            }

            var bookies = db.Bookmakers.AsEnumerable();
            ViewBag.Bookies = bookies.OrderBy(x => x.Name).Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Id.ToString()
            });       

            return View(arbList);            
        }

        private string GetExchangeURL(string sport, int betfairID)
        {
            const string url = "http://www.betfair.com/exchange/{sport}/market?id=1.{id}";

            return url.Replace("{sport}", sport.ToLower()).Replace("{id}", betfairID.ToString());
        }

        public string GetEventName(int eventID)
        {
            var name = "";
            switch (eventID)
            {
                case 1:
                    name = "Football";
                    break;
                case 7:
                    name = "Horse-Racing";
                    break;
            }

            return name;
        }

        /// <summary>
        /// Calculates an Arbitrate Rating of the bet
        /// </summary>
        /// <param name="back">The back odds</param>
        /// <param name="lay">The Lay odds</param>
        /// <returns></returns>
        public double CalcRating(double back, double lay)
        {
            const int backAmount = 10;     

            var toLay = (backAmount * back) / ((lay - 1) + (1 - comm));
            var layLoss = toLay * (lay - 1);

            var profit = backAmount * (back - 1) - layLoss;
            //var rating = profit/backAmount * 100;
            return Math.Round(profit * backAmount + 100, 2);          
        }

        public ActionResult About()
        {
            ViewBag.Message = "About Us";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Contact Us";
            return View();
        }
    }
}
