IF COL_LENGTH('dbo.Users', 'PhoneNumber') IS NOT NULL
   AND COL_LENGTH('dbo.Users', 'MobileNumber') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Users.PhoneNumber', 'MobileNumber', 'COLUMN';
END;

IF COL_LENGTH('dbo.Users', 'Email') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Users ALTER COLUMN Email nvarchar(255) NULL;
END;

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Users')
      AND name = 'IX_Users_Email'
)
BEGIN
    DROP INDEX IX_Users_Email ON dbo.Users;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Users')
      AND name = 'IX_Users_Email'
)
BEGIN
    CREATE UNIQUE INDEX IX_Users_Email
    ON dbo.Users (Email)
    WHERE Email IS NOT NULL;
END;

IF COL_LENGTH('dbo.Users', 'MobileNumber') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE dbo.Users
        SET MobileNumber = LTRIM(RTRIM(MobileNumber))
        WHERE MobileNumber IS NOT NULL;
    ');

    EXEC(N'ALTER TABLE dbo.Users ALTER COLUMN MobileNumber nvarchar(11) NOT NULL;');
END;

IF OBJECT_ID('dbo.CK_Users_MobileNumber_BD', 'C') IS NULL
BEGIN
    EXEC(N'
        ALTER TABLE dbo.Users WITH NOCHECK
        ADD CONSTRAINT CK_Users_MobileNumber_BD
        CHECK (MobileNumber LIKE ''01[3-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'');
    ');
END;
