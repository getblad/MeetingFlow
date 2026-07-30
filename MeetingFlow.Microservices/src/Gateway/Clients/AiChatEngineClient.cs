using AiChatEngine.Contracts;
using System.Net.Http.Json;

namespace Gateway.Clients;

public class AiChatEngineClient(HttpClient http)
{
    public async Task<DownstreamResult<ChatResult>> ChatAsync(ChatRequest request)
    {
        var response = await http.PostAsJsonAsync("/chat", request);
        return await DownstreamResult<ChatResult>.FromResponseAsync(response);
    }
}
