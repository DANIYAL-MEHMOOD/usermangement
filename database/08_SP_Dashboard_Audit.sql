USE MyAppDb;
GO

/* ==================================================================
   sp_GetDashboard
   Returns four result sets: summary counts, recent logins,
   recent activity, role statistics.
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_GetDashboard
    @RecentCount INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    -- 1) Summary counts
    SELECT
        (SELECT COUNT(*) FROM dbo.Users WHERE IsDeleted = 0) AS TotalUsers,
        (SELECT COUNT(*) FROM dbo.Users WHERE IsDeleted = 0 AND Status = 1) AS ActiveUsers,
        (SELECT COUNT(*) FROM dbo.Users WHERE IsDeleted = 0 AND Status = 2) AS LockedUsers,
        (SELECT COUNT(*) FROM dbo.Roles WHERE IsDeleted = 0) AS TotalRoles,
        (SELECT COUNT(*) FROM dbo.Menus WHERE IsDeleted = 0) AS TotalMenus;

    -- 2) Recent logins
    SELECT TOP (@RecentCount) UserId, Username, FullName, LastLogin
    FROM dbo.Users
    WHERE IsDeleted = 0 AND LastLogin IS NOT NULL
    ORDER BY LastLogin DESC;

    -- 3) Recent activity (audit log)
    SELECT TOP (@RecentCount) a.AuditLogId, a.UserId, u.FullName, a.Module, a.Action, a.ActionDate
    FROM dbo.AuditLogs a
    LEFT JOIN dbo.Users u ON u.UserId = a.UserId
    ORDER BY a.ActionDate DESC;

    -- 4) Role statistics
    SELECT r.RoleId, r.RoleName, COUNT(u.UserId) AS UserCount
    FROM dbo.Roles r
    LEFT JOIN dbo.Users u ON u.RoleId = r.RoleId AND u.IsDeleted = 0
    WHERE r.IsDeleted = 0
    GROUP BY r.RoleId, r.RoleName
    ORDER BY UserCount DESC;
END
GO

/* ==================================================================
   sp_InsertAuditLog
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_InsertAuditLog
    @UserId INT = NULL,
    @Module NVARCHAR(100),
    @Action NVARCHAR(100),
    @OldValue NVARCHAR(MAX) = NULL,
    @NewValue NVARCHAR(MAX) = NULL,
    @Browser NVARCHAR(300) = NULL,
    @IPAddress NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLogs (UserId, Module, Action, OldValue, NewValue, Browser, IPAddress)
    VALUES (@UserId, @Module, @Action, @OldValue, @NewValue, @Browser, @IPAddress);
END
GO

/* ==================================================================
   sp_GetAuditLogs — filtered + paginated
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_GetAuditLogs
    @SearchTerm NVARCHAR(200) = NULL,
    @Module NVARCHAR(100) = NULL,
    @Action NVARCHAR(100) = NULL,
    @UserId INT = NULL,
    @DateFrom DATETIME2 = NULL,
    @DateTo DATETIME2 = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 25
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT a.AuditLogId, a.UserId, u.FullName, a.Module, a.Action, a.OldValue, a.NewValue,
           a.Browser, a.IPAddress, a.ActionDate
    FROM dbo.AuditLogs a
    LEFT JOIN dbo.Users u ON u.UserId = a.UserId
    WHERE (@UserId IS NULL OR a.UserId = @UserId)
      AND (@Module IS NULL OR a.Module = @Module)
      AND (@Action IS NULL OR a.Action = @Action)
      AND (@DateFrom IS NULL OR a.ActionDate >= @DateFrom)
      AND (@DateTo IS NULL OR a.ActionDate <= @DateTo)
      AND (@SearchTerm IS NULL OR u.FullName LIKE '%' + @SearchTerm + '%'
           OR a.Module LIKE '%' + @SearchTerm + '%' OR a.Action LIKE '%' + @SearchTerm + '%'
           OR a.NewValue LIKE '%' + @SearchTerm + '%')
    ORDER BY a.ActionDate DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(*) AS TotalCount
    FROM dbo.AuditLogs a
    LEFT JOIN dbo.Users u ON u.UserId = a.UserId
    WHERE (@UserId IS NULL OR a.UserId = @UserId)
      AND (@Module IS NULL OR a.Module = @Module)
      AND (@Action IS NULL OR a.Action = @Action)
      AND (@DateFrom IS NULL OR a.ActionDate >= @DateFrom)
      AND (@DateTo IS NULL OR a.ActionDate <= @DateTo)
      AND (@SearchTerm IS NULL OR u.FullName LIKE '%' + @SearchTerm + '%'
           OR a.Module LIKE '%' + @SearchTerm + '%' OR a.Action LIKE '%' + @SearchTerm + '%'
           OR a.NewValue LIKE '%' + @SearchTerm + '%');
END
GO
