# Ai1669

ASP.NET Core 8 API. The folder layout follows `shetabKhadamat/PwaApi1669`: Clean Architecture layers within one web project.

```text
CallCenter/
  Controllers/                         HTTP endpoints
  Application/
    Abstractions/                      Repository and external-service contracts
    Common/                            Shared response contracts
    Features/<Feature>/                Services, service interfaces and DTOs
  Domain/Entities/                      Domain models
  Infrastructure/Persistence/
    SqlConnectionFactory.cs            SQL Server connection creation
    Repositories/                      ADO.NET SQL and stored procedure implementations
  Middlewares/                         Central exception handling
  Database/                            Database scripts and documentation
```

Controllers call Application services; services depend on repository interfaces in Application; Infrastructure implements those interfaces. Domain contains no infrastructure dependencies. This matches the reference's folder-based separation, rather than separate class-library projects.

```sh
dotnet restore Ai1669.sln
dotnet build Ai1669.sln --no-restore
dotnet run --project CallCenter --no-launch-profile --urls http://127.0.0.1:5028
```

`GET /weatherforecast` retains the existing five-item JSON response and delegates forecast generation to its Application service. It does not require SQL Server. The shared Response<T> contract and exception middleware follow the reference; the existing successful endpoint response is unchanged.

For database features set `ConnectionStrings__MainConnection` securely in the process environment. See `CallCenter/Database/README.md` for the direct SQL and stored procedure conventions. No database credentials, database-specific business endpoints, JWT settings, or SMS configuration are copied from the reference project.

## Voice coverage

`POST /api/voice-coverage` accepts multipart audio and returns city matches and coverage JSON. See [setup and Swagger guide](CallCenter/Database/VoiceCoverage.md) and run [001_ServiceCities.sql](CallCenter/Database/001_ServiceCities.sql) before using the database feature.
