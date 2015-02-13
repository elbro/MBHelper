namespace MBHelper.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LastUpdatedPrice : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Runners", "LastUpdated", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Runners", "LastUpdated");
        }
    }
}
