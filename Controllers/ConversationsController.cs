using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lexicon.Models;
using Lexicon.Models.Chat;
using Lexicon.Services.Chat;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Lexicon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationsController(
    IChatService chatService,
    IOptions<RagChatOptions> chatOptions) : ControllerBase
{
    private const int MaxTitleLength = 200;

    private static readonly JsonSerializerOptions SseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [HttpGet]
    [ProducesResponseType<CollectionResponse<ConversationSummaryViewModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var conversations = await chatService.ListConversationsAsync(cancellationToken);
        return Ok(new CollectionResponse<ConversationSummaryViewModel>(conversations));
    }

    [HttpPost]
    [ProducesResponseType<ConversationSummaryViewModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateConversationRequest? request,
        CancellationToken cancellationToken)
    {
        var trimmedTitle = request?.Title?.Trim();
        if (trimmedTitle is { Length: > MaxTitleLength })
        {
            ModelState.AddModelError(
                nameof(request.Title),
                $"Conversation title must be at most {MaxTitleLength} characters.");
            return ValidationProblem(ModelState);
        }

        var created = await chatService.CreateConversationAsync(trimmedTitle, cancellationToken);
        return CreatedAtAction(
            nameof(Get),
            new { conversationId = created.ConversationId },
            created);
    }

    [HttpGet("{conversationId:guid}")]
    [ProducesResponseType<ConversationDetailViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await chatService.GetConversationAsync(conversationId, cancellationToken);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpDelete("{conversationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid conversationId, CancellationToken cancellationToken)
    {
        var deleted = await chatService.DeleteConversationAsync(conversationId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{conversationId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var trimmedContent = request.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedContent))
        {
            ModelState.AddModelError(nameof(request.Content), "Message content cannot be empty or whitespace.");
        }
        else if (trimmedContent.Length > chatOptions.Value.MaxMessageLength)
        {
            ModelState.AddModelError(
                nameof(request.Content),
                $"Message content must be at most {chatOptions.Value.MaxMessageLength} characters.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!await chatService.ConversationExistsAsync(conversationId, cancellationToken))
        {
            return NotFound();
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        try
        {
            await foreach (var evt in chatService.StreamReplyAsync(
                conversationId,
                trimmedContent,
                request.DocumentIds,
                cancellationToken))
            {
                var (eventName, payloadJson) = SerializeSseEvent(evt);
                await Response.WriteAsync($"event: {eventName}\ndata: {payloadJson}\n\n", Encoding.UTF8, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client disconnected or aborted the request.
        }

        return new EmptyResult();
    }

    private static (string EventName, string PayloadJson) SerializeSseEvent(ChatStreamEvent evt) => evt switch
    {
        CitationsChatStreamEvent citations => (
            "citations",
            JsonSerializer.Serialize(citations.Citations, SseJsonOptions)),
        TokenChatStreamEvent token => (
            "token",
            JsonSerializer.Serialize(new { content = token.Content }, SseJsonOptions)),
        DoneChatStreamEvent done => (
            "done",
            JsonSerializer.Serialize(done.Message, SseJsonOptions)),
        ErrorChatStreamEvent error => (
            "error",
            JsonSerializer.Serialize(new { message = error.Message }, SseJsonOptions)),
        _ => throw new InvalidOperationException($"Unsupported SSE stream event type: {evt.GetType().Name}"),
    };
}
