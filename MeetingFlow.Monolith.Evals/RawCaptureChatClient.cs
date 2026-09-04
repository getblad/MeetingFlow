using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace MeetingFlow.Monolith.Evals;

/// <summary>
/// Passes every call through to the real client while keeping the raw text of the last reply.
/// The service under test validates and then discards the raw payload, so without this the eval
/// could never show what the model actually returned when validation rejected it.
/// </summary>
public sealed class RawCaptureChatClient(IChatClient inner) : IChatClient
{
    public string? LastRawText { get; private set; }

    public ChatOptions? LastOptions { get; private set; }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        LastOptions = options;
        LastRawText = null;

        var response = await inner.GetResponseAsync(messages, options, cancellationToken);
        LastRawText = response.Text;
        return response;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LastOptions = options;

        await foreach (var update in inner
            .GetStreamingResponseAsync(messages, options, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        inner.GetService(serviceType, serviceKey);

    // The inner client is owned by the caller and shared across cases, so it is not disposed here.
    public void Dispose()
    {
    }
}
