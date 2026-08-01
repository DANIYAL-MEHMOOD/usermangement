USE MyAppDb;
GO

/* ==================================================================
   sp_CreateUser
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_CreateUser
    @EmployeeNumber NVARCHAR(50) = NULL,
    @Username NVARCHAR(100),
    @PasswordHash VARBINARY(256),
    @PasswordSalt VARBINARY(128),
    @FullName NVARCHAR(200),
    @Email NVARCHAR(256),
    @Phone NVARCHAR(30) = NULL,
    @Designation NVARCHAR(150) = NULL,
    @Department NVARCHAR(150) = NULL,
    @ProfilePicturePath NVARCHAR(500) = NULL,
    @RoleId INT,
    @PasswordExpiryDays INT = 90,
    @CreatedBy INT,
    @NewUserId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username AND IsDeleted = 0)
    BEGIN
        RAISERROR('Username already exists.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email AND IsDeleted = 0)
    BEGIN
        RAISERROR('Email already exists.', 16, 1);
        RETURN;
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.Users
            (EmployeeNumber, Username, PasswordHash, PasswordSalt, FullName, Email, Phone,
             Designation, Department, ProfilePicturePath, RoleId, Status, PasswordExpiryDate, CreatedBy)
        VALUES
            (@EmployeeNumber, @Username, @PasswordHash, @PasswordSalt, @FullName, @Email, @Phone,
             @Designation, @Department, @ProfilePicturePath, @RoleId, 1,
             DATEADD(DAY, @PasswordExpiryDays, SYSUTCDATETIME()), @CreatedBy);

        SET @NewUserId = SCOPE_IDENTITY();

        INSERT INTO dbo.PasswordHistory (UserId, PasswordHash, PasswordSalt)
        VALUES (@NewUserId, @PasswordHash, @PasswordSalt);

        INSERT INTO dbo.UserPreferences (UserId)
        VALUES (@NewUserId);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ==================================================================
   sp_UpdateUser
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_UpdateUser
    @UserId INT,
    @EmployeeNumber NVARCHAR(50) = NULL,
    @FullName NVARCHAR(200),
    @Email NVARCHAR(256),
    @Phone NVARCHAR(30) = NULL,
    @Designation NVARCHAR(150) = NULL,
    @Department NVARCHAR(150) = NULL,
    @ProfilePicturePath NVARCHAR(500) = NULL,
    @RoleId INT,
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email AND UserId <> @UserId AND IsDeleted = 0)
    BEGIN
        RAISERROR('Email already in use by another user.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Users
    SET EmployeeNumber = @EmployeeNumber,
        FullName = @FullName,
        Email = @Email,
        Phone = @Phone,
        Designation = @Designation,
        Department = @Department,
        ProfilePicturePath = COALESCE(@ProfilePicturePath, ProfilePicturePath),
        RoleId = @RoleId,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE UserId = @UserId AND IsDeleted = 0;
END
GO

/* ==================================================================
   sp_DeleteUser (soft delete)
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_DeleteUser
    @UserId INT,
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET IsDeleted = 1, Status = 0, ModifiedBy = @ModifiedBy, ModifiedDate = SYSUTCDATETIME()
    WHERE UserId = @UserId;
END
GO

/* ==================================================================
   sp_SetUserStatus — handles both Activate and Deactivate
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_SetUserStatus
    @UserId INT,
    @Status TINYINT, -- 1 = Active, 0 = Inactive
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET Status = @Status, ModifiedBy = @ModifiedBy, ModifiedDate = SYSUTCDATETIME()
    WHERE UserId = @UserId AND IsDeleted = 0;
END
GO

/* ==================================================================
   sp_GetUserById
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId, u.EmployeeNumber, u.Username, u.FullName, u.Email, u.Phone,
        u.Designation, u.Department, u.ProfilePicturePath, u.RoleId, r.RoleName,
        u.Status, u.PasswordExpiryDate, u.MustChangePassword, u.IsLocked,
        u.LastLogin, u.LoginCount, u.CreatedDate, u.ModifiedDate, u.CreatedBy, u.ModifiedBy
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE u.UserId = @UserId AND u.IsDeleted = 0;
END
GO

/* ==================================================================
   sp_GetUsers
   Search + filter + sort + pagination in one round trip.
   @SortColumn is validated against a whitelist to prevent injection
   via dynamic ORDER BY.
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_GetUsers
    @SearchTerm NVARCHAR(200) = NULL,
    @RoleId INT = NULL,
    @Status TINYINT = NULL,
    @Department NVARCHAR(150) = NULL,
    @SortColumn NVARCHAR(50) = 'FullName',
    @SortDirection NVARCHAR(4) = 'ASC',
    @PageNumber INT = 1,
    @PageSize INT = 25
AS
BEGIN
    SET NOCOUNT ON;

    IF @SortColumn NOT IN ('FullName','Username','Email','Department','CreatedDate','LastLogin','Status')
        SET @SortColumn = 'FullName';
    IF @SortDirection NOT IN ('ASC','DESC')
        SET @SortDirection = 'ASC';

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    ;WITH Filtered AS
    (
        SELECT
            u.UserId, u.EmployeeNumber, u.Username, u.FullName, u.Email, u.Phone,
            u.Designation, u.Department, u.ProfilePicturePath, u.RoleId, r.RoleName,
            u.Status, u.LastLogin, u.LoginCount, u.CreatedDate
        FROM dbo.Users u
        INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
        WHERE u.IsDeleted = 0
          AND (@SearchTerm IS NULL OR u.FullName LIKE '%' + @SearchTerm + '%'
               OR u.Username LIKE '%' + @SearchTerm + '%' OR u.Email LIKE '%' + @SearchTerm + '%')
          AND (@RoleId IS NULL OR u.RoleId = @RoleId)
          AND (@Status IS NULL OR u.Status = @Status)
          AND (@Department IS NULL OR u.Department = @Department)
    )
    SELECT * FROM Filtered
    ORDER BY
        CASE WHEN @SortColumn = 'FullName' AND @SortDirection = 'ASC' THEN FullName END ASC,
        CASE WHEN @SortColumn = 'FullName' AND @SortDirection = 'DESC' THEN FullName END DESC,
        CASE WHEN @SortColumn = 'Username' AND @SortDirection = 'ASC' THEN Username END ASC,
        CASE WHEN @SortColumn = 'Username' AND @SortDirection = 'DESC' THEN Username END DESC,
        CASE WHEN @SortColumn = 'Email' AND @SortDirection = 'ASC' THEN Email END ASC,
        CASE WHEN @SortColumn = 'Email' AND @SortDirection = 'DESC' THEN Email END DESC,
        CASE WHEN @SortColumn = 'Department' AND @SortDirection = 'ASC' THEN Department END ASC,
        CASE WHEN @SortColumn = 'Department' AND @SortDirection = 'DESC' THEN Department END DESC,
        CASE WHEN @SortColumn = 'CreatedDate' AND @SortDirection = 'ASC' THEN CreatedDate END ASC,
        CASE WHEN @SortColumn = 'CreatedDate' AND @SortDirection = 'DESC' THEN CreatedDate END DESC,
        CASE WHEN @SortColumn = 'LastLogin' AND @SortDirection = 'ASC' THEN LastLogin END ASC,
        CASE WHEN @SortColumn = 'LastLogin' AND @SortDirection = 'DESC' THEN LastLogin END DESC,
        CASE WHEN @SortColumn = 'Status' AND @SortDirection = 'ASC' THEN Status END ASC,
        CASE WHEN @SortColumn = 'Status' AND @SortDirection = 'DESC' THEN Status END DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(*) AS TotalCount
    FROM dbo.Users u
    WHERE u.IsDeleted = 0
      AND (@SearchTerm IS NULL OR u.FullName LIKE '%' + @SearchTerm + '%'
           OR u.Username LIKE '%' + @SearchTerm + '%' OR u.Email LIKE '%' + @SearchTerm + '%')
      AND (@RoleId IS NULL OR u.RoleId = @RoleId)
      AND (@Status IS NULL OR u.Status = @Status)
      AND (@Department IS NULL OR u.Department = @Department);
END
GO

/* ==================================================================
   sp_GetUserPreferences / sp_SaveUserPreferences
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_GetUserPreferences
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Theme, SidebarCollapsed, Language, DashboardLayout, LandingPage
    FROM dbo.UserPreferences
    WHERE UserId = @UserId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SaveUserPreferences
    @UserId INT,
    @Theme NVARCHAR(20),
    @SidebarCollapsed BIT,
    @Language NVARCHAR(10),
    @DashboardLayout NVARCHAR(50) = NULL,
    @LandingPage NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.UserPreferences WHERE UserId = @UserId)
        UPDATE dbo.UserPreferences
        SET Theme = @Theme, SidebarCollapsed = @SidebarCollapsed, Language = @Language,
            DashboardLayout = @DashboardLayout, LandingPage = @LandingPage, ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @UserId;
    ELSE
        INSERT INTO dbo.UserPreferences (UserId, Theme, SidebarCollapsed, Language, DashboardLayout, LandingPage)
        VALUES (@UserId, @Theme, @SidebarCollapsed, @Language, @DashboardLayout, @LandingPage);
END
GO
