using CallCenter.Application.Abstractions;
using CallCenter.Domain.Entities;

namespace CallCenter.Infrastructure.Repositories;

public sealed class InMemoryCityRepository : ICityRepository
{
    private static readonly IReadOnlyList<ServiceCity> Cities = Array.AsReadOnly(new ServiceCity[]
    {
        new(1, "بندرعباس", true),
        new(2, "تهران", true),
        new(3, "شیراز", false)
    });

    public Task<IReadOnlyList<ServiceCity>> GetCitiesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Cities);
    }
}
