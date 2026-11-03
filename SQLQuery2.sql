USE ParkyDB;
GO

SELECT
    Id,
    ActionUrl,
    LEN(ActionUrl) AS ActionUrlLength,
    DATALENGTH(ActionUrl) AS ActionUrlBytes
FROM Notifications
WHERE ActionUrl IS NOT NULL
  AND DATALENGTH(ActionUrl) > 1000;
GO