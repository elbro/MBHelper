using System;
using System.Threading;
using System.Threading.Tasks;
using BetfairAPI.BFExchange;
using BetfairAPI.BFGlobal;
using MBHelper.Models;
using BetfairAPI;

namespace AutoUpdater
{
    class Program
    {
        public static Betfair Betfair;
        public static bool LoggedOn = false;

        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    const string uuu = "";
                    const string ppp = "";

                    Betfair = new Betfair();
                    Betfair.MessageRedirect(UserMsg);
                    LoggedOn = Betfair.Login(uuu, ppp);
                    Console.WriteLine(LoggedOn ? "{0} - Connected to Betfair" : "{0} - Failed to login to Betfair",
                                      DateTime.Now);         

                    Console.WriteLine("{0} - Beggining automatic price refresh", DateTime.Now);
                    var priceRefresh = new RefreshPrices() {UpdateInterval = 60000};
                    priceRefresh.StartUpdate();

                    Console.WriteLine("{0} - Beggining automatic market refresh", DateTime.Now);
                    var marketRefresh = new BetfairRefresh(Betfair);
                    marketRefresh.StartUpdate();
                }
                   catch (Exception e)
                {
                    // Crashed for some reason, wait 1 minute then restart
                    Console.WriteLine(e);
                    Thread.Sleep(60000);                   
                }
            }
        }

        // Delegate method for incoming messages from betfair API
        private static void UserMsg(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
 