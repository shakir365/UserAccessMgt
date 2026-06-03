IF OBJECT_ID(N'[dbo].[Holidays]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Holidays]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Holidays] PRIMARY KEY,
        [HolidayName] NVARCHAR(150) NOT NULL,
        [HolidayDate] DATETIME2 NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Holidays_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Holidays_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL
    );
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Holidays_InstituteId_HolidayDate' AND [object_id] = OBJECT_ID(N'[dbo].[Holidays]'))
    DROP INDEX [IX_Holidays_InstituteId_HolidayDate] ON [dbo].[Holidays];
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Holidays_Institutes_InstituteId')
    ALTER TABLE [dbo].[Holidays] DROP CONSTRAINT [FK_Holidays_Institutes_InstituteId];
GO

IF COL_LENGTH(N'[dbo].[Holidays]', N'InstituteId') IS NOT NULL
    ALTER TABLE [dbo].[Holidays] DROP COLUMN [InstituteId];
GO

;WITH DuplicateRows AS
(
    SELECT [Id],
           ROW_NUMBER() OVER (PARTITION BY [HolidayDate] ORDER BY [Id]) AS [RowNumber]
    FROM [dbo].[Holidays]
)
DELETE FROM DuplicateRows WHERE [RowNumber] > 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Holidays_HolidayDate' AND [object_id] = OBJECT_ID(N'[dbo].[Holidays]'))
    CREATE UNIQUE INDEX [IX_Holidays_HolidayDate] ON [dbo].[Holidays] ([HolidayDate]);
GO

IF OBJECT_ID(N'[dbo].[Weekends]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Weekends]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Weekends] PRIMARY KEY,
        [DayOfWeek] INT NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Weekends_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Weekends_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [CK_Weekends_DayOfWeek] CHECK ([DayOfWeek] BETWEEN 0 AND 6)
    );
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Weekends_InstituteId_DayOfWeek' AND [object_id] = OBJECT_ID(N'[dbo].[Weekends]'))
    DROP INDEX [IX_Weekends_InstituteId_DayOfWeek] ON [dbo].[Weekends];
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Weekends_Institutes_InstituteId')
    ALTER TABLE [dbo].[Weekends] DROP CONSTRAINT [FK_Weekends_Institutes_InstituteId];
GO

IF COL_LENGTH(N'[dbo].[Weekends]', N'InstituteId') IS NOT NULL
    ALTER TABLE [dbo].[Weekends] DROP COLUMN [InstituteId];
GO

;WITH DuplicateRows AS
(
    SELECT [Id],
           ROW_NUMBER() OVER (PARTITION BY [DayOfWeek] ORDER BY [Id]) AS [RowNumber]
    FROM [dbo].[Weekends]
)
DELETE FROM DuplicateRows WHERE [RowNumber] > 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Weekends_DayOfWeek' AND [object_id] = OBJECT_ID(N'[dbo].[Weekends]'))
    CREATE UNIQUE INDEX [IX_Weekends_DayOfWeek] ON [dbo].[Weekends] ([DayOfWeek]);
GO

IF OBJECT_ID(N'[dbo].[Shifts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Shifts]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Shifts] PRIMARY KEY,
        [ShiftCode] NVARCHAR(50) NOT NULL,
        [ShiftName] NVARCHAR(150) NOT NULL,
        [StartTime] TIME NOT NULL,
        [EndTime] TIME NOT NULL,
        [LateAfterMinutes] INT NOT NULL CONSTRAINT [DF_Shifts_LateAfterMinutes] DEFAULT (0),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Shifts_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Shifts_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL
    );
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Shifts_InstituteId_ShiftCode' AND [object_id] = OBJECT_ID(N'[dbo].[Shifts]'))
    DROP INDEX [IX_Shifts_InstituteId_ShiftCode] ON [dbo].[Shifts];
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Shifts_Institutes_InstituteId')
    ALTER TABLE [dbo].[Shifts] DROP CONSTRAINT [FK_Shifts_Institutes_InstituteId];
GO

IF COL_LENGTH(N'[dbo].[Shifts]', N'InstituteId') IS NOT NULL
    ALTER TABLE [dbo].[Shifts] DROP COLUMN [InstituteId];
GO

;WITH DuplicateRows AS
(
    SELECT [Id],
           ROW_NUMBER() OVER (PARTITION BY [ShiftCode] ORDER BY [Id]) AS [RowNumber]
    FROM [dbo].[Shifts]
)
DELETE FROM DuplicateRows WHERE [RowNumber] > 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Shifts_ShiftCode' AND [object_id] = OBJECT_ID(N'[dbo].[Shifts]'))
    CREATE UNIQUE INDEX [IX_Shifts_ShiftCode] ON [dbo].[Shifts] ([ShiftCode]);
GO
