USE MyAppDb;
GO

/* Permission types */
INSERT INTO dbo.PermissionTypes (Code, Name) VALUES
('VIEW', 'View'), ('ADD', 'Add'), ('EDIT', 'Edit'), ('DELETE', 'Delete'),
('PRINT', 'Print'), ('EXPORT', 'Export'), ('APPROVE', 'Approve'), ('REJECT', 'Reject');
GO

/* Default system role */
INSERT INTO dbo.Roles (RoleName, Description, IsSystemRole, IsActive)
VALUES ('Administrator', 'Full system access', 1, 1);
GO

/* Root menus (top-level) */
INSERT INTO dbo.Menus (ParentMenuId, MenuName, MenuIcon, ControllerPage, Route, MenuOrder, Status) VALUES
(NULL, 'Dashboard', 'bi-speedometer2', 'Dashboard', '/dashboard', 1, 1),
(NULL, 'User Management', 'bi-people', NULL, NULL, 2, 1),
(NULL, 'Settings', 'bi-gear', NULL, NULL, 3, 1),
(NULL, 'Audit Logs', 'bi-journal-text', 'AuditLogs', '/audit-logs', 4, 1);
GO

/* Child menus under User Management */
DECLARE @UserMgmtId INT = (SELECT MenuId FROM dbo.Menus WHERE MenuName = 'User Management' AND ParentMenuId IS NULL);

INSERT INTO dbo.Menus (ParentMenuId, MenuName, MenuIcon, ControllerPage, Route, MenuOrder, Status) VALUES
(@UserMgmtId, 'Users', 'bi-person', 'Users', '/users', 1, 1),
(@UserMgmtId, 'Roles', 'bi-shield-lock', 'Roles', '/roles', 2, 1),
(@UserMgmtId, 'Permission Matrix', 'bi-grid-3x3', 'Permissions', '/permissions', 3, 1),
(@UserMgmtId, 'Administration', 'bi-speedometer', 'Administration', '/administration', 4, 1);
GO

/* Child menus under Settings */
DECLARE @SettingsId INT = (SELECT MenuId FROM dbo.Menus WHERE MenuName = 'Settings' AND ParentMenuId IS NULL);

INSERT INTO dbo.Menus (ParentMenuId, MenuName, MenuIcon, ControllerPage, Route, MenuOrder, Status) VALUES
(@SettingsId, 'Menu Management', 'bi-list-nested', 'Menus', '/menus', 1, 1),
(@SettingsId, 'Preferences', 'bi-palette', 'Settings', '/settings', 2, 1);
GO

/* Grant Administrator role full permissions on every menu */
INSERT INTO dbo.RoleMenuPermissions (RoleId, MenuId, PermissionTypeId)
SELECT r.RoleId, m.MenuId, p.PermissionTypeId
FROM dbo.Roles r
CROSS JOIN dbo.Menus m
CROSS JOIN dbo.PermissionTypes p
WHERE r.RoleName = 'Administrator';
GO

/* Default admin user — password is 'ChangeMe123!' hashed with PBKDF2-SHA256
   (16-byte salt, 32-byte key, 210,000 iterations — matches the app's
   PasswordHasher), so the seeded account works out of the box. */
DECLARE @AdminRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Administrator');

INSERT INTO dbo.Users
    (EmployeeNumber, Username, PasswordHash, PasswordSalt, FullName, Email, RoleId, Status, PasswordExpiryDate)
VALUES
    ('EMP-0001', 'admin',
     0x3D770256915506259044E3EE63561858FC9A3AE135A09BEE899787A50EB0E1E9,
     0x7F6C2BCC82C4888DAD7FBEF517C86A30,
     'System Administrator', 'admin@myapp.local', @AdminRoleId, 1, DATEADD(DAY, 90, SYSUTCDATETIME()));
GO

INSERT INTO dbo.UserPreferences (UserId, Theme, SidebarCollapsed, Language, LandingPage)
SELECT UserId, 'light', 0, 'en', '/dashboard' FROM dbo.Users WHERE Username = 'admin';
GO

/* Themes supported by the UI theme engine: light | dark | glass */

