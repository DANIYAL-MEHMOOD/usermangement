/* ==================================================================
   MyApp - User Management Foundation
   01_Schema.sql
   Target: SQL Server 2019+
   ================================================================== */

IF DB_ID('MyAppDb') IS NULL
BEGIN
    CREATE DATABASE MyAppDb;
END
GO

USE MyAppDb;
GO

/* ------------------------------------------------------------------
   Roles
   ------------------------------------------------------------------ */
CREATE TABLE dbo.Roles
(
    RoleId          INT IDENTITY(1,1)   NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    RoleName        NVARCHAR(100)       NOT NULL,
    Description     NVARCHAR(500)       NULL,
    IsSystemRole    BIT                 NOT NULL CONSTRAINT DF_Roles_IsSystemRole DEFAULT (0),
    IsActive        BIT                 NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT (1),
    IsDeleted       BIT                 NOT NULL CONSTRAINT DF_Roles_IsDeleted DEFAULT (0),
    CreatedDate     DATETIME2           NOT NULL CONSTRAINT DF_Roles_CreatedDate DEFAULT (SYSUTCDATETIME()),
    ModifiedDate    DATETIME2           NULL,
    CreatedBy       INT                 NULL,
    ModifiedBy      INT                 NULL,
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);
GO

/* ------------------------------------------------------------------
   Users
   ------------------------------------------------------------------ */
CREATE TABLE dbo.Users
(
    UserId              INT IDENTITY(1,1)   NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    EmployeeNumber      NVARCHAR(50)        NULL,
    Username            NVARCHAR(100)       NOT NULL,
    PasswordHash        VARBINARY(256)      NOT NULL,
    PasswordSalt        VARBINARY(128)      NOT NULL,
    FullName            NVARCHAR(200)       NOT NULL,
    Email               NVARCHAR(256)       NOT NULL,
    Phone               NVARCHAR(30)        NULL,
    Designation         NVARCHAR(150)       NULL,
    Department          NVARCHAR(150)       NULL,
    ProfilePicturePath  NVARCHAR(500)       NULL,
    RoleId              INT                 NOT NULL,
    Status              TINYINT             NOT NULL CONSTRAINT DF_Users_Status DEFAULT (1), -- 1=Active,0=Inactive,2=Locked
    PasswordExpiryDate  DATETIME2           NULL,
    MustChangePassword  BIT                 NOT NULL CONSTRAINT DF_Users_MustChange DEFAULT (0),
    FailedLoginAttempts INT                 NOT NULL CONSTRAINT DF_Users_FailedAttempts DEFAULT (0),
    IsLocked            BIT                 NOT NULL CONSTRAINT DF_Users_IsLocked DEFAULT (0),
    LockoutEnd          DATETIME2           NULL,
    LastLogin           DATETIME2           NULL,
    LoginCount          INT                 NOT NULL CONSTRAINT DF_Users_LoginCount DEFAULT (0),
    IsDeleted           BIT                 NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT (0),
    CreatedDate         DATETIME2           NOT NULL CONSTRAINT DF_Users_CreatedDate DEFAULT (SYSUTCDATETIME()),
    ModifiedDate        DATETIME2           NULL,
    CreatedBy           INT                 NULL,
    ModifiedBy          INT                 NULL,
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
);
GO
CREATE INDEX IX_Users_RoleId ON dbo.Users(RoleId) WHERE IsDeleted = 0;
CREATE INDEX IX_Users_Status ON dbo.Users(Status) WHERE IsDeleted = 0;
GO

/* ------------------------------------------------------------------
   Password History (prevents reuse)
   ------------------------------------------------------------------ */
CREATE TABLE dbo.PasswordHistory
(
    PasswordHistoryId  INT IDENTITY(1,1)  NOT NULL CONSTRAINT PK_PasswordHistory PRIMARY KEY,
    UserId              INT                NOT NULL,
    PasswordHash        VARBINARY(256)     NOT NULL,
    PasswordSalt        VARBINARY(128)     NOT NULL,
    CreatedDate          DATETIME2          NOT NULL CONSTRAINT DF_PwdHistory_CreatedDate DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_PasswordHistory_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO
CREATE INDEX IX_PasswordHistory_UserId ON dbo.PasswordHistory(UserId, CreatedDate DESC);
GO

/* ------------------------------------------------------------------
   Session Tokens (opaque API session tokens — only SHA-256 hashes are
   stored, never the plaintext token; JWT is not used anywhere)
   ------------------------------------------------------------------ */
CREATE TABLE dbo.SessionTokens
(
    SessionTokenId  INT IDENTITY(1,1)  NOT NULL CONSTRAINT PK_SessionTokens PRIMARY KEY,
    UserId          INT                 NOT NULL,
    TokenHash       NVARCHAR(64)        NOT NULL,
    ExpiryDate      DATETIME2           NOT NULL,
    CreatedDate     DATETIME2           NOT NULL CONSTRAINT DF_SessionTokens_CreatedDate DEFAULT (SYSUTCDATETIME()),
    CreatedByIp     NVARCHAR(50)        NULL,
    UserAgent       NVARCHAR(300)       NULL,
    IsRevoked       BIT                 NOT NULL CONSTRAINT DF_SessionTokens_IsRevoked DEFAULT (0),
    RevokedDate     DATETIME2           NULL,
    CONSTRAINT FK_SessionTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT UQ_SessionTokens_TokenHash UNIQUE (TokenHash)
);
GO
CREATE INDEX IX_SessionTokens_UserId ON dbo.SessionTokens(UserId);
GO

/* ------------------------------------------------------------------
   Menus (self-referencing tree)
   ------------------------------------------------------------------ */
CREATE TABLE dbo.Menus
(
    MenuId          INT IDENTITY(1,1)  NOT NULL CONSTRAINT PK_Menus PRIMARY KEY,
    ParentMenuId    INT                 NULL,
    MenuName        NVARCHAR(150)       NOT NULL,
    MenuIcon        NVARCHAR(100)       NULL,
    ControllerPage  NVARCHAR(200)       NULL,
    Route           NVARCHAR(300)       NULL,
    MenuOrder       INT                 NOT NULL CONSTRAINT DF_Menus_Order DEFAULT (0),
    Status          BIT                 NOT NULL CONSTRAINT DF_Menus_Status DEFAULT (1),
    IsDeleted       BIT                 NOT NULL CONSTRAINT DF_Menus_IsDeleted DEFAULT (0),
    CreatedDate     DATETIME2           NOT NULL CONSTRAINT DF_Menus_CreatedDate DEFAULT (SYSUTCDATETIME()),
    ModifiedDate    DATETIME2           NULL,
    CreatedBy       INT                 NULL,
    ModifiedBy      INT                 NULL,
    CONSTRAINT FK_Menus_Parent FOREIGN KEY (ParentMenuId) REFERENCES dbo.Menus(MenuId)
);
GO
CREATE INDEX IX_Menus_ParentMenuId ON dbo.Menus(ParentMenuId) WHERE IsDeleted = 0;
GO

/* ------------------------------------------------------------------
   Permission Types (lookup: View/Add/Edit/Delete/Print/Export/Approve/Reject)
   ------------------------------------------------------------------ */
CREATE TABLE dbo.PermissionTypes
(
    PermissionTypeId   INT IDENTITY(1,1)  NOT NULL CONSTRAINT PK_PermissionTypes PRIMARY KEY,
    Code                NVARCHAR(30)       NOT NULL,
    Name                 NVARCHAR(100)      NOT NULL,
    CONSTRAINT UQ_PermissionTypes_Code UNIQUE (Code)
);
GO

/* ------------------------------------------------------------------
   Role -> Menu -> Permission matrix
   ------------------------------------------------------------------ */
CREATE TABLE dbo.RoleMenuPermissions
(
    RoleMenuPermissionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RoleMenuPermissions PRIMARY KEY,
    RoleId                INT               NOT NULL,
    MenuId                INT               NOT NULL,
    PermissionTypeId      INT               NOT NULL,
    CreatedDate           DATETIME2         NOT NULL CONSTRAINT DF_RMP_CreatedDate DEFAULT (SYSUTCDATETIME()),
    CreatedBy             INT               NULL,
    CONSTRAINT FK_RMP_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId),
    CONSTRAINT FK_RMP_Menus FOREIGN KEY (MenuId) REFERENCES dbo.Menus(MenuId),
    CONSTRAINT FK_RMP_PermissionTypes FOREIGN KEY (PermissionTypeId) REFERENCES dbo.PermissionTypes(PermissionTypeId),
    CONSTRAINT UQ_RMP UNIQUE (RoleId, MenuId, PermissionTypeId)
);
GO
CREATE INDEX IX_RMP_RoleId ON dbo.RoleMenuPermissions(RoleId);
CREATE INDEX IX_RMP_MenuId ON dbo.RoleMenuPermissions(MenuId);
GO

/* ------------------------------------------------------------------
   Optional per-user menu/permission overrides (User Mapping)
   ------------------------------------------------------------------ */
CREATE TABLE dbo.UserMenuPermissions
(
    UserMenuPermissionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserMenuPermissions PRIMARY KEY,
    UserId                INT               NOT NULL,
    MenuId                INT               NOT NULL,
    PermissionTypeId      INT               NOT NULL,
    IsGranted             BIT               NOT NULL CONSTRAINT DF_UMP_IsGranted DEFAULT (1), -- allows explicit deny (0) to override role grant
    CreatedDate           DATETIME2         NOT NULL CONSTRAINT DF_UMP_CreatedDate DEFAULT (SYSUTCDATETIME()),
    CreatedBy             INT               NULL,
    CONSTRAINT FK_UMP_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_UMP_Menus FOREIGN KEY (MenuId) REFERENCES dbo.Menus(MenuId),
    CONSTRAINT FK_UMP_PermissionTypes FOREIGN KEY (PermissionTypeId) REFERENCES dbo.PermissionTypes(PermissionTypeId),
    CONSTRAINT UQ_UMP UNIQUE (UserId, MenuId, PermissionTypeId)
);
GO

/* ------------------------------------------------------------------
   User Preferences (theme, sidebar, language, layout, landing page)
   ------------------------------------------------------------------ */
CREATE TABLE dbo.UserPreferences
(
    UserId              INT             NOT NULL CONSTRAINT PK_UserPreferences PRIMARY KEY,
    Theme               NVARCHAR(20)    NOT NULL CONSTRAINT DF_UserPreferences_Theme DEFAULT ('light'), -- light/dark/auto
    SidebarCollapsed    BIT             NOT NULL CONSTRAINT DF_UserPreferences_Sidebar DEFAULT (0),
    Language             NVARCHAR(10)    NOT NULL CONSTRAINT DF_UserPreferences_Language DEFAULT ('en'),
    DashboardLayout      NVARCHAR(50)    NULL,
    LandingPage           NVARCHAR(300)   NULL,
    ModifiedDate           DATETIME2       NOT NULL CONSTRAINT DF_UserPreferences_ModifiedDate DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_UserPreferences_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO

/* ------------------------------------------------------------------
   Audit Log
   ------------------------------------------------------------------ */
CREATE TABLE dbo.AuditLogs
(
    AuditLogId  BIGINT IDENTITY(1,1)   NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
    UserId      INT                     NULL,
    Module      NVARCHAR(100)           NOT NULL,
    Action      NVARCHAR(100)           NOT NULL,
    OldValue    NVARCHAR(MAX)           NULL,
    NewValue    NVARCHAR(MAX)           NULL,
    Browser     NVARCHAR(300)           NULL,
    IPAddress   NVARCHAR(50)            NULL,
    ActionDate  DATETIME2               NOT NULL CONSTRAINT DF_AuditLogs_ActionDate DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO
CREATE INDEX IX_AuditLogs_UserId ON dbo.AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_ActionDate ON dbo.AuditLogs(ActionDate DESC);
CREATE INDEX IX_AuditLogs_Module ON dbo.AuditLogs(Module);
GO
