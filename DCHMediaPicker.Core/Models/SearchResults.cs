using System;
using System.Collections.Generic;

namespace DCHMediaPicker.Core.Models
{
    public class SearchResults
    {
        public IList<MediaItem> Items { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}