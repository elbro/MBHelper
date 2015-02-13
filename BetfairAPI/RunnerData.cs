using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetfairAPI
{
    public struct Price
    {
        public double Amount;
        public double Odds;
    }

    public class RunnerData
    {
        public Price[] BackPrice;
        public Price[] LayPrice;

        public string Name { get; set; }
        public int ID;

        public int AsianLineID;
        public double Handicap;
        
        public double BSP;
        public double LastPriceMatched;

        public bool Vacant;
        public int GridRow;
        
        public RunnerData()
        {
            Name = "";
            ID = -1;

            AsianLineID = 0;
            Handicap = 0;
            BSP = 0;
            Vacant = false;
            GridRow = -1;

            BackPrice = new Price[3];
            LayPrice = new Price[3];
        }
    }
}
