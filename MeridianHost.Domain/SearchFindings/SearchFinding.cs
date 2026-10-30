using CSharpFunctionalExtensions;

namespace MeridianHost.Domain.SearchFindings;

public class SearchFinding
{
    public Guid Id { get; private set; }
    public Guid SearchSessionId { get; private set; }
    public string ProviderKey { get; private set; }
    public string Type { get; private set; }
    public string DataJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SearchFinding(Guid id, Guid searchSessionId, string providerKey, string type, string dataJson, DateTimeOffset createdAt)
    {
        Id = id;
        SearchSessionId = searchSessionId;
        ProviderKey = providerKey;
        Type = type;
        DataJson = dataJson;
        CreatedAt = createdAt;
    }

    public static Result<SearchFinding> Create(Guid searchSessionId, string providerKey, string type, string dataJson)
        => Result.Success(new SearchFinding(Guid.NewGuid(), searchSessionId, providerKey, type, dataJson, DateTimeOffset.UtcNow));
    
    public static SearchFinding Load(Guid id, Guid searchSessionId, string providerKey, string type, string dataJson, DateTimeOffset createdAt)
        => new(id, searchSessionId, providerKey, type, dataJson, createdAt);
}