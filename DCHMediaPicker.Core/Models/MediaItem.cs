using GlobalCMSUmbraco.Core.Json.JsonConverters;
using Newtonsoft.Json;
using System;

namespace DCHMediaPicker.Core.Models
{
    public class MediaItem
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string Thumbnail { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public DateTime? Expiry { get; set; }
        public bool Resolved { get; set; }
        public string AltText { get; set; }

        [JsonProperty("imageOptions")]
        [JsonConverter(typeof(KeepAsJsonConverter))]
        public string OptionsJson { get; set; }
    }
}