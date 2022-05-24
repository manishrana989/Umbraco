using DCHMediaPicker.Data.Migrations;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;
using Umbraco.Core.Migrations.Upgrade;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;

namespace DCHMediaPicker.Data.Components
{
    [RuntimeLevel(MinLevel = Umbraco.Core.RuntimeLevel.Run)]
    public class UpgradeComponentComposer : ComponentComposer<UpgradeComponent>
    { }

    public class UpgradeComponent : IComponent
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IMigrationBuilder _migrationBuilder;
        private readonly IKeyValueService _keyValueService;
        private readonly IProfilingLogger _logger;

        public UpgradeComponent(IScopeProvider scopeProvider, IMigrationBuilder migrationBuilder, IKeyValueService keyValueService, IProfilingLogger logger)
        {
            _scopeProvider = scopeProvider;
            _migrationBuilder = migrationBuilder;
            _keyValueService = keyValueService;
            _logger = logger;
        }

        public void Initialize()
        {
            var upgrader = new Upgrader(new UpgradeMigrationPlan());
            upgrader.Execute(_scopeProvider, _migrationBuilder, _keyValueService, _logger);
        }

        public void Terminate()
        {

        }
    }
}