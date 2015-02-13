using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MBHelper.Models
{
    public class Bookmaker
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Price> Prices { get; set; }
    }
}