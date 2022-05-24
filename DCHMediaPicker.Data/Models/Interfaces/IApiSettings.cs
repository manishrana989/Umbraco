namespace DCHMediaPicker.Data.Models.Interfaces
{
    public interface IApiSettings
    {
        string Endpoint { get; set; }
        string ClientId { get; set; }
        string ClientSecret { get; set; }
        string CorrelationId { get; set; }
        string UserAgent { get; set; }
    }
}