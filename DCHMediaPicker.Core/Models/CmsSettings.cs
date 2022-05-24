using Newtonsoft.Json;
using System.Collections.Generic;

namespace DCHMediaPicker.Core.Models
{
    public class CmsSettings
    {
        [JsonProperty("collections")]
        public List<Collection> Collections { get; set; }

        public class Collection
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("searchTerms")]
            public string SearchTerms { get; set; }

            [JsonProperty("collection")]
            public string CollectionType { get; set; }

            [JsonProperty("keywords")]
            public string Keywords { get; set; }

            [JsonProperty("days")]
            public int? Days { get; set; }
        }
    }
}