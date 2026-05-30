SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH(N'[dbo].[Users]', N'GradeId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [GradeId] INT NULL;
END
GO

IF COL_LENGTH(N'[dbo].[Users]', N'DesignationId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [DesignationId] INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Users_GradeId' AND [object_id] = OBJECT_ID(N'[dbo].[Users]'))
    CREATE INDEX [IX_Users_GradeId] ON [dbo].[Users] ([GradeId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Users_DesignationId' AND [object_id] = OBJECT_ID(N'[dbo].[Users]'))
    CREATE INDEX [IX_Users_DesignationId] ON [dbo].[Users] ([DesignationId]);
GO

IF OBJECT_ID(N'[dbo].[FK_Users_Grades_GradeId]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] WITH CHECK
    ADD CONSTRAINT [FK_Users_Grades_GradeId]
    FOREIGN KEY ([GradeId]) REFERENCES [dbo].[Grades] ([Id]);
END
GO

IF OBJECT_ID(N'[dbo].[FK_Users_Designations_DesignationId]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] WITH CHECK
    ADD CONSTRAINT [FK_Users_Designations_DesignationId]
    FOREIGN KEY ([DesignationId]) REFERENCES [dbo].[Designations] ([Id]);
END
GO
