using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace AutoUpdater
{
    public class XmlFeed
    {
        private readonly Uri _feedUri;
        private string _xmlString;
        public bool Loaded { get; set; }
        public XDocument Xml { get; set; }
        public string Name { get; set; }

        public XmlFeed(string name, string uri)
        {
            Name = name;
            _feedUri = new Uri(uri);
            Loaded = false;
        }

        public void ParseFeed()
        {
            var count = 0;
            do
            {
                _xmlString = _feedUri.GetWebPage();

                if (count == 2)
                {
                    // Feed is likely down
                    //TODO: Log error
                    return;
                }

                try
                {
                    Xml = XDocument.Parse(_xmlString);

                    // Loaded successfully
                    Loaded = true;
                }
                catch (XmlException e)
                {
                    count++;
                    // Try again
                    Thread.Sleep(10000);
                }
                  
            }
            while (!Loaded);
        }
    }
}
