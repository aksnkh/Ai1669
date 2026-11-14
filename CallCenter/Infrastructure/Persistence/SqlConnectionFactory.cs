using Microsoft.Data.SqlClient;
namespace CallCenter.Infrastructure.Persistence;
public sealed class SqlConnectionFactory(string connectionString)
{
    public SqlConnection Create()
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'MainConnection' is missing.");
        return new SqlConnection(connectionString);
    }
}