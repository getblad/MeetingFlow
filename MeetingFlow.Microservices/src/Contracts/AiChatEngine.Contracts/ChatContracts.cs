namespace AiChatEngine.Contracts;

public sealed record ChatMessageDto(string Role, string Content);

public sealed record ChatRequest(
    string Message,
    IReadOnlyList<ChatMessageDto>? History);

public sealed record ChatActionDto(
    string Type,
    IReadOnlyDictionary<string, string> Parameters);

public sealed record ChatResult(
    string Reply,
    ChatActionDto? Action,
    object? Data);
