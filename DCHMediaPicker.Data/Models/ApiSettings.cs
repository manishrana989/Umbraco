using DCHMediaPicker.Data.Models.Interfaces;

namespace DCHMediaPicker.Data.Models
{
    public class ApiSettings : IApiSettings
    {
        public string Endpoint { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string CorrelationId { get; set; }
        public string UserAgent { get; set; }
    }
}