using System.Net;
using System.Text.Json;

namespace Gateway.Clients;

public sealed record DownstreamResult<T>(
    HttpStatusCode StatusCode,
    T? Value,
    JsonElement? Error)
{
    public bool IsSuccess =>
        (int)StatusCode is >= 200 and <= 299;

    public static async Task<DownstreamResult<T>> FromResponseAsync(
        HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var value = await response.Content.ReadFromJsonAsync<T>();
            return new DownstreamResult<T>(response.StatusCode, value, null);
        }

        JsonElement? error = null;
        if (response.Content.Headers.ContentLength is not 0)
        {
            error = await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        return new DownstreamResult<T>(response.StatusCode, default, error);
    }
}

public sealed record DownstreamStatus(HttpStatusCode StatusCode, JsonElement? Error)
{
    public bool IsSuccess => (int)StatusCode is >= 200 and <= 299;

    public static async Task<DownstreamStatus> FromResponseAsync(
        HttpResponseMessage response)
    {
        JsonElement? error = null;
        if (!response.IsSuccessStatusCode && response.Content.Headers.ContentLength is not 0)
        {
            error = await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        return new DownstreamStatus(response.StatusCode, error);
    }
}
