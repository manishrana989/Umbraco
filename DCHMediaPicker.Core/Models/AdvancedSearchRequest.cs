using Newtonsoft.Json;
using System;

namespace DCHMediaPicker.Core.Models
{
    public class AdvancedSearchRequest
    {
        [JsonProperty("searchTerms")]
        public string SearchTerms { get; set; }

        [JsonProperty("projectName")]
        public string ProjectName { get; set; }

        [JsonProperty("assetType")]
        public string AssetType { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("fileType")]
        public string FileType { get; set; }

        [JsonProperty("keywords")]
        public string Keywords { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("modified")]
        public AdvancedSearchRequestModified Modified { get; set; }

        [JsonProperty("nodeId")]
        public int NodeId { get; set; }

        public class AdvancedSearchRequestModified
        {
            [JsonProperty("start")]
            public DateTime Start { get; set; }

            [JsonProperty("end")]
            public DateTime End { get; set; }
        }
    }
}