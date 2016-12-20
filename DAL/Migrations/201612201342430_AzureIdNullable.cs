namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AzureIdNullable : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.ToDoes", "AzureId", c => c.Int());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.ToDoes", "AzureId", c => c.Int(nullable: false));
        }
    }
}
