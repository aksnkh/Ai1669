namespace CallCenter.Application.Common;
public class Response<T>
{
    public int statusCode { get; set; }
    public string message { get; set; } = string.Empty;
    public string title { get; set; } = string.Empty;
    public MessageLevel messageLevel { get; set; }
    public string informationLink { get; set; } = string.Empty;
    public string devMessage { get; set; } = string.Empty;
    public string parameters { get; set; } = string.Empty;
    public T? payload { get; set; }
    public Response() { }
    public Response(T payload) { this.payload = payload; }
}
public enum MessageLevel { Admin = 1, Cif = 2, Tech = 3 }