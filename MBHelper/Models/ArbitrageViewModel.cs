using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace MBHelper.Models
{
    public class ArbitrageViewModel
    {
        public string Exchange { get; set; }
        public string Bookie { get; set; }      
        public string Sport { get; set; }       
        public string Name { get; set; }
        public string Details { get; set; }
        public string BookieUrl { get; set; }
        public string ExchangeUrl { get; set; }
        public DateTime Date { get; set; }

        private string _bet;
        public string Bet
        {
            get { return _bet; } 
            set { _bet = value.Replace("The ", ""); } 
        }

        public string Type { get; set; }
        public double Rating { get; set; }
        public double BackOdds { get; set; }
        public double BackAge { get; set; }
        public double LayOdds { get; set; }
        public double LayAge { get; set; }
        public int Liquidity { get; set; }
        
    }

}