using System.Net;
using Application.Interfaces.Restful;

namespace Infrastructure.Restful;

public class RestfulService(IHttpClientFactory clientFactory) : IRestfulService
{
    public async Task<(HttpStatusCode, string)> Get(string url, Dictionary<string, string>? headers = null)
    {
        var client = CreateClient(headers);
        var response = await client.GetAsync(url);
        var results = await response.Content.ReadAsStringAsync();

        return (response.StatusCode, results);
    }

    public async Task<(HttpStatusCode, Stream?)> GetAsStream(string url, Dictionary<string, string>? headers = null)
    {
        var client = CreateClient(headers);
        var response = await client.GetAsync(url);

        Stream? stream = null;
        if (response.StatusCode == HttpStatusCode.OK) stream = await response.Content.ReadAsStreamAsync();

        return (response.StatusCode, stream);
    }

    public async Task<(HttpStatusCode, string)> Post(string url, HttpContent content,
        Dictionary<string, string>? headers = null)
    {
        var client = CreateClient(headers);
        var response = await client.PostAsync(url, content);
        var results = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, results);
    }

    public async Task<(HttpStatusCode, string)> Post(string url, MultipartFormDataContent multipartContent,
        Dictionary<string, string>? headers = null)
    {
        var client = CreateClient(headers);
        var response = await client.PostAsync(url, multipartContent);
        var results = await response.Content.ReadAsStringAsync();

        return (response.StatusCode, results);
    }

    public async Task<(HttpStatusCode, string)> Post(string url, Dictionary<string, string> formData,
        Dictionary<string, string>? headers = null)
    {
        var client = CreateClient(headers);
        var formContent = new FormUrlEncodedContent(formData);
        var response = await client.PostAsync(url, formContent);
        var results = await response.Content.ReadAsStringAsync();

        return (response.StatusCode, results);
    }

    public async Task<(HttpStatusCode, Stream)> PostAsStream(string url, MultipartFormDataContent multipartContent,
        Dictionary<string, string>? headers = null)
    {
        var client = CreateClient(headers);
        var response = await client.PostAsync(url, multipartContent);
        var results = await response.Content.ReadAsStreamAsync();

        return (response.StatusCode, results);
    }

    public async Task<(HttpStatusCode, string)> Delete(string url, Dictionary<string, string>? headers = null)
    {
        var client = CreateClient(headers);
        var response = await client.DeleteAsync(url);
        var results = await response.Content.ReadAsStringAsync();

        return (response.StatusCode, results);
    }

    private HttpClient CreateClient(Dictionary<string, string>? headers = null)
    {
        var client = clientFactory.CreateClient();
        if (headers != null && headers.Count > 0)
            foreach (var item in headers)
                client.DefaultRequestHeaders.Add(item.Key, item.Value);

        return client;
    }
}