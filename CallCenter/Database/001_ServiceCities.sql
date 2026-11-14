-- Run in the intended application database with a tool supporting GO (SSMS/sqlcmd).
-- Repeatable: preserves existing city data and coverage decisions.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.ServiceCities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ServiceCities
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ServiceCities PRIMARY KEY,
        Name nvarchar(100) NOT NULL,
        Province nvarchar(100) NULL,
        IsCovered bit NOT NULL CONSTRAINT DF_ServiceCities_IsCovered DEFAULT (0),
        CONSTRAINT CK_ServiceCities_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
    );
END;
IF OBJECT_ID(N'dbo.ServiceCityAliases', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ServiceCityAliases
    (
        CityId int NOT NULL,
        Alias nvarchar(100) NOT NULL,
        CONSTRAINT PK_ServiceCityAliases PRIMARY KEY (CityId, Alias),
        CONSTRAINT FK_ServiceCityAliases_City FOREIGN KEY (CityId) REFERENCES dbo.ServiceCities(Id),
        CONSTRAINT CK_ServiceCityAliases_Alias CHECK (LEN(LTRIM(RTRIM(Alias))) > 0)
    );
END;
COMMIT;
GO
CREATE OR ALTER PROCEDURE dbo.ServiceCities_Select
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Id, c.Name, c.Province, c.IsCovered, a.Alias
    FROM dbo.ServiceCities AS c
    LEFT JOIN dbo.ServiceCityAliases AS a ON a.CityId = c.Id
    ORDER BY c.Id;
END;
GO
-- No cities are seeded: coverage is a business decision.
-- Example only; replace values and execute separately:
-- INSERT dbo.ServiceCities(Name, Province, IsCovered) VALUES (N'تهران', N'تهران', 1);
-- INSERT dbo.ServiceCities(Name, Province, IsCovered) VALUES (N'شیراز', N'فارس', 0);
-- INSERT dbo.ServiceCityAliases(CityId, Alias) VALUES (<actual city id>, N'<alternative spelling>');
-- Runtime principal needs EXECUTE on dbo.ServiceCities_Select, not schema modification rights.
