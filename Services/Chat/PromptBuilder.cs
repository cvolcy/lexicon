using System.Text;
using Lexicon.Models.Chat;
using Lexicon.Services.Retrieval;
using Microsoft.Extensions.Options;
using AiChatMessage = Microsoft.Extensions.AI.ChatMessage;
using AiChatRole = Microsoft.Extensions.AI.ChatRole;

namespace Lexicon.Services.Chat;

public sealed class PromptBuilder(IOptions<RagChatOptions> options) : IPromptBuilder
{
    private const string SystemPrompt = """
        You are Lexicon, a helpful local-first document assistant.
        Answer the user's question using ONLY the facts provided in the numbered context passages.

        Rules:
        - Cite supporting statements inline using bracketed numbers like [1] or [2] that match the numbered passages.
        - Never cite passage numbers that do not exist, and never invent facts, file names, or details outside the provided passages.
        - If the passages do not contain enough information to answer, clearly state what is missing or say that you don't know based on the indexed documents.
        - Format responses in clean Markdown (lists, code blocks, or tables when helpful).
        """;

    public PromptBuildResult Build(
        string userQuestion,
        IReadOnlyList<ConversationMessage> priorHistory,
        IReadOnlyList<RetrievedChunk> retrievedChunks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userQuestion);
        ArgumentNullException.ThrowIfNull(priorHistory);
        ArgumentNullException.ThrowIfNull(retrievedChunks);

        var opts = options.Value;
        var citations = SelectContextChunks(retrievedChunks, opts.MaxContextTokens);
        var messages = new List<AiChatMessage>(2 + opts.MaxHistoryMessages)
        {
            new(AiChatRole.System, SystemPrompt),
        };

        foreach (var historyMessage in SelectHistoryMessages(priorHistory, opts.MaxHistoryMessages, opts.MaxHistoryTokens))
        {
            var role = historyMessage.Role == ConversationMessageRole.User
                ? AiChatRole.User
                : AiChatRole.Assistant;
            messages.Add(new AiChatMessage(role, historyMessage.Content));
        }

        messages.Add(new AiChatMessage(AiChatRole.User, FormatUserTurn(userQuestion.Trim(), citations)));

        return new PromptBuildResult(messages, citations);
    }

    private static List<CitationViewModel> SelectContextChunks(
        IReadOnlyList<RetrievedChunk> retrievedChunks,
        int maxContextTokens)
    {
        var citations = new List<CitationViewModel>(retrievedChunks.Count);
        var usedTokens = 0;

        foreach (var chunk in retrievedChunks)
        {
            var chunkTokens = TokenEstimator.Estimate(chunk.Text);
            if (citations.Count > 0 && usedTokens + chunkTokens > maxContextTokens)
            {
                break;
            }

            citations.Add(CitationViewModel.From(citations.Count + 1, chunk));
            usedTokens += chunkTokens;
        }

        return citations;
    }

    private static List<ConversationMessage> SelectHistoryMessages(
        IReadOnlyList<ConversationMessage> priorHistory,
        int maxHistoryMessages,
        int maxHistoryTokens)
    {
        if (priorHistory.Count == 0 || maxHistoryMessages <= 0 || maxHistoryTokens <= 0)
        {
            return [];
        }

        var selected = new List<ConversationMessage>(Math.Min(priorHistory.Count, maxHistoryMessages));
        var usedTokens = 0;

        for (var i = priorHistory.Count - 1; i >= 0 && selected.Count < maxHistoryMessages; i--)
        {
            var message = priorHistory[i];
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                continue;
            }

            var messageTokens = TokenEstimator.Estimate(message.Content);
            if (selected.Count > 0 && usedTokens + messageTokens > maxHistoryTokens)
            {
                break;
            }

            selected.Add(message);
            usedTokens += messageTokens;
        }

        selected.Reverse();
        return selected;
    }

    private static string FormatUserTurn(string userQuestion, IReadOnlyList<CitationViewModel> citations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Context passages:");

        if (citations.Count == 0)
        {
            builder.AppendLine("(No relevant context passages were retrieved.)");
        }
        else
        {
            for (var i = 0; i < citations.Count; i++)
            {
                if (i > 0)
                {
                    builder.AppendLine();
                }

                var citation = citations[i];
                builder.Append('[').Append(citation.Index).Append("] Source: ").Append(citation.DocumentFileName);

                var locationParts = FormatLocation(citation.PageNumber, citation.Section);
                if (locationParts is not null)
                {
                    builder.Append(" (").Append(locationParts).Append(')');
                }

                builder.AppendLine();
                builder.AppendLine(citation.Text.Trim());
            }
        }

        builder.AppendLine();
        builder.AppendLine("Question:");
        builder.Append(userQuestion);
        return builder.ToString();
    }

    private static string? FormatLocation(int? pageNumber, string? section)
    {
        var hasPage = pageNumber.HasValue;
        var hasSection = !string.IsNullOrWhiteSpace(section);

        return (hasPage, hasSection) switch
        {
            (true, true) => $"Page {pageNumber}, Section: {section!.Trim()}",
            (true, false) => $"Page {pageNumber}",
            (false, true) => $"Section: {section!.Trim()}",
            _ => null,
        };
    }
}
