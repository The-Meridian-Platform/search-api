using MeridianHost.Domain.Enums;
using MeridianHost.Domain.Models;

namespace MeridianHost.Persistence.Entities;

public sealed class SearchTargetEntity
{
    public Guid Id { get; set; }
    public string Value { get; set; }
    public SearchTargetType TargetType { get; set; }
}