using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace TranscribeWin.Services;

public class TranscribeClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private string _baseUrl;

    public TranscribeClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public void UpdateBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<string> TranscribeAsync(byte[] wavData, string language = "zh", bool punctuation = true)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(wavData);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "file", "recording.wav");

        if (!string.IsNullOrEmpty(language))
            content.Add(new StringContent(language), "language");

        content.Add(new StringContent(punctuation.ToString().ToLower()), "punctuation");
        content.Add(new StringContent("json"), "response_format");

        var response = await _httpClient.PostAsync($"{_baseUrl}/v1/audio/transcriptions", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("text", out var textProp))
                return textProp.GetString() ?? "";
        }
        catch { }

        return responseJson;
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/healthz");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
