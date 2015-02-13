using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BetfairAPI.BFExchange;
using BetfairAPI.BFGlobal;

namespace BetfairAPI
{
    public delegate void UserMsgDelegate(string msg);

    public class Betfair
    {
        private const int ProductId = 82;
        private const int VendorSoftwareId = 0;

        private readonly BFGlobalService _bfGlobal;
        private readonly BFExchangeService _bfExchange;
        private readonly BFExchange.APIRequestHeader _exchReqHdr;
        private readonly BFGlobal.APIRequestHeader _globReqHdr;
                
        private string _username;
        private string _password;
        private string _sessionToken;
        private string _currency;

        public Dictionary<String, String> Abbreviations;

        public Betfair()
        {
            Debug.WriteLine("{0} - Initializing Betfair API", DateTime.Now);

            _bfGlobal = new BFGlobalService { EnableDecompression = true};
            _bfExchange = new BFExchangeService { EnableDecompression = true};

            _globReqHdr = new BFGlobal.APIRequestHeader();
            _exchReqHdr = new BFExchange.APIRequestHeader();

            _username = "";
            _password = "";
            _sessionToken = "";
            _currency = "GBP";

            LoadAbbreviations();
        }

        private void LoadAbbreviations()
        {
            Abbreviations = new Dictionary<string, string>();

            var executingPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var lines = File.ReadAllLines(executingPath+"\\Abbreviations.txt");
            
            foreach (var line in lines.Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Split('\t')))
            {
                Abbreviations.Add(line[1], line[0]);
            }
        }

        #region Message Delegates

        private UserMsgDelegate UserMsg;
        public void MessageRedirect(UserMsgDelegate messageHandler)
        {
            UserMsg = messageHandler;
        }

        #endregion



        public double GetBalance()
        {
            var request = new GetAccountFundsReq()
                              {
                                  header = _exchReqHdr
                              };

            var response = _bfExchange.getAccountFunds(request);

            return response.availBalance;
        }

        public bool GetActiveEventTypes(out GetEventTypesResp response)
        {
            const string serviceName = "getActiveEventTypes";

            Debug.WriteLine(string.Format("{0} - CBetfairAPI - {1}", DateTime.Now, serviceName));


            var request = new GetEventTypesReq {header = _globReqHdr};

            response = _bfGlobal.getActiveEventTypes(request);

            var success = CheckResponse(serviceName,
                                        Convert.ToString(response.header.errorCode),
                                        Convert.ToString(response.errorCode),
                                        response.header.sessionToken);

            foreach (var et in response.eventTypeItems)
            {
                Debug.WriteLine("EventItem: ({0}), ({1})", et.name, et.id);
            }
            return success;
        }

        public bool GetEvents(int eventId, out GetEventsResp response)
        {
            const string serviceName = "getEvents";
            Debug.WriteLine("{0} - BetfairAPI - {1}", DateTime.Now, serviceName);

            var request = new GetEventsReq {header = _globReqHdr, eventParentId = eventId};

            response = _bfGlobal.getEvents(request);

            var success = CheckResponse(serviceName,
                                          Convert.ToString(response.header.errorCode),
                                          Convert.ToString(response.errorCode),
                                          response.header.sessionToken);

            //foreach (var evt in response.eventItems)
            //{
            //    Debug.WriteLine("Name: " + evt.eventName + " id: " + evt.eventId);
            //}

            return success;
        }

        /// <summary>
        /// Gets all markets from a eventTypeId in the next 2 months
        /// </summary>
        /// <param name="eventId">EventTypeId from GetActiveEventTypes</param>
        /// <param name="toDate">retrieve events untill this date</param>
        /// <param name="response">The betfair response</param>
        public bool GetAllMarkets(int eventId, DateTime toDate, out GetAllMarketsResp response)
        {
            const string serviceName = "getAllMarkets";
            Debug.WriteLine("{0} - BetfairAPI - {1}", DateTime.Now, serviceName);

            var events = new int?[] {eventId};

            var request = new GetAllMarketsReq
                              {
                                  header = _exchReqHdr,
                                  eventTypeIds = events,
                                  fromDate = DateTime.Today,
                                  toDate = toDate
                              };

            response = _bfExchange.getAllMarkets(request);

            var success = CheckResponse(serviceName,
                                          Convert.ToString(response.header.errorCode),
                                          Convert.ToString(response.errorCode),
                                          response.header.sessionToken);

            return success;
        }

        public bool GetMarketPricesCompressed(int marketId, out GetMarketPricesCompressedResp response)
        {
            const string serviceName = "GetMarketPricesCompressed";
            Debug.WriteLine("{0} - BetfairAPI - {1}", DateTime.Now, serviceName);

            var request = new GetMarketPricesCompressedReq
                              {
                                  currencyCode = _currency,
                                  marketId = marketId,
                                  header = _exchReqHdr
                              };

            response = _bfExchange.getMarketPricesCompressed(request);

            return CheckResponse(serviceName,
                                        Convert.ToString(response.header.errorCode),
                                        Convert.ToString(response.errorCode),
                                        response.header.sessionToken);
        }

        public bool GetMarketPrices(int marketId, out GetMarketPricesResp response)
        {
            const string serviceName = "GetMarketPrices";
            Debug.WriteLine("{0} - BetfairAPI - {1}", DateTime.Now, serviceName);

            var request = new GetMarketPricesReq
            {
                currencyCode = _currency,
                marketId = marketId,
                header = _exchReqHdr
            };

            response = _bfExchange.getMarketPrices(request);

            return CheckResponse(serviceName,
                                        Convert.ToString(response.header.errorCode),
                                        Convert.ToString(response.errorCode),
                                        response.header.sessionToken);
        }

        // Get Market info for a market id
        public bool GetMarket(int marketId, out GetMarketResp marketResp)
        {
            const string serviceName = "GetMarketReq";

            var request = new GetMarketReq {header = _exchReqHdr, marketId = marketId};

            marketResp = _bfExchange.getMarket(request);

            return CheckResponse(serviceName,
                                 Convert.ToString(marketResp.header.errorCode),
                                 Convert.ToString(marketResp.errorCode),
                                 marketResp.header.sessionToken);
        }

        // Lite version of GetMarket
        public bool GetMarketInfo(int marketId, out GetMarketInfoResp marketResp)
        {
            const string serviceName = "GetMarketInfoReq";

            var request = new GetMarketInfoReq { header = _exchReqHdr, marketId = marketId };

            marketResp = _bfExchange.getMarketInfo(request);

            return CheckResponse(serviceName,
                                 Convert.ToString(marketResp.header.errorCode),
                                 Convert.ToString(marketResp.errorCode),
                                 marketResp.header.sessionToken);
        }

        public bool Login(string username, string password)
        {
            const string serviceName = "Login";
            Debug.WriteLine("{0} - BetfairAPI - {1}", DateTime.Now, serviceName);

            _username = username;
            _password = password;

            var request = new LoginReq
                              {
                                  username = _username,
                                  password = _password,
                                  productId = ProductId,
                                  vendorSoftwareId = VendorSoftwareId
                              };

            var response = _bfGlobal.login(request);

            var retCode = CheckResponse(serviceName,
                                         response.header.errorCode.ToString(),
                                         response.errorCode.ToString(),
                                         response.header.sessionToken);

            if (!retCode)
            {
                Debug.WriteLine("{0} - BetfairAPI - {1} - FAILED", DateTime.Now, serviceName);
                return false;
            }

            _currency = response.currency;

            Debug.WriteLine("{0} - BetfairAPI - {1} - OK", DateTime.Now, serviceName);

            return true;
        }

        public bool Logout()
        {
            const string serviceName = "LogOut";

            Debug.WriteLine("{0} - BetfairAPI - {1}", DateTime.Now, serviceName);


            var request = new LogoutReq {header = _globReqHdr};

            var response = _bfGlobal.logout(request);

            var bRetCode = CheckResponse(serviceName,
                                          Convert.ToString(response.header.errorCode),
                                          Convert.ToString(response.errorCode),
                                          response.header.sessionToken);

            if (bRetCode == false)
            {
                Debug.WriteLine("{0} - BetfairAPI - {1} - FAILED", DateTime.Now, serviceName);
                return false;
            }

            Debug.WriteLine("{0} - BetfairAPI - {1} - OK", DateTime.Now, serviceName);

            return true;
        }

        private bool CheckResponse(string serviceName, string hdrErrCd, string respErrCd, string sessionToken)
        {
            if (!string.IsNullOrEmpty(sessionToken))
            {
                _sessionToken = sessionToken;
                _globReqHdr.sessionToken = sessionToken;
                _exchReqHdr.sessionToken = sessionToken;
            }

            if (hdrErrCd != "OK")
            {
                UserMsg(string.Format("{0} - FAILED: Response.Header.ErrorCode = {1}", serviceName, hdrErrCd));
                return false;
            }
            if (respErrCd != "OK")
            {
                UserMsg(string.Format("{0} - FAILED: Response.ErrorCode = {1}", serviceName, respErrCd));
                return false;
            }

            return true;
        }
    }
}