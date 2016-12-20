namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addedAzureId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ToDoes", "AzureId", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ToDoes", "AzureId");
        }
    }
}
