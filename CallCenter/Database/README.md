# SQL Server persistence

Use `ConnectionStrings__MainConnection` in the process environment or .NET user secrets. Do not commit production connection strings. The connection factory is resolved only by SQL repositories, so features without persistence can run without a database.

Follow PwaApi1669's ADO.NET pattern (Microsoft.Data.SqlClient), without Entity Framework:

- Repository interfaces belong in `Application/Abstractions` and use domain entities or application DTOs; do not expose SqlConnection or SqlCommand to Application.
- Implementations belong in `Infrastructure/Persistence/Repositories` and receive `SqlConnectionFactory` through constructor injection.
- Create a connection per operation with `connections.Create()`. Dispose connections, commands and readers with `await using`.
- Direct SQL uses `CommandType.Text`, constant SQL and typed `SqlParameter` values. Never interpolate user input into SQL.
- Stored procedures use a fixed procedure name and `CommandType.StoredProcedure`, also with typed parameters. Keep versioned SQL scripts and procedure documentation in this folder when actual business features are added.
- Set a finite command timeout (30 seconds as in the reference) and pass CancellationToken to OpenAsync, ExecuteReaderAsync and ReadAsync.
- Register each repository and service with AddScoped in Program.cs.

Example command construction inside a repository:

```csharp
await using var connection = connections.Create();
await using var command = new SqlCommand(
    "SELECT Id FROM dbo.Example WHERE Id = @id", connection)
{
    CommandType = CommandType.Text,
    CommandTimeout = 30
};
command.Parameters.Add("@id", SqlDbType.Int).Value = id;
await connection.OpenAsync(cancellationToken);
await using var reader = await command.ExecuteReaderAsync(cancellationToken);
```

For an existing stored procedure, replace the command text with its schema-qualified name and use `CommandType.StoredProcedure`. `dbo.Example` above is documentation only, not an assumed application table. No schema or stored procedure has been invented or applied during this restructuring.
