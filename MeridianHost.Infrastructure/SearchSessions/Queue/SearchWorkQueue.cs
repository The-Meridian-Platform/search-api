using System.Threading.Channels;
using MeridianHost.Application.SearchSessions.Execute.Queue;

namespace MeridianHost.Infrastructure.SearchSessions.Queue;

public class SearchWorkQueue : ISearchWorkQueue
{
    private readonly Channel<Guid> _channel;

    public SearchWorkQueue()
    {
        var options = new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        
        _channel = Channel.CreateBounded<Guid>(options);
    }
    
    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) 
        => _channel.Reader.ReadAllAsync(cancellationToken);

    public ValueTask EnqueueAsync(Guid searchSessionId, CancellationToken cancellationToken) 
        => _channel.Writer.WriteAsync(searchSessionId, cancellationToken);
}