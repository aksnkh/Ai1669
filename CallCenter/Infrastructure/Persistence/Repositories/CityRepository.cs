using System.Data;
using CallCenter.Application.Abstractions;
using CallCenter.Domain.Entities;
using Microsoft.Data.SqlClient;
namespace CallCenter.Infrastructure.Persistence.Repositories;
public sealed class CityRepository(SqlConnectionFactory connections) : ICityRepository
{
    public async Task<IReadOnlyList<ServiceCity>> GetCitiesAsync(CancellationToken cancellationToken)
    {
        await using var connection = connections.Create();
        await using var command = new SqlCommand("dbo.ServiceCities_Select", connection)
        { CommandType = CommandType.StoredProcedure, CommandTimeout = 30 };
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new Dictionary<int, (string Name, string? Province, bool Covered, List<string> Aliases)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32(0);
            if (!rows.TryGetValue(id, out var city))
            {
                city = (reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetBoolean(3), new());
                rows.Add(id, city);
            }
            if (!reader.IsDBNull(4)) city.Aliases.Add(reader.GetString(4));
        }
        return rows.Select(row => new ServiceCity(row.Key, row.Value.Name, row.Value.Province,
            row.Value.Covered, row.Value.Aliases)).ToArray();
    }
}
