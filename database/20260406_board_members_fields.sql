USE GeoURPDb;
GO

IF COL_LENGTH('dbo.BoardMembers', 'Code') IS NULL
BEGIN
    ALTER TABLE dbo.BoardMembers
    ADD Code NVARCHAR(50) NULL;
END;
GO

IF COL_LENGTH('dbo.BoardMembers', 'Birthday') IS NULL
BEGIN
    ALTER TABLE dbo.BoardMembers
    ADD Birthday NVARCHAR(5) NULL;
END;
GO
