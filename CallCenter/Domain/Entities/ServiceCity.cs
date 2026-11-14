namespace CallCenter.Domain.Entities;
public sealed record ServiceCity(int Id, string Name, string? Province, bool IsCovered, IReadOnlyList<string> Aliases);
