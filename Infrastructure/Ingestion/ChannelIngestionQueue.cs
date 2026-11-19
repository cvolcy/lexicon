using System.Threading.Channels;

namespace Lexicon.Infrastructure.Ingestion;

public sealed class ChannelIngestionQueue : IIngestionQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
    });

    public ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(documentId, cancellationToken);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
