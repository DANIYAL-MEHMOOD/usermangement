USE MyAppDb;
GO

/* ==================================================================
   sp_SaveRole — create when @RoleId IS NULL, update otherwise
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_SaveRole
    @RoleId INT = NULL,
    @RoleName NVARCHAR(100),
    @Description NVARCHAR(500) = NULL,
    @IsActive BIT = 1,
    @UserId INT,
    @NewRoleId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @RoleId IS NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = @RoleName AND IsDeleted = 0)
        BEGIN
            RAISERROR('Role name already exists.', 16, 1);
            RETURN;
        END

        INSERT INTO dbo.Roles (RoleName, Description, IsActive, CreatedBy)
        VALUES (@RoleName, @Description, @IsActive, @UserId);

        SET @NewRoleId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = @RoleName AND RoleId <> @RoleId AND IsDeleted = 0)
        BEGIN
            RAISERROR('Role name already exists.', 16, 1);
            RETURN;
        END

        UPDATE dbo.Roles
        SET RoleName = @RoleName, Description = @Description, IsActive = @IsActive,
            ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE RoleId = @RoleId AND IsSystemRole = 0;

        SET @NewRoleId = @RoleId;
    END
END
GO

/* ==================================================================
   sp_GetRoles
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_GetRoles
    @SearchTerm NVARCHAR(200) = NULL,
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.RoleId, r.RoleName, r.Description, r.IsSystemRole, r.IsActive, r.CreatedDate,
           (SELECT COUNT(*) FROM dbo.Users u WHERE u.RoleId = r.RoleId AND u.IsDeleted = 0) AS UserCount
    FROM dbo.Roles r
    WHERE r.IsDeleted = 0
      AND (@SearchTerm IS NULL OR r.RoleName LIKE '%' + @SearchTerm + '%')
      AND (@IsActive IS NULL OR r.IsActive = @IsActive)
    ORDER BY r.RoleName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetRoleById
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT RoleId, RoleName, Description, IsSystemRole, IsActive, CreatedDate, ModifiedDate
    FROM dbo.Roles
    WHERE RoleId = @RoleId AND IsDeleted = 0;
END
GO

/* ==================================================================
   sp_DeleteRole — blocked if users are still assigned or it's a
   system role.
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_DeleteRole
    @RoleId INT,
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleId = @RoleId AND IsSystemRole = 1)
    BEGIN
        RAISERROR('System roles cannot be deleted.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Users WHERE RoleId = @RoleId AND IsDeleted = 0)
    BEGIN
        RAISERROR('Cannot delete a role that still has assigned users.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Roles
    SET IsDeleted = 1, IsActive = 0, ModifiedBy = @ModifiedBy, ModifiedDate = SYSUTCDATETIME()
    WHERE RoleId = @RoleId;
END
GO

/* ==================================================================
   sp_CloneRole — copies a role's full menu/permission matrix onto a
   brand-new role.
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_CloneRole
    @SourceRoleId INT,
    @NewRoleName NVARCHAR(100),
    @Description NVARCHAR(500) = NULL,
    @CreatedBy INT,
    @NewRoleId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = @NewRoleName AND IsDeleted = 0)
    BEGIN
        RAISERROR('Role name already exists.', 16, 1);
        RETURN;
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.Roles (RoleName, Description, IsActive, CreatedBy)
        VALUES (@NewRoleName, @Description, 1, @CreatedBy);

        SET @NewRoleId = SCOPE_IDENTITY();

        INSERT INTO dbo.RoleMenuPermissions (RoleId, MenuId, PermissionTypeId, CreatedBy)
        SELECT @NewRoleId, MenuId, PermissionTypeId, @CreatedBy
        FROM dbo.RoleMenuPermissions
        WHERE RoleId = @SourceRoleId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ==================================================================
   sp_AssignRole — change a single user's role
   ================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_AssignRole
    @UserId INT,
    @RoleId INT,
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET RoleId = @RoleId, ModifiedBy = @ModifiedBy, ModifiedDate = SYSUTCDATETIME()
    WHERE UserId = @UserId AND IsDeleted = 0;
END
GO
