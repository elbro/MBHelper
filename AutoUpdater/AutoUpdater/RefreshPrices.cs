using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoUpdater.Bookies;
using BetfairAPI;
using BetfairAPI.BFExchange;
using MBHelper.Models;
using NLog;

namespace AutoUpdater
{
    public class RefreshPrices
    {
        public int UpdateInterval { get; set; }

        private Thread _tWillHill, _tBluesq, _tBetfred, _tbetClick;
        private volatile bool _threadsStopped, _stopThreads;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();  

        ~RefreshPrices()
        {
            StopUpdate();
        }

        private void NullThreads()
        {
            _tWillHill = _tBluesq = _tBetfred = _tbetClick = null;
        }

        public void StartUpdate()
        {
            if (_threadsStopped) return;

            NullThreads();
            _threadsStopped = false;
            _stopThreads = false;

            Message("Starting William Hill Thread");
            _tWillHill = new Thread(WilliamHill) { IsBackground = true, Name = "WilliamHill" };
            _tWillHill.Start();

            Message("Starting Bluesq Thread");
            // Bluesquare feeds down for the moment.
            //_tBluesq = new Thread(Bluesquare) { IsBackground = true, Name = "Bluesq" };
            //_tBluesq.Start();

            Message("Starting Betfred Thread");
            _tBetfred = new Thread(Betfred) { IsBackground = true, Name = "Betfred" };
            _tBetfred.Start();

            Message("Starting Betclick Thread");
            _tbetClick = new Thread(Betclick) { IsBackground = true, Name = "Betclick" };
            _tbetClick.Start();
        }

        public void StopUpdate()
        {
            if (_tWillHill == null) return;

            _stopThreads = true;  // try an orderly stop
            Message("Stopping AutoRefresh Threads");
            Thread.Sleep(300);

            if (!_threadsStopped)
            {
                // Orderly stop failed so abort threads manually
                Message("StopAutoRefresh() threads still running? - Issuing Abort()");
                _tWillHill.Abort();
                _tBluesq.Abort();
                _tBetfred.Abort();
                _tbetClick.Abort();
                Thread.Sleep(100);
            }

            NullThreads();
        }

        private void Betfred()
        {
            while (!_stopThreads)
            {
                var bookie = new Betfred();
                bookie.StartParsing();
                Thread.Sleep(UpdateInterval);
            }
        }

        private void Betclick()
        {
            while (!_stopThreads)
            {
                var bookie = new Betclick();
                bookie.StartParsing();
                Thread.Sleep(UpdateInterval);
            }
        }
              
        private void Bluesquare()
        {
            while (!_stopThreads)
            {
                var bluesq = new Bluesq();
                bluesq.StartParsing();
                Thread.Sleep(UpdateInterval);
            }

        }

        private void WilliamHill()
        {
            while (!_stopThreads)
            {
                var bookie = new WilliamHill();
                bookie.StartParsing();
                Thread.Sleep(UpdateInterval);                                        
            }
        }

        // Process Messages from class
        private void Message(string msg)
        {
            Debug.WriteLine("{0} - Odds Updater - {1}", DateTime.Now, msg);
        }        
    }
}
