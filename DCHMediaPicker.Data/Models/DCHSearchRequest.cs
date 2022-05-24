using Newtonsoft.Json;
using System.Collections.Generic;

namespace DCHMediaPicker.Data.Models
{
    public class DCHSearchRequest
    {
        public DCHSearchRequest()
        {
            Defaults = "DCHIntegrationsSearchConfiguration";
            Culture = "en-US";
            Skip = 0;
            Take = 48;
            View = "grid";
            Query = "";
            Filters = new List<DCHFilter>();
            Fulltext = new string[0];
            Sorting = new DCHSorting()
            {
                Field = "relevance",
                Asc = false
            };
        }

        [JsonProperty("defaults")]
        public string Defaults { get; set; }

        [JsonProperty("culture")]
        public string Culture { get; set; }

        [JsonProperty("skip")]
        public int Skip { get; set; }

        [JsonProperty("take")]
        public int Take { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("filters")]
        public List<DCHFilter> Filters { get; set; }

        [JsonProperty("fulltext")]
        public string[] Fulltext { get; set; }

        [JsonProperty("view")]
        public string View { get; set; }

        [JsonProperty("sorting")]
        public DCHSorting Sorting { get; set; }

        public class DCHSorting
        {
            [JsonProperty("field")]
            public string Field { get; set; }

            [JsonProperty("asc")]
            public bool Asc { get; set; }
        }

        public class DCHFilter
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("operator")]
            public string Operator { get; set; }

            [JsonProperty("values")]
            public string[] Values { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }
        }
    }
}