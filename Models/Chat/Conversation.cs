namespace Lexicon.Models.Chat;

public class Conversation
{
    public const int MaxTitleLength = 200;
    public const string DefaultTitle = "New conversation";

    public Guid ConversationId { get; set; }

    public required string Title { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ConversationMessage> Messages { get; set; } = [];
}
