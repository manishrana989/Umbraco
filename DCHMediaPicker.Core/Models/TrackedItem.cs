using System;

namespace DCHMediaPicker.Core.Models
{
    public class TrackedItem
    {
        public int UserId { get; set; }
        public int NodeId { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Thumbnail { get; set; }
        public int MediaId { get; set; }
        public string FileName { get; set; }
        public DateTime? Expiry { get; set; }
        public string PageUrl { get; set; }
    }
}