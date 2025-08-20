using System.Text;
using Newtonsoft.Json;
using PeplinkSPTool.Models;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace PeplinkSPTool.Classes
{
    internal class PeplinkAPI
    {
        private readonly HttpClient _Client = new();
        private readonly string? _ClientSecret;
        private readonly string? _ClientID;
        private readonly string? _BaseURI;
        private string? _AuthToken;
        
        #region Initialisation
        public PeplinkAPI(IConfiguration configuration)
        {
            // Get API details from configuration
            var APIEndpoint = configuration.GetSection("PeplinkSPTool").GetValue<string>("Endpoint");
            _ClientID = configuration.GetSection("PeplinkSPTool").GetValue<string>("ClientID");
            _ClientSecret = configuration.GetSection("PeplinkSPTool").GetValue<string>("ClientSecret");
            if( string.IsNullOrWhiteSpace(APIEndpoint) || string.IsNullOrWhiteSpace(_ClientID) || string.IsNullOrWhiteSpace(_ClientSecret) )
            {
                throw new ArgumentException("Configuration for Peplink API is invalid. Please check the PeplinkSPTool section of the application configuration");
            }
            
            // Configure the web client
            _BaseURI = APIEndpoint;
            _Client.Timeout = TimeSpan.FromSeconds(10);
        }
        #endregion Initialisation
        
        #region Public Methods
        public async Task<PepOrgResponse[]> GetOrganisations()
        {
            _AuthToken ??= await _GetAuthToken();
            if( _AuthToken == null ) throw new Exception("Authentication Failed");
            
            _Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _AuthToken);
            var ResponseData = await _Client.SendAsync(new HttpRequestMessage
            {
                RequestUri = new Uri($"{_BaseURI}/rest/o"),
                Method = HttpMethod.Get
            });
            
            // Check for successful response
            if( ResponseData.IsSuccessStatusCode )
            {
                var Response = await ResponseData.Content.ReadAsStringAsync();
                var ServerResponse = JsonConvert.DeserializeObject<PepResponse<PepOrgResponse[]>>(Response);
                if( ServerResponse == null )
                {
                    throw new Exception("Server provided no response");
                }
                else if( ServerResponse.resp_code != "SUCCESS" )
                {
                    throw new Exception($"Server response {ServerResponse.resp_code}: Message: {ServerResponse.message}");
                }
                else if( ServerResponse.data == null )
                {
                    throw new Exception("Server returned no data in response");
                }
                
                return ServerResponse.data;
            }
            
            throw new Exception($"Response did not return an expected response code: {ResponseData.StatusCode}");
        }
        
        public async Task<PepGroupResponse[]> GetGroups(string OrganisationID)
        {
            _AuthToken ??= await _GetAuthToken();
            if( _AuthToken == null ) throw new Exception("Authentication Failed");
            
            _Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _AuthToken);
            var ResponseData = await _Client.SendAsync(new HttpRequestMessage
            {
                RequestUri = new Uri($"{_BaseURI}/rest/o/{OrganisationID}/g"),
                Method = HttpMethod.Get
            });
            
            // Check for successful response
            if( ResponseData.IsSuccessStatusCode )
            {
                var Response = await ResponseData.Content.ReadAsStringAsync();
                var ServerResponse = JsonConvert.DeserializeObject<PepResponse<PepGroupResponse[]>>(Response);
                if( ServerResponse == null )
                {
                    throw new Exception("Server provided no response");
                }
                else if( ServerResponse.resp_code != "SUCCESS" )
                {
                    throw new Exception($"Server response {ServerResponse.resp_code}: Message: {ServerResponse.message}");
                }
                else if( ServerResponse.data == null )
                {
                    throw new Exception("Server returned no data in response");
                }
                
                return ServerResponse.data;
            }
            
            throw new Exception($"Response did not return an expected response code: {ResponseData.StatusCode}");
        }
        
        public async Task<PepDeviceResponse[]> GetDevices(string OrganisationID, long GroupID)
        {
            _AuthToken ??= await _GetAuthToken();
            if( _AuthToken == null ) throw new Exception("Authentication Failed");
            
            _Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _AuthToken);
            var ResponseData = await _Client.SendAsync(new HttpRequestMessage
            {
                RequestUri = new Uri($"{_BaseURI}/rest/o/{OrganisationID}/g/{GroupID}/d"),
                Method = HttpMethod.Get
            });
            
            // Check for successful response
            if( ResponseData.IsSuccessStatusCode )
            {
                var Response = await ResponseData.Content.ReadAsStringAsync();
                var ServerResponse = JsonConvert.DeserializeObject<PepResponse<PepDeviceResponse[]>>(Response);
                if( ServerResponse == null )
                {
                    throw new Exception("Server provided no response");
                }
                else if( ServerResponse.resp_code != "SUCCESS" )
                {
                    throw new Exception($"Server response {ServerResponse.resp_code}: Message: {ServerResponse.message}");
                }
                else if( ServerResponse.data == null )
                {
                    throw new Exception("Server returned no data in response");
                }
                
                return ServerResponse.data;
            }
            
            throw new Exception($"Response did not return an expected response code: {ResponseData.StatusCode}");
        }
        
        public async Task<string> SetDeviceSPStatus(string OrganisationID, long GroupID, List<long> Devices, bool Active)
        {
            _AuthToken ??= await _GetAuthToken();
            if( _AuthToken == null ) throw new Exception("Authentication Failed");
            var PepSetRequest = new PepRequest<PepSpSetRequest>
            {
                data = new PepSpSetRequest
                {
                    active = Active,
                    device_ids = Devices.ToArray()
                }
            };
            
            _Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _AuthToken);
            var ResponseData = await _Client.SendAsync(new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(PepSetRequest), Encoding.UTF8, "application/json"),
                RequestUri = new Uri($"{_BaseURI}/rest/o/{OrganisationID}/g/{GroupID}/sp_default"),
                Method = HttpMethod.Post
            });

            // Check for successful response
            if( ResponseData.IsSuccessStatusCode )
            {
                var Response = await ResponseData.Content.ReadAsStringAsync();
                var ServerResponse = JsonConvert.DeserializeObject<PepResponse<PepSpCheckResponse>>(Response);
                if( ServerResponse == null )
                {
                    throw new Exception("Server provided no response");
                }
                else if( ServerResponse.resp_code != null && ServerResponse.resp_code != "SUCCESS" && ServerResponse.resp_code != "PENDING" )
                {
                    throw new Exception($"Server response {ServerResponse.resp_code}: Message: {ServerResponse.message}");
                }
                else if( ServerResponse.data?.ticket_id == null )
                {
                    throw new Exception("Server returned no data in response");
                }
                
                return ServerResponse.data.ticket_id;
            }
            
            throw new Exception($"Response did not return an expected response code: {ResponseData.StatusCode}");
        }
        #endregion Public Methods
        
        #region Private Methods
        private async Task<string?> _GetAuthToken()
        {
            if( _ClientID == null || _ClientSecret == null || Program.PeplinkChallengeCode == null ) return null;
            var FormParams = new Dictionary<string, string>
            {
                { "redirect_uri", $"{Program.Bind}/reply" },
                { "code", Program.PeplinkChallengeCode },
                { "grant_type", "authorization_code" },
                { "client_secret", _ClientSecret },
                { "client_id", _ClientID }
            };
            
            var ResponseData = await _Client.SendAsync(new HttpRequestMessage
            {
                RequestUri = new Uri($"{_BaseURI}/api/oauth2/token"),
                Content = new FormUrlEncodedContent(FormParams),
                Method = HttpMethod.Post
            });
            
            if( ResponseData.IsSuccessStatusCode )
            {
                var Response = await ResponseData.Content.ReadAsStringAsync();
                return (JsonConvert.DeserializeObject<PepAuthResponseModel>(Response))?.access_token;
            }
            
            throw new Exception("Unable to retrieve auth token from response");
        }
        #endregion Private Methods
    }
}