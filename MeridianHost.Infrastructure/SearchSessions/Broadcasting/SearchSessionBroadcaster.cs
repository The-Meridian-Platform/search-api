using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using MeridianHost.Application.SearchSessions.Execute.Broadcasting.Contracts;
using MeridianHost.Application.SearchSessions.Execute.Broadcasting.Contracts.Additional;
using MeridianHost.Application.SearchSessions.Execute.Broadcasting.Repositories;

namespace MeridianHost.Infrastructure.SearchSessions.Broadcasting;

public class SearchSessionBroadcaster : ISearchSessionBroadcaster
{
    private readonly ConcurrentDictionary<Guid, List<Channel<SearchSessionEvent>>> _subscribers = new();
    private readonly object _lock = new();
    
    public ValueTask PublishAsync(SearchSessionEvent searchSessionEvent, CancellationToken cancellationToken = default)
    {
        if (_subscribers.TryGetValue(searchSessionEvent.SessionId, out var channels))
        {
            List<Channel<SearchSessionEvent>> targets;
            
            lock (_lock) targets = [..channels];

            foreach (var channel in targets)
            {
                channel.Writer.TryWrite(searchSessionEvent);

                if (searchSessionEvent is SessionFinishedEvent)
                    channel.Writer.TryComplete();
            }
            
            if (searchSessionEvent is SessionFinishedEvent)
                _subscribers.TryRemove(searchSessionEvent.SessionId, out _);
        }

        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<SearchSessionEvent> SubscribeAsync(
        Guid sessionId, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<SearchSessionEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        lock (_lock)
        {
            var list = _subscribers.GetOrAdd(sessionId, _ => []);
            list.Add(channel);
        }

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
                yield return evt;
        }

        finally
        {
            lock (_lock)
            {
                if (_subscribers.TryGetValue(sessionId, out var list))
                {
                    list.Remove(channel);
                    if (list.Count == 0)
                        _subscribers.TryRemove(sessionId, out _);
                }
            }
        }
    }
}