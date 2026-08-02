USE MyAppDb;
GO

/* ==================================================================
   sp_Login
   Returns the user row needed for credential verification at the app
   layer (password hash/salt compared in code, never in T-SQL).
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_Login
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId, u.Username, u.PasswordHash, u.PasswordSalt, u.FullName, u.Email,
        u.RoleId, r.RoleName, u.Status, u.IsLocked, u.LockoutEnd,
        u.FailedLoginAttempts, u.PasswordExpiryDate, u.MustChangePassword
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE u.Username = @Username AND u.IsDeleted = 0;
END
GO

/* ==================================================================
   sp_RecordLoginSuccess
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_RecordLoginSuccess
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET LastLogin = SYSUTCDATETIME(),
        LoginCount = LoginCount + 1,
        FailedLoginAttempts = 0,
        IsLocked = 0,
        LockoutEnd = NULL
    WHERE UserId = @UserId;
END
GO

/* ==================================================================
   sp_RecordLoginFailure
   Increments failed attempts; locks the account when @MaxAttempts
   is reached, for @LockoutMinutes.
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_RecordLoginFailure
    @UserId INT,
    @MaxAttempts INT = 5,
    @LockoutMinutes INT = 15
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET FailedLoginAttempts = FailedLoginAttempts + 1,
        IsLocked = CASE WHEN FailedLoginAttempts + 1 >= @MaxAttempts THEN 1 ELSE IsLocked END,
        LockoutEnd = CASE WHEN FailedLoginAttempts + 1 >= @MaxAttempts
                          THEN DATEADD(MINUTE, @LockoutMinutes, SYSUTCDATETIME())
                          ELSE LockoutEnd END
    WHERE UserId = @UserId;
END
GO

/* ==================================================================
   sp_UnlockExpiredLockouts — call from a scheduled job, or lazily
   before login checks.
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_UnlockExpiredLockouts
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET IsLocked = 0, LockoutEnd = NULL, FailedLoginAttempts = 0
    WHERE IsLocked = 1 AND LockoutEnd IS NOT NULL AND LockoutEnd <= SYSUTCDATETIME();
END
GO

/* ==================================================================
   sp_ChangePassword
   Writes the new hash and archives the previous one to history.
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_ChangePassword
    @UserId INT,
    @NewPasswordHash VARBINARY(256),
    @NewPasswordSalt VARBINARY(128),
    @ExpiryDays INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.PasswordHistory (UserId, PasswordHash, PasswordSalt)
        SELECT UserId, PasswordHash, PasswordSalt FROM dbo.Users WHERE UserId = @UserId;

        UPDATE dbo.Users
        SET PasswordHash = @NewPasswordHash,
            PasswordSalt = @NewPasswordSalt,
            PasswordExpiryDate = DATEADD(DAY, @ExpiryDays, SYSUTCDATETIME()),
            MustChangePassword = 0,
            ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @UserId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ==================================================================
   sp_CheckPasswordHistory
   Returns matching historical hashes for the app layer to compare
   against a candidate new password (last N, default 5).
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_CheckPasswordHistory
    @UserId INT,
    @HistoryCount INT = 5
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@HistoryCount) PasswordHash, PasswordSalt
    FROM dbo.PasswordHistory
    WHERE UserId = @UserId
    ORDER BY CreatedDate DESC;
END
GO

/* ==================================================================
   sp_SetForgotPasswordToken / sp_ValidateResetToken use the same
   table for simplicity — a dedicated PasswordResetTokens table is
   recommended if reset volume is high; kept minimal here.
   ================================================================== */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PasswordResetTokens' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PasswordResetTokens
    (
        TokenId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PasswordResetTokens PRIMARY KEY,
        UserId       INT                NOT NULL,
        Token        NVARCHAR(200)      NOT NULL,
        ExpiryDate   DATETIME2          NOT NULL,
        IsUsed       BIT                NOT NULL CONSTRAINT DF_PRT_IsUsed DEFAULT (0),
        CreatedDate  DATETIME2          NOT NULL CONSTRAINT DF_PRT_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_PRT_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreatePasswordResetToken
    @UserId INT,
    @Token NVARCHAR(200),
    @ExpiryMinutes INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PasswordResetTokens (UserId, Token, ExpiryDate)
    VALUES (@UserId, @Token, DATEADD(MINUTE, @ExpiryMinutes, SYSUTCDATETIME()));
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ValidatePasswordResetToken
    @Token NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TokenId, UserId, ExpiryDate, IsUsed
    FROM dbo.PasswordResetTokens
    WHERE Token = @Token AND IsUsed = 0 AND ExpiryDate > SYSUTCDATETIME();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConsumePasswordResetToken
    @TokenId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.PasswordResetTokens SET IsUsed = 1 WHERE TokenId = @TokenId;
END
GO

/* ==================================================================
   sp_FindUserByEmail — account lookup for password reset (never
   reveals whether the email exists to callers)
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_FindUserByEmail
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 UserId
    FROM dbo.Users
    WHERE Email = @Email AND IsDeleted = 0;
END
GO

/* ==================================================================
   sp_GetPasswordHash — current credentials for change-password
   verification (hash/salt are compared in the app layer only)
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_GetPasswordHash
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT PasswordHash, PasswordSalt
    FROM dbo.Users
    WHERE UserId = @UserId AND IsDeleted = 0;
END
GO

/* ==================================================================
   Session tokens (opaque, hashed at rest — see SessionTokenService)
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_SaveSessionToken
    @UserId INT,
    @TokenHash NVARCHAR(64),
    @ExpiryDate DATETIME2,
    @IpAddress NVARCHAR(50) = NULL,
    @UserAgent NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SessionTokens (UserId, TokenHash, ExpiryDate, CreatedByIp, UserAgent)
    VALUES (@UserId, @TokenHash, @ExpiryDate, @IpAddress, @UserAgent);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetSessionByTokenHash
    @TokenHash NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT st.SessionTokenId, st.UserId, u.Username, u.RoleId, r.RoleName, st.ExpiryDate
    FROM dbo.SessionTokens st
    INNER JOIN dbo.Users u ON u.UserId = st.UserId AND u.IsDeleted = 0
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE st.TokenHash = @TokenHash
      AND st.IsRevoked = 0
      AND st.ExpiryDate > SYSUTCDATETIME()
      AND u.Status = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_RevokeSessionByTokenHash
    @TokenHash NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SessionTokens
    SET IsRevoked = 1, RevokedDate = SYSUTCDATETIME()
    WHERE TokenHash = @TokenHash AND IsRevoked = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_RevokeSession
    @SessionTokenId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SessionTokens
    SET IsRevoked = 1, RevokedDate = SYSUTCDATETIME()
    WHERE SessionTokenId = @SessionTokenId AND UserId = @UserId AND IsRevoked = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetUserSessions
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SessionTokenId, UserId, TokenHash, ExpiryDate, CreatedDate, CreatedByIp, UserAgent
    FROM dbo.SessionTokens
    WHERE UserId = @UserId AND IsRevoked = 0
    ORDER BY CreatedDate DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_RevokeAllUserSessions
    @UserId INT,
    @ExcludeTokenHash NVARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SessionTokens
    SET IsRevoked = 1, RevokedDate = SYSUTCDATETIME()
    WHERE UserId = @UserId AND IsRevoked = 0
      AND (@ExcludeTokenHash IS NULL OR TokenHash <> @ExcludeTokenHash);
END
GO
