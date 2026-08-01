USE MyAppDb;
GO

/* ==================================================================
   sp_GetProfile
   Returns profile information and preferences for a user
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_GetProfile
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId,
        u.EmployeeNumber,
        u.Username,
        u.FullName,
        u.Email,
        u.Phone,
        u.Designation,
        u.Department,
        u.ProfilePicturePath,
        u.RoleId,
        r.RoleName,
        u.LastLogin,
        ISNULL(p.Theme, 'light') AS Theme,
        ISNULL(p.SidebarCollapsed, 0) AS SidebarCollapsed,
        ISNULL(p.Language, 'en') AS Language,
        p.DashboardLayout,
        p.LandingPage
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    LEFT JOIN dbo.UserPreferences p ON p.UserId = u.UserId
    WHERE u.UserId = @UserId AND u.IsDeleted = 0;
END
GO

/* ==================================================================
   sp_UpdateProfile
   Updates personal profile information for a user
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_UpdateProfile
    @UserId INT,
    @FullName NVARCHAR(200),
    @Email NVARCHAR(256),
    @Phone NVARCHAR(30) = NULL,
    @Designation NVARCHAR(150) = NULL,
    @Department NVARCHAR(150) = NULL,
    @ProfilePicturePath NVARCHAR(500) = NULL,
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email AND UserId <> @UserId AND IsDeleted = 0)
    BEGIN
        RAISERROR('Email already exists.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Users
    SET
        FullName = @FullName,
        Email = @Email,
        Phone = @Phone,
        Designation = @Designation,
        Department = @Department,
        ProfilePicturePath = COALESCE(@ProfilePicturePath, ProfilePicturePath),
        ModifiedDate = SYSUTCDATETIME(),
        ModifiedBy = @ModifiedBy
    WHERE UserId = @UserId AND IsDeleted = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
