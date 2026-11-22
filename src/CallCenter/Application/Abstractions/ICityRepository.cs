using CallCenter.Domain.Entities;

namespace CallCenter.Application.Abstractions;

public interface ICityRepository
{
    Task<IReadOnlyList<ServiceCity>> GetCitiesAsync(CancellationToken cancellationToken);
}
