USE master;
GO

SELECT
    name,
    state_desc
FROM sys.databases
ORDER BY name;
GO

SELECT DB_ID('ParkyDB') AS ParkyDatabaseId;