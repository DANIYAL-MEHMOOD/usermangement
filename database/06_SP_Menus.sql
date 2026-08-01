USE MyAppDb;
GO

/* ==================================================================
   sp_GetMenus — full flat list, for the Menu Management admin screen
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_GetMenus
AS
BEGIN
    SET NOCOUNT ON;

    SELECT MenuId, ParentMenuId, MenuName, MenuIcon, ControllerPage, Route,
           MenuOrder, Status, CreatedDate, ModifiedDate
    FROM dbo.Menus
    WHERE IsDeleted = 0
    ORDER BY ParentMenuId, MenuOrder;
END
GO

/* ==================================================================
   sp_GetUserMenus
   Returns only the menus the logged-in user is entitled to see:
   role-granted VIEW permission, minus any explicit user-level deny,
   plus any explicit user-level grant. Also returns ancestor menus so
   the sidebar tree isn't missing parent nodes.
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_GetUserMenus
    @UserId INT,
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH ViewPermId AS (
        SELECT PermissionTypeId FROM dbo.PermissionTypes WHERE Code = 'VIEW'
    ),
    Granted AS (
        SELECT rmp.MenuId
        FROM dbo.RoleMenuPermissions rmp
        INNER JOIN ViewPermId vp ON vp.PermissionTypeId = rmp.PermissionTypeId
        WHERE rmp.RoleId = @RoleId

        UNION

        SELECT ump.MenuId
        FROM dbo.UserMenuPermissions ump
        INNER JOIN ViewPermId vp ON vp.PermissionTypeId = ump.PermissionTypeId
        WHERE ump.UserId = @UserId AND ump.IsGranted = 1
    ),
    Denied AS (
        SELECT ump.MenuId
        FROM dbo.UserMenuPermissions ump
        INNER JOIN ViewPermId vp ON vp.PermissionTypeId = ump.PermissionTypeId
        WHERE ump.UserId = @UserId AND ump.IsGranted = 0
    ),
    Allowed AS (
        SELECT MenuId FROM Granted
        EXCEPT
        SELECT MenuId FROM Denied
    ),
    -- walk up to include ancestor menus so the tree renders correctly
    WithAncestors AS (
        SELECT m.MenuId, m.ParentMenuId, m.MenuName, m.MenuIcon, m.ControllerPage,
               m.Route, m.MenuOrder
        FROM dbo.Menus m
        INNER JOIN Allowed a ON a.MenuId = m.MenuId
        WHERE m.Status = 1 AND m.IsDeleted = 0

        UNION

        SELECT p.MenuId, p.ParentMenuId, p.MenuName, p.MenuIcon, p.ControllerPage,
               p.Route, p.MenuOrder
        FROM dbo.Menus p
        INNER JOIN WithAncestors c ON c.ParentMenuId = p.MenuId
        WHERE p.Status = 1 AND p.IsDeleted = 0
    )
    SELECT DISTINCT MenuId, ParentMenuId, MenuName, MenuIcon, ControllerPage, Route, MenuOrder
    FROM WithAncestors
    ORDER BY ParentMenuId, MenuOrder;
END
GO

/* ==================================================================
   sp_SaveMenu — create when @MenuId IS NULL, update otherwise
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_SaveMenu
    @MenuId INT = NULL,
    @ParentMenuId INT = NULL,
    @MenuName NVARCHAR(150),
    @MenuIcon NVARCHAR(100) = NULL,
    @ControllerPage NVARCHAR(200) = NULL,
    @Route NVARCHAR(300) = NULL,
    @MenuOrder INT = 0,
    @Status BIT = 1,
    @UserId INT,
    @NewMenuId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @MenuId IS NULL
    BEGIN
        INSERT INTO dbo.Menus (ParentMenuId, MenuName, MenuIcon, ControllerPage, Route, MenuOrder, Status, CreatedBy)
        VALUES (@ParentMenuId, @MenuName, @MenuIcon, @ControllerPage, @Route, @MenuOrder, @Status, @UserId);

        SET @NewMenuId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        IF @ParentMenuId = @MenuId
        BEGIN
            RAISERROR('A menu cannot be its own parent.', 16, 1);
            RETURN;
        END

        UPDATE dbo.Menus
        SET ParentMenuId = @ParentMenuId, MenuName = @MenuName, MenuIcon = @MenuIcon,
            ControllerPage = @ControllerPage, Route = @Route, MenuOrder = @MenuOrder,
            Status = @Status, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE MenuId = @MenuId;

        SET @NewMenuId = @MenuId;
    END
END
GO

/* ==================================================================
   sp_DeleteMenu — blocked if child menus still reference it
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_DeleteMenu
    @MenuId INT,
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Menus WHERE ParentMenuId = @MenuId AND IsDeleted = 0)
    BEGIN
        RAISERROR('Cannot delete a menu that still has child menus.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Menus
    SET IsDeleted = 1, Status = 0, ModifiedBy = @ModifiedBy, ModifiedDate = SYSUTCDATETIME()
    WHERE MenuId = @MenuId;
END
GO
