using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace MBHelper.Models
{
    public class OddsContext : DbContext
    {
        public DbSet<Bookmaker> Bookmakers { get; set; }
        public DbSet<Market> Markets { get; set; }
        public DbSet<Runner> Runners { get; set; }
        public DbSet<Price> Prices { get; set; }
    }
}