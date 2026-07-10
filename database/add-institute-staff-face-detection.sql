IF COL_LENGTH(N'[dbo].[Institutes]', N'StaffFaceDetectionIsRequired') IS NULL
BEGIN
    ALTER TABLE [dbo].[Institutes]
        ADD [StaffFaceDetectionIsRequired] bit NOT NULL
            CONSTRAINT [DF_Institutes_StaffFaceDetectionIsRequired] DEFAULT (0);
END
ELSE
BEGIN
    UPDATE [dbo].[Institutes]
    SET [StaffFaceDetectionIsRequired] = 0
    WHERE [StaffFaceDetectionIsRequired] IS NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE [object_id] = OBJECT_ID(N'[dbo].[Institutes]')
          AND [name] = N'StaffFaceDetectionIsRequired'
          AND [is_nullable] = 1
    )
    BEGIN
        ALTER TABLE [dbo].[Institutes]
            ALTER COLUMN [StaffFaceDetectionIsRequired] bit NOT NULL;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.[object_id] = dc.[parent_object_id]
           AND c.[column_id] = dc.[parent_column_id]
        WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[Institutes]')
          AND c.[name] = N'StaffFaceDetectionIsRequired'
    )
    BEGIN
        ALTER TABLE [dbo].[Institutes]
            ADD CONSTRAINT [DF_Institutes_StaffFaceDetectionIsRequired]
            DEFAULT (0) FOR [StaffFaceDetectionIsRequired];
    END
END
