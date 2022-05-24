using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DCHMediaPicker.Data.Models
{
    public class DCHPublicLinks
    {
        [JsonProperty("properties")]
        public JObject Properties { get; set; }
    }
}