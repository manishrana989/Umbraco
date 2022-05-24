using DCHMediaPicker.Data.Models;
using DCHMediaPicker.Data.Models.Interfaces;
using DCHMediaPicker.Data.Repositories.Interfaces;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Logging;

namespace DCHMediaPicker.Data.Repositories
{
    public class DCHMediaRepository : IDCHMediaRepository
    {
        private readonly IApiSettings _apiSettings;
        private readonly ILogger _logger;
        private HttpClient _client;
        public DCHMediaRepository(IApiSettings apiSettings, ILogger logger)
        {
            _apiSettings = apiSettings;
            _logger = logger;
            _client = new HttpClient
            {
                BaseAddress = new Uri(_apiSettings.Endpoint)
            };
            _client.DefaultRequestHeaders.Add(Constants.ApiClientIdHeader, _apiSettings.ClientId);
            _client.DefaultRequestHeaders.Add(Constants.ApiClientSecretHeader, _apiSettings.ClientSecret);
            _client.DefaultRequestHeaders.Add(Constants.ApiCorrelationId, _apiSettings.CorrelationId);
            _client.DefaultRequestHeaders.Add(Constants.ApiUserAgent, _apiSettings.UserAgent);
        }

        public void OverrideSettings(string endpoint, string clientId, string clientSecret)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(endpoint)
            };
            _client.DefaultRequestHeaders.Add(Constants.ApiClientIdHeader, clientId);
            _client.DefaultRequestHeaders.Add(Constants.ApiClientSecretHeader, clientSecret);
            _client.DefaultRequestHeaders.Add(Constants.ApiCorrelationId, _apiSettings.CorrelationId);
            _client.DefaultRequestHeaders.Add(Constants.ApiUserAgent, _apiSettings.UserAgent);
        }

        public async Task<DCHResponse> SearchAsync(DCHSearchRequest searchRequest)
        {
            var urlPath = Constants.ApiSearchPath;
            var content = new StringContent(JsonConvert.SerializeObject(searchRequest, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            }), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(urlPath, content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<DCHResponse>();
            }

            _logger.Warn<DCHMediaRepository>("Failed status code {statusCode} from DCH API", response.StatusCode);
            return await Task.FromResult(new DCHResponse());
        }

        public async Task<DCHPublicLinks> GetPublicLinksAsync(int id)
        {
            var urlPath = string.Concat(Constants.ApiPublicLinksPath, id);
            var response = await _client.GetAsync(urlPath);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<DCHPublicLinks>();
            }

            _logger.Warn<DCHMediaRepository>("Failed status code {statusCode} from DCH API", response.StatusCode);
            return await Task.FromResult(new DCHPublicLinks());
        }

        public async Task<DCHMediaItem> GetAssetByIdAsync(string id)
        {
            var urlPath = Constants.ApiAssetIdPath;
            var response = await _client.GetAsync(string.Concat(urlPath, id));

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<DCHMediaItem>();
            }

            _logger.Warn<DCHMediaRepository>("Failed status code {statusCode} from DCH API", response.StatusCode);
            return await Task.FromResult<DCHMediaItem>(null);
        }
    }
}