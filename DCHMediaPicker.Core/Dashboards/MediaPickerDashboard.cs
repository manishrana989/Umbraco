using System;
using Umbraco.Core.Composing;
using Umbraco.Core.Dashboards;

namespace DCHMediaPicker.Core.Dashboards
{
    [Weight(30)]
    public class MediaPickerDashboard : IDashboard
    {
        public string Alias => "DCHMediaPickerDashboard";

        public string[] Sections => new[]
        {
            Umbraco.Core.Constants.Applications.Content
        };

        public string View => "/App_Plugins/DCHMediaPicker/Views/dashboard.html";

        public IAccessRule[] AccessRules => Array.Empty<IAccessRule>();
    }
}
