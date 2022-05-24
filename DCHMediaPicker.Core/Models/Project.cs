using Newtonsoft.Json;

namespace DCHMediaPicker.Core.Models
{
    public class Project
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("projectName")]
        public string ProjectName { get; set; }
    }
}