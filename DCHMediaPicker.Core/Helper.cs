using System.Collections.Specialized;
using System.Configuration;

namespace DCHMediaPicker.Core
{
    public static class Helper
    {
        public static int GetSkipAmount(int page, int itemsPerPage) => (page * itemsPerPage) - itemsPerPage;

        public static string GetAppSetting(string key)
        {
            var dchConfig = (NameValueCollection)ConfigurationManager.GetSection("dchMediaPicker");
            return dchConfig[key];
        }
    }
}