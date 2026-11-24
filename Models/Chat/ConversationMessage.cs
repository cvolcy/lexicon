namespace Lexicon.Models.Chat;

public class ConversationMessage
{
    public Guid MessageId { get; set; }

    public Guid ConversationId { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public ConversationMessageRole Role { get; set; }

    public required string Content { get; set; }

    public string? CitationsJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
