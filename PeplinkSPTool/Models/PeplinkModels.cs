namespace PeplinkSPTool.Models
{
    public class PepResponse<T>
    {
		public string? resp_code { get; set; }
		public string? caller_ref { get; set; }
		public string? server_ref { get; set; }
		public string? message { get; set; }
		public T? data { get; set; }
    }
    
    public class PepRequest<T>
    {
	    public required T data { get; set; }
    }
    
    public class PepOrgResponse
    {
	    public string? id { get; set; }
	    public string? name { get; set; }
	    public bool? primary { get; set; }
	    public string? status { get; set; }
	    public long? migrateStatus { get; set; }
    }
    
    public class PepGroupResponse
    {
	    public long? id { get; set; }
	    public string? name { get; set; }
	    public long? online_device_count { get; set; }
	    public long? offline_device_count { get; set; }
    }
    
    public class PepSpCheckResponse
    {
	    public string? ticket_id { get; set; }
	    public PepDeviceResponse[]? devices { get; set; }
    }
    
    public class PepSpSetRequest
    {
	    public bool active { get; set; }
	    public long[] device_ids { get; set; } = [];
    }
    
    public class PepDeviceResponse
    {
	    public long? id { get; set; }
	    public string? sn { get; set; }
	    public string? name { get; set; }
	    public bool sp_default { get; set; }
    }
    
    public class PepAuthResponseModel
    {
        public string? access_token { get; set; }
        public string? token_type { get; set; }
        public string? refresh_token { get; set; }
        public long? expires_in { get; set; }
    }
}