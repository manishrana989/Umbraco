using System.Collections.Generic;

namespace DCHMediaPicker.Core.Models
{
    public class DashboardResponse
    {
        public IList<TrackedItem> Items { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}