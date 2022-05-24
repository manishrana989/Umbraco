using Umbraco.Core.Migrations;

namespace DCHMediaPicker.Data.Migrations
{
    public class UpgradeMigrationPlan : MigrationPlan
    {
        public UpgradeMigrationPlan() : base("DCH Media Picker")
        {
            From(string.Empty).To<CreateTableMigration>("Create table");
        }
    }
}