USE MyAppDb;
GO

/* ==================================================================
   sp_GetPermissionTypes
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_GetPermissionTypes
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PermissionTypeId, Code, Name FROM dbo.PermissionTypes ORDER BY PermissionTypeId;
END
GO

/* ==================================================================
   sp_GetRolePermissions
   Returns the full menu x permission-type grid for a role, with a
   0/1 Granted flag per cell — exactly what the Permission Matrix
   screen needs to render checkboxes.
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_GetRolePermissions
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.MenuId, m.ParentMenuId, m.MenuName, m.MenuOrder,
        pt.PermissionTypeId, pt.Code, pt.Name,
        CASE WHEN rmp.RoleMenuPermissionId IS NULL THEN 0 ELSE 1 END AS Granted
    FROM dbo.Menus m
    CROSS JOIN dbo.PermissionTypes pt
    LEFT JOIN dbo.RoleMenuPermissions rmp
        ON rmp.MenuId = m.MenuId AND rmp.PermissionTypeId = pt.PermissionTypeId AND rmp.RoleId = @RoleId
    WHERE m.IsDeleted = 0
    ORDER BY m.ParentMenuId, m.MenuOrder, pt.PermissionTypeId;
END
GO

/* ==================================================================
   sp_AssignPermissions
   Bulk-saves the entire matrix for a role in one call. Pass a
   MenuId/PermissionTypeId pair list as TVP; anything not in the
   list is revoked, everything in it is granted (full replace).
   ================================================================== */
IF NOT EXISTS (SELECT 1 FROM sys.types WHERE is_table_type = 1 AND name = 'MenuPermissionTableType')
BEGIN
    CREATE TYPE dbo.MenuPermissionTableType AS TABLE
    (
        MenuId INT NOT NULL,
        PermissionTypeId INT NOT NULL
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AssignPermissions
    @RoleId INT,
    @Permissions dbo.MenuPermissionTableType READONLY,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        DELETE FROM dbo.RoleMenuPermissions WHERE RoleId = @RoleId;

        INSERT INTO dbo.RoleMenuPermissions (RoleId, MenuId, PermissionTypeId, CreatedBy)
        SELECT @RoleId, MenuId, PermissionTypeId, @UserId
        FROM @Permissions;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ==================================================================
   sp_UserHasPermission — single-cell check used by API-layer
   authorization filters (menu/action level, not just role level).
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_UserHasPermission
    @UserId INT,
    @RoleId INT,
    @ControllerPage NVARCHAR(200),
    @PermissionCode NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MenuId INT = (SELECT TOP 1 MenuId FROM dbo.Menus WHERE ControllerPage = @ControllerPage AND IsDeleted = 0);
    DECLARE @PermissionTypeId INT = (SELECT PermissionTypeId FROM dbo.PermissionTypes WHERE Code = @PermissionCode);

    IF @MenuId IS NULL OR @PermissionTypeId IS NULL
    BEGIN
        SELECT CAST(0 AS BIT) AS HasPermission;
        RETURN;
    END

    DECLARE @Denied BIT = CASE WHEN EXISTS (
        SELECT 1 FROM dbo.UserMenuPermissions
        WHERE UserId = @UserId AND MenuId = @MenuId AND PermissionTypeId = @PermissionTypeId AND IsGranted = 0
    ) THEN 1 ELSE 0 END;

    DECLARE @Granted BIT = CASE WHEN EXISTS (
        SELECT 1 FROM dbo.RoleMenuPermissions
        WHERE RoleId = @RoleId AND MenuId = @MenuId AND PermissionTypeId = @PermissionTypeId
    ) OR EXISTS (
        SELECT 1 FROM dbo.UserMenuPermissions
        WHERE UserId = @UserId AND MenuId = @MenuId AND PermissionTypeId = @PermissionTypeId AND IsGranted = 1
    ) THEN 1 ELSE 0 END;

    SELECT CASE WHEN @Denied = 1 THEN CAST(0 AS BIT) ELSE @Granted END AS HasPermission;
END
GO
