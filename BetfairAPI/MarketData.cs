using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetfairAPI.BFExchange;

namespace BetfairAPI
{
    public class MarketData
    {
        public volatile object VarLock = new object();

        private int _id;
        public int ID
        {
            get { lock (VarLock) { return _id; } }
            set { lock (VarLock) { _id = value; } }
        }

        public int Runners;
        public int Winners;
        public string Name;
        public string Desc;
        public string Info;
        public string RemovedRunners;
        public string MenuPath;
        public string MktDisplayName;

        public int Delay;
        public string Status;
        public string PrevStatus;

        public bool BSPMarket;
        public int EventTypeID;

        public DateTime StartTime, LastUpdated;

        public RunnerData[] ListRunners;

        public double BackPercent, LayPercent;

        public bool Error;
        public string ErrorDescription;
        
        public MarketData()
        {
            ID = -1;
            PrevStatus = "Not Set";
            Error = false;
            ErrorDescription = "";
        }

        public void LoadNewMarket(ref GetMarketResp marketResp)
        {
            lock (VarLock)
            {
                Clear();

                ID = marketResp.market.marketId;
                Runners = marketResp.market.runners.Length;
                Winners = marketResp.market.numberOfWinners;
                Name = marketResp.market.name;
                Desc = marketResp.market.marketDescription;
                BSPMarket = marketResp.market.bspMarket;
                EventTypeID = marketResp.market.eventTypeId;
                StartTime = marketResp.market.marketTime;
                MenuPath = marketResp.market.menuPath;

                var displayTime = marketResp.market.marketDisplayTime;

                // Generate User friendly Market display name
                if (displayTime.ToString("dd-MMM-yyyy") != "01-Jan-0001")
                {                    
                    MktDisplayName = Winners > 1 ? String.Format("{0}\r\n{1}  {2}\r\n{3} Runners,  {4} Winners", MenuPath.Replace('\\', ' '), displayTime.ToString("HH:mm"), Name, Runners, Winners) 
                        : String.Format("{0}\r\n{1}  {2}\r\n{3} Runners,  Win Only", MenuPath.Replace('\\', ' '), displayTime.ToString("HH:mm"), Name, Runners);
                }
                else
                {
                    MktDisplayName = Winners > 1 ? String.Format("{0}\r\n{1}\r\n{2} Selections,  {3} Winners", MenuPath.Replace('\\', ' '), Name, Runners, Winners) 
                        : String.Format("{0}\r\n{1}\r\n{2} Selections,  Win Only", MenuPath.Replace('\\', ' '), Name, Runners);
                }
                
                MktDisplayName = MktDisplayName.TrimStart(' ');

                ListRunners = new RunnerData[Runners];

                // Ensure we dont continue if there are no runners
                if (marketResp.market.runners == null)
                    return;
                
                // Load runner data
                int r = 0;
                foreach (var runner in marketResp.market.runners)
                {
                    ListRunners[r] = new RunnerData
                                        {
                                            Name = runner.name,
                                            ID = runner.selectionId,
                                            Handicap = runner.handicap,
                                            AsianLineID = runner.asianLineId,
                                            GridRow = r + 1
                                        };
                    r++;
                }
            }

        }

        public bool UpdateMarketPrices(ref GetMarketPricesResp marketPricesResp)
        {
            lock (VarLock)
            {
                // Double check it is the same market
                if (marketPricesResp.marketPrices.marketId != ID)
                    return false;

                BackPercent = LayPercent = 0;
                Delay = marketPricesResp.marketPrices.delay;
                Status = Convert.ToString(marketPricesResp.marketPrices.marketStatus);
                Info = marketPricesResp.marketPrices.marketInfo;
                LastUpdated = marketPricesResp.header.timestamp;
                RemovedRunners = marketPricesResp.marketPrices.removedRunners;

                if (Runners == 0 || marketPricesResp.marketPrices.runnerPrices == null)
                    return false;

                var i = 0;
                foreach (var runnerPrices in marketPricesResp.marketPrices.runnerPrices)
                {
                    int rInd;
                    if (runnerPrices.selectionId == ListRunners[i].ID)
                    {
                        rInd = i;
                        i++;
                    }
                    else
                    {
                        if ((rInd = GetIndexFromID(runnerPrices.selectionId)) == -1)
                        {
                            TraceMsg("Error: UpdateMarketPrices() Could not match RunnerID ({0}) with known Market Runners.", runnerPrices.selectionId);
                            return false;
                        }
                    }

                    // Get Back prices
                    if (runnerPrices.bestPricesToBack != null)
                    {
                        foreach (var price in runnerPrices.bestPricesToBack)
                        {
                            ListRunners[rInd].BackPrice[price.depth - 1].Odds = price.price;
                            ListRunners[rInd].BackPrice[price.depth - 1].Amount = price.amountAvailable;
                        }
                    }

                    // Get Lay prices
                    if (runnerPrices.bestPricesToLay != null)
                    {
                        foreach (var price in runnerPrices.bestPricesToLay)
                        {
                            ListRunners[rInd].LayPrice[price.depth - 1].Odds = price.price;
                            ListRunners[rInd].LayPrice[price.depth - 1].Amount = price.amountAvailable;
                        }
                    }

                    // Get back and lay overall percentages
                    BackPercent += (100 / ListRunners[rInd].BackPrice[0].Odds);
                    LayPercent += (100 / ListRunners[rInd].LayPrice[0].Odds);
                }
            }
            // Update successful
            return true;
        }

        public bool UpdateMarketPricesCompressed(ref GetMarketPricesCompressedResp compressedPricesResp)
        {
            lock (VarLock)
            {
                LastUpdated = compressedPricesResp.header.timestamp;
                BackPercent = LayPercent = 0;                
                var responseString = compressedPricesResp.marketPrices.Replace("\\:", ";");
                var allData = responseString.Split(':');
                var marketData = allData[0].Split('~');

                // Double check we have the same market ID
                if (int.Parse(marketData[0]) != ID)
                    return false;

                //mktDataArr[1];  // string Currency
                Status = marketData[2];
                Delay = int.Parse(marketData[3]);
                Winners = int.Parse(marketData[4]);
                Info = marketData[5];
                //mktDataArr[6];  // bool Discount Allowed;
                //mktDataArr[7];  // string Market Base Rate;
                //mktDataArr[8];  // Long Refresh Time in MilliSeconds;
                //TraceMsg("RefreshTime (" + mktDataArr[8] + ")");
                RemovedRunners = marketData[9];
                //mktDataArr[10];  // string BSP Market = Y or N;

                // If there are no runners we have no need to continue
                if (Runners == 0)
                    return false;

                // For each runner in the market
                for (int r = 1; r < allData.Count(); r++)
                {
                    var runnerSplit = allData[r].Split('|');
                    var runnerData = runnerSplit[0].Split('~');

                    var tempID = int.Parse(runnerData[0]);

                    int index;
                    if (tempID != ListRunners[(r - 1)].ID)
                    {
                        // Get index of the runner
                        if ((index = GetIndexFromID(tempID)) < 0)
                        {
                            UserMsg("ERROR: UpdateMarketPricesCompressed()->GetIndexFromID(" + tempID.ToString() + ") FAILED: Could not find matching SELECTION ID");
                            return false;
                        }
                    }
                    else
                    {
                        // First element in runnerData (0)
                        index = (r - 1);
                    }

                    // runnerData[1] - int Order Index
                    // runnerData[2] - double Total Ammount Matched
                    ListRunners[index].LastPriceMatched = runnerData[3].Length > 0 ? double.Parse(runnerData[3]) : 0;
                    // runnerData[4] - double Handicap
                    // runnerData[5] - double Reduction Factor
                    ListRunners[index].Vacant = (runnerData[6] == "true"); // Vacant trap for greyhounds
                    // runnerData[7] - double FAR SP Price
                    // runnerData[8] - double NEAR SP Price
                    //TraceMsg("runnerData[9] = (" + runnerData[9] + ")");

                    // runnerData[9] - double Actual SP Price
                    if (runnerData[9] == "NaN")
                    {
                        ListRunners[index].BSP = 0;
                    }
                    else
                    {
                        ListRunners[index].BSP = runnerData[9].Length > 0 ? double.Parse(runnerData[9]) : 0;
                    }

                    // We hold 3 back and lay prices for each runner
                    // TODO: this seems a bit uncnessary, initalise with struct constructor!?
                    for (int i = 0; i < 3; i++)
                    {
                        ListRunners[index].BackPrice[i].Amount = 0;
                        ListRunners[index].BackPrice[i].Odds = 0;
                        ListRunners[index].LayPrice[i].Amount = 0;
                        ListRunners[index].LayPrice[i].Odds = 0;
                    }

                    var backPricesArr = runnerSplit[1].Split('~');
                    var layPricesArr = runnerSplit[2].Split('~');

                    // BackPricesArr[0] - double Odds
                    // BackPricesArr[1] - double Ammount Available
                    // BackPricesArr[2] - string 'L' = available to back, 'B' = available to Lay
                    // BackPricesArr[3] - int Depth, 1 = Best, 2,3

                    // Back odds - divide by 4 to get the number of prices, max 3
                    var numPrices = (backPricesArr.Length - 1) / 4;
                    int oddsIndex;
                    for (int i = 0; i < numPrices; i++)
                    {
                        oddsIndex = i * 4;
                        ListRunners[index].BackPrice[i].Odds = backPricesArr[oddsIndex].Length > 0 ? double.Parse(backPricesArr[oddsIndex]) : 0;
                        ListRunners[index].BackPrice[i].Amount = backPricesArr[(oddsIndex + 1)].Length > 0 ? double.Parse(backPricesArr[(oddsIndex + 1)]) : 0;

                        // Calc Back percentage based on best back price
                        if (i == 0)
                        {
                            BackPercent += (100 / ListRunners[index].BackPrice[i].Odds);
                        }
                    }

                    // Same again but for the lay odds
                    numPrices = (layPricesArr.Length - 1) / 4;
                    for (int i = 0; i < numPrices; i++)
                    {
                        oddsIndex = i * 4;

                        ListRunners[index].LayPrice[i].Odds = layPricesArr[oddsIndex].Length > 0 ? double.Parse(layPricesArr[oddsIndex]) : 0;
                        ListRunners[index].LayPrice[i].Amount = layPricesArr[(oddsIndex + 1)].Length > 0 ? double.Parse(layPricesArr[(oddsIndex + 1)]) : 0;

                        // Calc lay percentage based on best lay price
                        if (i == 0)
                        {
                            LayPercent += (100 / ListRunners[index].LayPrice[i].Odds);
                        }
                    }
                }
            }

            // If we reach here market has successfully been updated
            return true;
        }

        // Return the Runnerdata index of the ID passed, else -1
        private int GetIndexFromID(int id)
        {
            for (int i = 0; i < ListRunners.Count(); i++)
            {
                if( ListRunners[i].ID == id )
                {
                    return i;
                }
            }
            return -1;
        }

        #region Message Delegates
        // Delegate functions for message processing
        private UserMsgDelegate UserMsg;

        private void TraceMsg(string msg)
        {
            Debug.WriteLine(string.Format("{0}$ TRACEMSG: MarketData: {1}", DateTime.Now, msg));
        }

        public void TraceMsg(string format, params object[] objs)
        {
            Debug.WriteLine(string.Format(format, objs));
        }

        public void MessageRedirect(UserMsgDelegate msgHandler)
        {
            UserMsg = msgHandler;
        }
        #endregion

        // Clears Market and Runner data
        public void Clear()
        {
            ListRunners = null;
            ID = Runners = Winners = EventTypeID = 0;
            Name = Desc = "";            
            BSPMarket = false;
        }
    }
}
