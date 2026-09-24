namespace AiAssistant.Domain.Entities;

/// <summary>Holds the turn history for a single conversation session.</summary>
public sealed class ChatSession
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    private readonly List<ChatTurn> _turns = [];
    public IReadOnlyList<ChatTurn> Turns => _turns.AsReadOnly();

    public void AddTurn(string role, string content) =>
        _turns.Add(new ChatTurn(role, content, DateTime.UtcNow));
}

public sealed record ChatTurn(string Role, string Content, DateTime Timestamp);
