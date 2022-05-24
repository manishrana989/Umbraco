using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace DCHMediaPicker.Data.Models
{
    public class DCHResponse
    {
        [JsonProperty("items")]
        public IEnumerable<DCHMediaItem> Items { get; set; }

        [JsonProperty("totalItemCount")]
        public int TotalItems { get; set; }

        [JsonProperty("returned_items")]
        public int ReturnedItems { get; set; }
    }

    public class DCHMediaItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("properties")]
        public DCHMediaProperty Properties { get; set; }

        [JsonProperty("renditions")]
        public DCHMediaRendition Renditions { get; set; }

        [JsonProperty("relations")]
        public JObject Relations { get; set; }

        public class DCHMediaProperty
        {
            [JsonProperty("FileName")]
            public string FileName { get; set; }

            [JsonProperty("Title")]
            public string Title { get; set; }
        }

        public class DCHMediaRendition
        {
            [JsonProperty("thumbnail")]
            public IEnumerable<DCHMediaRenditionItem> Thumbnail { get; set; }

            public class DCHMediaRenditionItem
            {
                [JsonProperty("href")]
                public string Href { get; set; }
            }
        }
    }
}