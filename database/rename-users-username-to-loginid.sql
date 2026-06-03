IF COL_LENGTH('dbo.Users', 'Username') IS NOT NULL
   AND COL_LENGTH('dbo.Users', 'LoginID') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Users.Username', 'LoginID', 'COLUMN';
END;

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Users_Username'
      AND object_id = OBJECT_ID('dbo.Users')
)
BEGIN
    EXEC sp_rename 'dbo.Users.IX_Users_Username', 'IX_Users_LoginID', 'INDEX';
END;
