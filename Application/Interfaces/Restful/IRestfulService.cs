using System.Net;

namespace Application.Interfaces.Restful;

public interface IRestfulService
{
    Task<(HttpStatusCode, string)> Get(string url, Dictionary<string, string>? headers = null);

    Task<(HttpStatusCode, Stream?)> GetAsStream(string url, Dictionary<string, string>? headers = null);

    Task<(HttpStatusCode, string)> Post(string url, HttpContent content, Dictionary<string, string>? headers = null);

    Task<(HttpStatusCode, string)> Post(string url, MultipartFormDataContent multipartContent,
        Dictionary<string, string>? headers = null);

    Task<(HttpStatusCode, string)> Post(string url, Dictionary<string, string> formData,
        Dictionary<string, string>? headers = null);

    Task<(HttpStatusCode, Stream)> PostAsStream(string url, MultipartFormDataContent multipartContent,
        Dictionary<string, string>? headers = null);

    Task<(HttpStatusCode, string)> Delete(string url, Dictionary<string, string>? headers = null);
}