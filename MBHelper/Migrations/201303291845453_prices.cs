namespace MBHelper.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class prices : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Prices",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Odds = c.Double(nullable: false),
                        LastUpdated = c.DateTime(nullable: false),
                        BookmakerID = c.Int(nullable: false),
                        RunnerID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Bookmakers", t => t.BookmakerID, cascadeDelete: true)
                .ForeignKey("dbo.Runners", t => t.RunnerID, cascadeDelete: true)
                .Index(t => t.BookmakerID)
                .Index(t => t.RunnerID);
            
            DropColumn("dbo.Runners", "WillHill");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Runners", "WillHill", c => c.Double());
            DropIndex("dbo.Prices", new[] { "RunnerID" });
            DropIndex("dbo.Prices", new[] { "BookmakerID" });
            DropForeignKey("dbo.Prices", "RunnerID", "dbo.Runners");
            DropForeignKey("dbo.Prices", "BookmakerID", "dbo.Bookmakers");
            DropTable("dbo.Prices");
        }
    }
}
