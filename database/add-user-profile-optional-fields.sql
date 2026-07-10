SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH(N'[dbo].[Users]', N'Photo') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [Photo] NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH(N'[dbo].[Users]', N'Gender') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [Gender] NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH(N'[dbo].[Users]', N'DateOfBirth') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [DateOfBirth] DATE NULL;
END
GO

IF COL_LENGTH(N'[dbo].[Users]', N'NID') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [NID] NVARCHAR(50) NULL;
END
GO
