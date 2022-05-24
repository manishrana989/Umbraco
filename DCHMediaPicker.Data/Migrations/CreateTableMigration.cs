using DCHMediaPicker.Data.Models;
using Umbraco.Core.Migrations;

namespace DCHMediaPicker.Data.Migrations
{
    public class CreateTableMigration : MigrationBase
    {
        public CreateTableMigration(IMigrationContext context)
            : base(context)
        {
        }

        public override void Migrate()
        {
            if (!TableExists("DCHTrackedMediaItem"))
            { 
                Create.Table<TrackedMediaItem>().Do();
            }
        }
    }
}