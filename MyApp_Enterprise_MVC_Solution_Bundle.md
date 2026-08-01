# Enterprise ASP.NET Core MVC (.NET 8) — Complete Production-Ready Architecture & Implementation Guide

**Author:** Principal Software Architect & Senior ASP.NET Core (.NET 8) Developer  
**Date:** 2026-08-02  
**Target Platform:** ASP.NET Core .NET 8, Microsoft SQL Server 2019+, Bootstrap 5  
**Deliverable Format:** Standalone Single-File Downloadable Technical Reference & Architecture Specification

---

## Table of Contents
1. [Executive Summary & Primary Goal](#1-executive-summary--primary-goal)
2. [Architectural Patterns & Core Rules](#2-architectural-patterns--core-rules)
3. [Complete Solution & Project Structures](#3-complete-solution--project-structures)
   - [MyApp.sln](#31-myappsln-two-project-structure)
   - [MyApp.Api Structure](#32-myappapi-project-structure)
   - [MyApp.Web Structure](#33-myappweb-project-structure)
4. [End-to-End Request Flow](#4-end-to-end-request-flow)
5. [Database Access & Stored Procedure Specifications](#5-database-access--stored-procedure-specifications)
6. [Unified API Response Model & Global Exception Handling](#6-unified-api-response-model--global-exception-handling)
7. [Authentication, Authorization & Security Implementation](#7-authentication-authorization--security-implementation)
8. [Module-by-Module Architectural Specifications](#8-module-by-module-architectural-specifications)
   - [Authentication Module](#81-authentication-module)
   - [User Management Module](#82-user-management-module)
   - [Role & Privilege Management Module](#83-role--privilege-management-module)
   - [Navigation Menus Module](#84-navigation-menus-module)
   - [Granular Role Permission Matrix Module](#85-granular-role-permission-matrix-module)
   - [User Profile & Preferences Module](#86-user-profile--preferences-module)
   - [Executive Dashboard Module](#87-executive-dashboard-module)
   - [System Audit Logs Module](#88-system-audit-logs-module)
9. [Core Layer Implementations (Full Code Listing)](#9-core-layer-implementations-full-code-listing)
   - [ApiResponse&lt;T&gt; & PagedResult&lt;T&gt;](#91-apiresponset--pagedresultt)
   - [ADO.NET SqlDataAccess & Connection Factory](#92-adonet-sqldataaccess--connection-factory)
   - [Typed ApiClient (HTTP Communication)](#93-typed-apiclient-http-communication)
   - [Global Exception Middlewares (API & Web)](#94-global-exception-middlewares-api--web)
   - [Dependency Injection Registration Extensions](#95-dependency-injection-registration-extensions)
   - [Program.cs (API & Web Pipelines)](#96-programcs-api--web-pipelines)
10. [Downloadable Solution Archive Instruction](#10-downloadable-solution-archive-instruction)

---

## 1. Executive Summary & Primary Goal

This deliverable contains the complete, enterprise-level architecture and implementation guide for a production-ready **ASP.NET Core MVC (.NET 8)** application.

### Primary Constraints Achieved
1. **Only Two Projects**:
   ```
   Solution (MyApp.sln)
   │
   ├── MyApp.Api    (ASP.NET Core 8 Web API — ADO.NET + Stored Procedures)
   │
   └── MyApp.Web    (ASP.NET Core 8 MVC Application — NO Razor Pages)
   ```
2. **Zero Direct Database Connections from Web**:
   - `MyApp.Web` **never** opens a `SqlConnection` or references any database client library.
   - Every operation is dispatched to `MyApp.Api` via a centralized, typed `IApiClient` service using `IHttpClientFactory`.
3. **Strict Layer Separation & Thin Controllers**:
   - **Controllers** receive HTTP requests and call Service interfaces only. Zero business logic or database access exists in controllers.
   - **Services** implement 100% of application logic, password policies, validation coordination, and audit logging.
   - **Repositories** implement 100% of database access using ADO.NET and stored procedures. Zero business logic exists in repositories.

---

## 2. Architectural Patterns & Core Rules

### Architectural Principles Applied
- **MVC Pattern**: Model-View-Controller in the Web front end; API Controller-Service-Repository in the API backend.
- **Repository Pattern**: Extends testability and decouples data access from business logic.
- **Service Layer**: Explicit `Services/Interfaces/` and `Services/Implementations/` in both API and Web projects.
- **Dependency Injection**: Registered cleanly via constructor injection using Microsoft's standard `IServiceCollection`.
- **SOLID Principles**:
  - *Single Responsibility*: Controllers route, Services compute/validate, Repositories query, Middlewares intercept.
  - *Open/Closed*: Functionality is extended via new interfaces and extension methods without altering core engines.
  - *Liskov Substitution*: Implementations are substitutable for their interfaces (`IUserService`, `IUserRepository`, etc.).
  - *Interface Segregation*: Separate focused interfaces per domain module.
  - *Dependency Inversion*: High-level controllers depend on service abstractions, never concretions.
- **Clean Code & Asynchrony**: All I/O operations across HTTP, file systems, and database layers use `async`/`await` with cancellation tokens.

---

## 3. Complete Solution & Project Structures

### 3.1 `MyApp.sln` (Two-Project Structure)
```
/home/user/usermangement/
├── MyApp.sln
├── README.md
├── database/               # SQL Server Schema, Seed Data, and 9 Stored Procedure files
│   ├── 01_Schema.sql
│   ├── 02_SeedData.sql
│   ├── 03_SP_Auth.sql
│   ├── 04_SP_Users.sql
│   ├── 05_SP_Roles.sql
│   ├── 06_SP_Menus.sql
│   ├── 07_SP_Permissions.sql
│   ├── 08_SP_Dashboard_Audit.sql
│   └── 09_SP_Profile.sql
├── MyApp.Api/              # Backend Web API (.NET 8)
└── MyApp.Web/              # Traditional ASP.NET Core MVC Application (.NET 8)
```

### 3.2 `MyApp.Api` Project Structure
```
MyApp.Api/
├── Controllers/
│   ├── AuthController.cs          # Login, Refresh, Logout, Forgot/Reset/Change Password
│   ├── UsersController.cs         # Search, GetById, Create, Update, Delete, Status, Prefs
│   ├── RolesController.cs         # GetAll, GetById, Save, Delete, Clone, Assign
│   ├── MenusController.cs         # GetHierarchy, GetMine, Save, Delete
│   ├── PermissionsController.cs   # GetTypes, GetMatrix, Assign, Check
│   ├── ProfileController.cs       # Get, Update, GetPreferences, UpdatePreferences, ChangePassword
│   ├── DashboardController.cs     # GetSummary
│   └── AuditLogsController.cs     # Search, Log
├── Repository/
│   ├── Interfaces/
│   │   ├── IAuthRepository.cs
│   │   ├── IUserRepository.cs
│   │   ├── IRoleRepository.cs
│   │   ├── IMenuRepository.cs
│   │   ├── IPermissionRepository.cs
│   │   ├── IProfileRepository.cs
│   │   ├── IDashboardRepository.cs
│   │   └── IAuditLogRepository.cs
│   └── Implementations/
│       ├── AuthRepository.cs      # Executes dbo.sp_Login, dbo.sp_ChangePassword, etc.
│       ├── UserRepository.cs      # Executes dbo.sp_CreateUser, dbo.sp_GetUsers, etc.
│       ├── RoleRepository.cs      # Executes dbo.sp_SaveRole, dbo.sp_CloneRole, etc.
│       ├── MenuRepository.cs      # Executes dbo.sp_GetMenus, dbo.sp_GetUserMenus, etc.
│       ├── PermissionRepository.cs# Executes dbo.sp_GetRolePermissions, dbo.sp_AssignPermissions
│       ├── ProfileRepository.cs   # Executes dbo.sp_GetProfile, dbo.sp_UpdateProfile
│       ├── DashboardRepository.cs # Executes dbo.sp_GetDashboard
│       └── AuditLogRepository.cs  # Executes dbo.sp_InsertAuditLog, dbo.sp_GetAuditLogs
├── Services/
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── IUserService.cs
│   │   ├── IRoleService.cs
│   │   ├── IMenuService.cs
│   │   ├── IPermissionService.cs
│   │   ├── IProfileService.cs
│   │   ├── IDashboardService.cs
│   │   ├── IAuditService.cs
│   │   └── ICurrentUserService.cs
│   └── Implementations/
│       ├── AuthService.cs         # All authentication, token issuance & lockout rules
│       ├── UserService.cs         # All user CRUD & password hashing validation
│       ├── RoleService.cs         # All role rules & system role protections
│       ├── MenuService.cs         # All menu hierarchy logic
│       ├── PermissionService.cs   # All granular RBAC checks
│       ├── ProfileService.cs      # All user profile & preference management
│       ├── DashboardService.cs    # All KPI aggregations
│       ├── AuditService.cs        # All audit record formatting & search
│       └── CurrentUserService.cs  # ClaimsPrincipal accessor
├── Models/
│   ├── User.cs                    # Domain entity for dbo.Users
│   ├── Role.cs                    # Domain entity for dbo.Roles
│   ├── Menu.cs                    # Domain entity for dbo.Menus
│   ├── PermissionType.cs          # Domain entity for dbo.PermissionTypes
│   ├── RefreshToken.cs            # Domain entity for dbo.RefreshTokens
│   ├── UserPreferences.cs         # Domain entity for dbo.UserPreferences
│   └── AuditLog.cs                # Domain entity for dbo.AuditLogs
├── DTOs/
│   ├── AuthDtos.cs                # LoginRequest, LoginResponse, ResetPasswordRequest, etc.
│   ├── UserDtos.cs                # CreateUserRequest, UpdateUserRequest, UserListItemDto, etc.
│   ├── RoleDtos.cs                # RoleItem, SaveRoleRequest, CloneRoleRequest, etc.
│   ├── MenuDtos.cs                # MenuNode, SaveMenuRequest, etc.
│   ├── PermissionDtos.cs          # AssignPermissionsRequest, RolePermissionRow, etc.
│   ├── ProfileDtos.cs             # ProfileDto, UpdateProfileDto, UpdatePreferencesDto
│   └── DashboardDtos.cs           # DashboardSummary, ActivityLogDto
├── Data/
│   ├── ISqlConnectionFactory.cs   # Interface for SQL connection creation
│   ├── SqlConnectionFactory.cs    # ADO.NET SqlConnection factory implementation
│   ├── SqlDataAccess.cs           # Core ADO.NET command execution helper
│   └── SqlDataReaderExtensions.cs # Null-safe reader extensions
├── Helpers/
│   └── PasswordPolicyHelper.cs    # Password complexity and history validation
├── Utilities/
│   └── DateTimeUtility.cs         # UTC date formatting utilities
├── Extensions/
│   └── ServiceCollectionExtensions.cs # DI registration for all API Services & Repositories
├── Exceptions/
│   └── AppException.cs            # Custom exceptions (NotFound, Unauthorized, Forbidden)
├── Middlewares/
│   └── GlobalExceptionMiddleware.cs # Catches unhandled exceptions -> ApiResponse<T>
├── Filters/
│   └── ValidateModelAttribute.cs  # Intercepts invalid ModelState -> ApiResponse<T>
├── Security/
│   ├── IJwtTokenGenerator.cs
│   ├── JwtTokenGenerator.cs       # JWT Access + Refresh token generator
│   ├── IPasswordHasher.cs
│   ├── PasswordHasher.cs          # PBKDF2 password hasher with cryptographic salt
│   └── JwtSettings.cs             # Strongly typed JWT options
├── Validators/
│   └── Validators.cs              # Custom request validators
├── Configurations/
│   └── DatabaseSettings.cs        # Database connection options
├── Common/
│   ├── ApiResponse.cs             # Standard ApiResponse<T> envelope
│   ├── PagedResult.cs             # Paging metadata and results container
│   └── UserStatus.cs              # Active=1, Inactive=0, Locked=2
└── Program.cs
```

### 3.3 `MyApp.Web` Project Structure
```
MyApp.Web/
├── Controllers/
│   ├── AuthController.cs          # Login, Logout, Forgot/Reset/Change Password
│   ├── DashboardController.cs     # Executive Dashboard view controller
│   ├── UsersController.cs         # User list, create, edit, delete, status toggle
│   ├── RolesController.cs         # Role list, create, edit, delete, clone
│   ├── MenusController.cs         # Menu tree view, create, edit, delete
│   ├── PermissionsController.cs   # Role permission matrix view & AJAX assign
│   ├── ProfileController.cs       # Profile display, edit, change password
│   ├── SettingsController.cs      # Theme switcher, language, sidebar preferences
│   ├── ErrorController.cs         # Centralized /Error, /Error/404, /Error/AccessDenied
│   └── HomeController.cs          # Root redirector to /Dashboard or /Auth/Login
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml         # Full enterprise dashboard layout
│   │   ├── _AuthLayout.cshtml     # Centered authentication layout
│   │   ├── _Navbar.cshtml         # Header navbar with theme toggle & user profile menu
│   │   ├── _Sidebar.cshtml        # Sidebar containing dynamic component menu
│   │   ├── _Footer.cshtml         # Footer
│   │   ├── _Alerts.cshtml         # Responsive Bootstrap success/error toast/alerts
│   │   ├── _Pagination.cshtml     # Standard pagination component
│   │   └── Components/SidebarMenu/
│   │       ├── Default.cshtml     # Root sidebar menu component view
│   │       └── _MenuNode.cshtml   # Recursive partial view for nested submenus
│   ├── Dashboard/
│   │   └── Index.cshtml           # Executive dashboard KPIs, charts, and audit table
│   ├── Users/
│   │   ├── Index.cshtml           # AJAX filterable user list table with status badges
│   │   ├── Create.cshtml          # User account creation form
│   │   └── Edit.cshtml            # User account editing form
│   ├── Roles/
│   │   ├── Index.cshtml           # Roles list with badge indicators and clone links
│   │   ├── Create.cshtml          # Role creation form
│   │   ├── Edit.cshtml            # Role editing form
│   │   └── Clone.cshtml           # Role cloning form
│   ├── Menus/
│   │   ├── Index.cshtml           # Hierarchical table view of navigation menus
│   │   ├── Create.cshtml          # Menu node creation form
│   │   └── Edit.cshtml            # Menu node editing form
│   ├── Permissions/
│   │   └── Index.cshtml           # Interactive matrix of modules x permission types
│   ├── Profile/
│   │   ├── Index.cshtml           # User personal profile card
│   │   ├── Edit.cshtml            # Profile editing form
│   │   └── ChangePassword.cshtml  # Account password change form
│   ├── Settings/
│   │   └── Index.cshtml           # Theme switcher (Light/Dark/Auto) & language options
│   ├── Error/
│   │   ├── Index.cshtml           # User-friendly error page with Request ID & Reference No.
│   │   ├── 404.cshtml             # 404 Not Found page
│   │   └── AccessDenied.cshtml    # 403 Access Denied page
│   └── Auth/
│       ├── Login.cshtml           # Login screen with Remember Me & lockout alerts
│       ├── ForgotPassword.cshtml  # Password recovery request screen
│       └── ResetPassword.cshtml   # Password reset form
├── Models/
│   ├── AuthModels.cs              # Client-side UI models for authentication
│   ├── DashboardModels.cs         # StatCard, RecentActivity, DashboardSummary
│   ├── UserModels.cs              # UserListItem, UserDetail, CreateUserModel, etc.
│   ├── RoleModels.cs              # RoleItem, SaveRoleModel, CloneRoleModel
│   ├── MenuModels.cs              # MenuItem, SaveMenuModel
│   ├── PermissionModels.cs        # PermissionType, PermissionMatrixCell, etc.
│   ├── ProfileModels.cs           # ProfileModel, UpdateProfileModel, UserPreferencesModel
│   └── AuditModels.cs             # AuditLogItem, AuditLogSearchModel
├── ViewModels/
│   └── WebViewModels.cs           # LoginViewModel, UserListViewModel, PermissionMatrixViewModel, etc.
├── DTOs/
│   └── WebDtos.cs                 # Transfer DTOs matching API JSON shapes
├── Services/
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── IUserService.cs
│   │   ├── IRoleService.cs
│   │   ├── IMenuService.cs
│   │   ├── IPermissionService.cs
│   │   ├── IProfileService.cs
│   │   ├── IDashboardService.cs
│   │   ├── IAuditService.cs
│   │   └── ICurrentUserService.cs
│   └── Implementations/
│       ├── AuthService.cs         # Calls API /auth/login, sets Session & Cookie
│       ├── UserService.cs         # Calls API /users endpoints via ApiClient
│       ├── RoleService.cs         # Calls API /roles endpoints via ApiClient
│       ├── MenuService.cs         # Calls API /menus endpoints via ApiClient
│       ├── PermissionService.cs   # Calls API /permissions endpoints via ApiClient
│       ├── ProfileService.cs      # Calls API /profile endpoints via ApiClient
│       ├── DashboardService.cs    # Calls API /dashboard endpoints via ApiClient
│       ├── AuditService.cs        # Calls API /auditlogs endpoints via ApiClient
│       └── CurrentUserService.cs  # Inspects ISession and HttpContext
├── ApiClient/
│   ├── IApiClient.cs              # Typed HTTP client interface
│   └── ApiClient.cs               # Handles Bearer token injection and automatic token refresh
├── Helpers/
│   └── WebHelpers.cs              # CookieHelper, SessionHelper, HtmlHelperExtensions
├── Extensions/
│   └── ServiceCollectionExtensions.cs # DI registration for all Web Services, ApiClient & Auth Cookies
├── Common/
│   ├── ApiResponse.cs             # Standard ApiResponse<T> envelope matching API
│   ├── PagedResult.cs             # Paging metadata container
│   └── SessionKeys.cs             # AccessToken, RefreshToken, UserId, RoleName constants
├── Filters/
│   ├── AuthGuardFilter.cs         # Validates access token in session -> bounces to /Auth/Login
│   └── AdminOnlyFilter.cs         # Protects administrator screens client-side
├── Middlewares/
│   └── GlobalExceptionMiddleware.cs # Intercepts Web exceptions -> redirects to /Error
├── Components/
│   └── SidebarMenuViewComponent.cs # Dynamically renders sidebar menu from IMenuService
├── TagHelpers/
│   ├── PermissionTagHelper.cs     # <permission-guard module="..." action="..." />
│   └── PaginationTagHelper.cs     # <pagination meta="@Model.Pagination" url="..." />
├── wwwroot/
│   ├── css/
│   │   └── site.css               # Dynamic CSS variables for Light, Dark, and Auto themes
│   └── js/
│       └── site.js                # Theme restoration, sidebar collapse, toasts, modals
└── Program.cs
```

---

## 4. End-to-End Request Flow

Every request initiated from an MVC page or AJAX script follows this strict architecture:

```
+-------------------------------------------------------------------------------+
|                                 BROWSER                                       |
|  User clicks "Save Role" -> SUBMIT POST /Roles/Edit/5                         |
+-------------------------------------------------------------------------------+
                                      │
                                      ▼
+-------------------------------------------------------------------------------+
|                       MVC WEB CONTROLLER (MyApp.Web)                          |
|  RolesController.Edit(5, model)                                               |
|  • Injects IRoleService ONLY. Zero database code.                             |
|  • Calls: await _roleService.SaveAsync(dto)                                   |
+-------------------------------------------------------------------------------+
                                      │
                                      ▼
+-------------------------------------------------------------------------------+
|                       WEB SERVICE LAYER (MyApp.Web)                           |
|  RoleService.SaveAsync(dto) (implements IRoleService)                         |
|  • Calls: await _api.PostAsync<SaveRoleRequestDto, int>("api/roles", dto)     |
+-------------------------------------------------------------------------------+
                                      │
                                      ▼
+-------------------------------------------------------------------------------+
|                      CENTRALIZED API CLIENT (MyApp.Web)                       |
|  ApiClient.PostAsync (implements IApiClient using IHttpClientFactory)         |
|  • Inspects ISession["AccessToken"]; silently exchanges refresh token if stale |
|  • Adds HTTP Header: Authorization: Bearer <jwt-access-token>                 |
|  • Executes HTTPS request to MyApp.Api                                        |
+-------------------------------------------------------------------------------+
                                      │
                         (HTTPS JSON over Local/Network)
                                      │
                                      ▼
+-------------------------------------------------------------------------------+
|                         API CONTROLLER (MyApp.Api)                            |
|  RolesController.Save(request)                                                |
|  • Validates JWT token & [Authorize] permission                               |
|  • Injects IRoleService ONLY. Zero business logic or database queries.        |
|  • Calls: await _roleService.SaveAsync(request, currentUserId)                |
+-------------------------------------------------------------------------------+
                                      │
                                      ▼
+-------------------------------------------------------------------------------+
|                       API SERVICE LAYER (MyApp.Api)                           |
|  RoleService.SaveAsync (implements IRoleService)                              |
|  • Contains ALL Business Logic: checks if system role, validates naming rules  |
|  • Calls: await _roles.SaveAsync(request, currentUserId)                      |
|  • Calls: await _auditLog.InsertAsync(...) to record audit trail              |
+-------------------------------------------------------------------------------+
                                      │
                                      ▼
+-------------------------------------------------------------------------------+
|                     REPOSITORY LAYER (MyApp.Api)                              |
|  RoleRepository.SaveAsync (implements IRoleRepository)                        |
|  • Contains ONLY Database Operations: no business logic                       |
|  • Uses ADO.NET: _db.ExecuteScalarAsync("dbo.sp_SaveRole", params)            |
+-------------------------------------------------------------------------------+
                                      │
                                      ▼
+-------------------------------------------------------------------------------+
|                    MICROSOFT SQL SERVER 2019+ (MyAppDb)                       |
|  Stored Procedure: dbo.sp_SaveRole                                            |
|  • Uses SET NOCOUNT ON, SET XACT_ABORT ON, BEGIN TRANSACTION                  |
|  • Inserts/Updates dbo.Roles table and returns SCOPE_IDENTITY()               |
+-------------------------------------------------------------------------------+
```

---

## 5. Database Access & Stored Procedure Specifications

### 5.1 Strict ADO.NET Constraints
- **Zero ORM Usage**: No Entity Framework, EF Core, Dapper, LINQ to SQL, or raw SQL queries are allowed.
- **Parametric SQL**: Every database query is executed by invoking a Stored Procedure (`CommandType.StoredProcedure`) via `SqlCommand` and `SqlParameter` to prevent SQL injection.
- **Null Safety**: All data readers use defensive extension methods (`reader.GetNullableString(...)`, `reader.GetNullableDateTime(...)`) to prevent `DBNull` cast exceptions.

### 5.2 Stored Procedure Catalog (`database/01` to `09`)
| Script File | Stored Procedure Name | Primary Responsibility |
| :--- | :--- | :--- |
| `03_SP_Auth.sql` | `dbo.sp_Login` | Authenticates username, returns PasswordHash, Salt, lock status, and role details. |
| `03_SP_Auth.sql` | `dbo.sp_RecordLoginSuccess` | Resets failed attempts to 0 and updates `LastLogin` timestamp. |
| `03_SP_Auth.sql` | `dbo.sp_RecordLoginFailure` | Increments failure attempt count; locks account if attempts exceed threshold. |
| `03_SP_Auth.sql` | `dbo.sp_UnlockExpiredLockouts` | Unlocks accounts whose `LockoutEnd` timestamp has passed. |
| `03_SP_Auth.sql` | `dbo.sp_ChangePassword` | Updates password hash, salt, expiry date, and appends to `PasswordHistory`. |
| `03_SP_Auth.sql` | `dbo.sp_CheckPasswordHistory` | Returns last *N* password hashes to prevent password reuse. |
| `03_SP_Auth.sql` | `dbo.sp_CreatePasswordResetToken` | Generates a time-limited password reset token. |
| `03_SP_Auth.sql` | `dbo.sp_ValidatePasswordResetToken`| Validates token freshness and unconsumed state. |
| `03_SP_Auth.sql` | `dbo.sp_ConsumePasswordResetToken` | Marks reset token as used. |
| `03_SP_Auth.sql` | `dbo.sp_SaveRefreshToken` | Persists JWT refresh token with expiry and client IP. |
| `03_SP_Auth.sql` | `dbo.sp_GetRefreshToken` | Fetches active refresh token for rotation. |
| `03_SP_Auth.sql` | `dbo.sp_RevokeRefreshToken` | Revokes an existing refresh token and logs revocation IP. |
| `04_SP_Users.sql` | `dbo.sp_CreateUser` | Inserts user account with hashed password and role assignment. |
| `04_SP_Users.sql` | `dbo.sp_UpdateUser` | Updates personal info, designation, department, and role ID. |
| `04_SP_Users.sql` | `dbo.sp_DeleteUser` | Soft-deletes user account (`IsDeleted = 1`). |
| `04_SP_Users.sql` | `dbo.sp_SetUserStatus` | Modifies account status (`1 = Active, 0 = Inactive, 2 = Locked`). |
| `04_SP_Users.sql` | `dbo.sp_GetUserById` | Returns full user profile, role name, and login statistics. |
| `04_SP_Users.sql` | `dbo.sp_GetUsers` | Searches, filters, sorts, and paginates users in a single round-trip. |
| `04_SP_Users.sql` | `dbo.sp_GetUserPreferences` | Reads user theme, language, and sidebar preferences. |
| `04_SP_Users.sql` | `dbo.sp_SaveUserPreferences` | Upserts user UI preferences. |
| `05_SP_Roles.sql` | `dbo.sp_SaveRole` | Inserts or updates a security role. |
| `05_SP_Roles.sql` | `dbo.sp_GetRoles` | Returns all active roles and assigned user counts. |
| `05_SP_Roles.sql` | `dbo.sp_GetRoleById` | Returns role details by ID. |
| `05_SP_Roles.sql` | `dbo.sp_DeleteRole` | Soft-deletes custom roles; prevents deleting system roles. |
| `05_SP_Roles.sql` | `dbo.sp_CloneRole` | Clones a role along with its complete permission matrix. |
| `05_SP_Roles.sql` | `dbo.sp_AssignRole` | Reassigns a user to a new role ID. |
| `06_SP_Menus.sql` | `dbo.sp_GetMenus` | Returns complete navigation menu tree for admin editing. |
| `06_SP_Menus.sql` | `dbo.sp_GetUserMenus` | Returns dynamic sidebar menu tree filtered by user permissions. |
| `06_SP_Menus.sql` | `dbo.sp_SaveMenu` | Inserts or updates navigation menu nodes. |
| `06_SP_Menus.sql` | `dbo.sp_DeleteMenu` | Soft-deletes menu node and any child items. |
| `07_SP_Permissions.sql`| `dbo.sp_GetPermissionTypes` | Returns permission codes (`VIEW, CREATE, UPDATE, DELETE, EXPORT, AUDIT`). |
| `07_SP_Permissions.sql`| `dbo.sp_GetRolePermissions` | Returns cross-join permission matrix of all menu nodes x permission types. |
| `07_SP_Permissions.sql`| `dbo.sp_AssignPermissions` | Bulk-updates role permissions using Table-Valued Parameters (TVP). |
| `07_SP_Permissions.sql`| `dbo.sp_UserHasPermission` | High-speed single-permission check by module and action. |
| `08_SP_Dashboard_Audit.sql` | `dbo.sp_GetDashboard` | Aggregates user counts, role counts, recent logins, and audit logs. |
| `08_SP_Dashboard_Audit.sql` | `dbo.sp_InsertAuditLog` | Inserts immutable audit log record with old/new JSON snapshots. |
| `08_SP_Dashboard_Audit.sql` | `dbo.sp_GetAuditLogs` | Paginated and filterable search across system audit logs. |
| `09_SP_Profile.sql` | `dbo.sp_GetProfile` | Returns personal profile info and preferences for current user. |
| `09_SP_Profile.sql` | `dbo.sp_UpdateProfile` | Updates personal profile fields (`FullName, Email, Phone, Designation`). |

---

## 6. Unified API Response Model & Global Exception Handling

### 6.1 Standard `ApiResponse<T>` Contract
Every endpoint in `MyApp.Api` returns the exact same JSON response envelope:
```json
{
  "success": true,
  "statusCode": 200,
  "message": "User created successfully.",
  "data": 1042,
  "errors": null,
  "pagination": null,
  "traceId": "0HN6F8E92K001"
}
```

### 6.2 Global Exception Handling Pipeline
1. **Repository Layer**: Throws ADO.NET or database connectivity errors without catching.
2. **Service Layer**: Throws domain exceptions (`NotFoundException`, `ValidationException`, `UnauthorizedException`, `ForbiddenException`).
3. **Controller Layer**: Does not wrap code in try/catch blocks; delegates error propagation to middleware.
4. **API Middleware (`MyApp.Api.Middlewares.GlobalExceptionMiddleware`)**:
   - Catches unhandled exceptions, logs full stack trace to Serilog.
   - Maps domain exceptions to HTTP status codes (`400`, `401`, `403`, `404`); maps unexpected errors to `500`.
   - Sets `TraceId = context.TraceIdentifier` and writes standard JSON `ApiResponse<T>`.
5. **Web Middleware (`MyApp.Web.Middlewares.GlobalExceptionMiddleware`)**:
   - Intercepts unhandled page exceptions in the MVC front end.
   - Generates a Reference Number (`REF-yyyyMMdd-XXXX`) and captures Request Id.
   - Redirects standard browser requests to `/Error?requestId=...&referenceNumber=...`.
   - Returns standard JSON error envelope for AJAX/Fetch requests.
6. **Error Controller & Views (`Views/Error/Index.cshtml`)**:
   - Displays a friendly message, Request ID, and Reference Number.
   - **Never** displays technical stack traces to the end user.

---

## 7. Authentication, Authorization & Security Implementation

### 7.1 Enterprise Security Implementation Matrix

| Security Domain | Threat Mitigated | Technical Implementation |
| :--- | :--- | :--- |
| **SQL Injection** | Malicious SQL payload execution | 100% Parameterized queries via ADO.NET `SqlCommand` & stored procedures. Zero raw SQL. |
| **CSRF / XSRF** | Cross-Site Request Forgery | `[ValidateAntiForgeryToken]` on POST actions; `X-CSRF-TOKEN` header validated by ASP.NET Core Antiforgery for AJAX calls. |
| **XSS** | Cross-Site Scripting | Output encoding in Razor views; input sanitization in service layers; strict CSP-ready markup. |
| **Session Hijacking** | Access Token Interception | HTTPS only (`RequireHttpsMetadata = true`); secure HTTP-only cookies (`CookieSecurePolicy.Always`); `SameSiteMode.Strict`. |
| **Brute Force & DoS** | Credential Stuffing / Flooding | Account lockout after 5 failed login attempts (`dbo.sp_RecordLoginFailure`); IP rate limiting via `AspNetCoreRateLimit`. |
| **Parameter Tampering** | Unauthorized ID access / Elevation | ID verification against authenticated user claims; server-side permission checks (`IPermissionService.HasPermissionAsync`). |
| **Password Security** | Credential Crackability | PBKDF2 with HMAC-SHA256 and cryptographic salt (`IPasswordHasher`); password history verification against last 5 passwords. |
| **Session Timeout** | Idle Session Abandonment | 30-minute idle session expiration (`options.IdleTimeout = TimeSpan.FromMinutes(30)`); silent refresh token rotation. |
| **Role & RBAC** | Unauthorized Module Access | Server-side JWT `[Authorize(Roles = "...")]`; client-side `AdminOnlyFilter` & dynamic `SidebarMenuViewComponent`. |

---

## 8. Module-by-Module Architectural Specifications

### 8.1 Authentication Module
- **Endpoints**: `/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`, `/api/auth/forgot-password`, `/api/auth/reset-password`, `/api/auth/change-password`.
- **Services**: `IAuthService` -> `AuthService` (API and Web).
- **Repositories**: `IAuthRepository` -> `AuthRepository` (API).
- **Stored Procedures**: `dbo.sp_Login`, `dbo.sp_RecordLoginSuccess`, `dbo.sp_RecordLoginFailure`, `dbo.sp_UnlockExpiredLockouts`, `dbo.sp_ChangePassword`, `dbo.sp_CheckPasswordHistory`, `dbo.sp_CreatePasswordResetToken`, `dbo.sp_ValidatePasswordResetToken`, `dbo.sp_ConsumePasswordResetToken`, `dbo.sp_SaveRefreshToken`, `dbo.sp_GetRefreshToken`, `dbo.sp_RevokeRefreshToken`.
- **Web Views**: `Views/Auth/Login.cshtml`, `Views/Auth/ForgotPassword.cshtml`, `Views/Auth/ResetPassword.cshtml`.

### 8.2 User Management Module
- **Endpoints**: `/api/users` (GET/POST), `/api/users/{id}` (GET/PUT/DELETE), `/api/users/{id}/activate`, `/api/users/{id}/deactivate`, `/api/users/preferences`.
- **Services**: `IUserService` -> `UserService` (API and Web).
- **Repositories**: `IUserRepository` -> `UserRepository` (API).
- **Stored Procedures**: `dbo.sp_CreateUser`, `dbo.sp_UpdateUser`, `dbo.sp_DeleteUser`, `dbo.sp_SetUserStatus`, `dbo.sp_GetUserById`, `dbo.sp_GetUsers`, `dbo.sp_GetUserPreferences`, `dbo.sp_SaveUserPreferences`.
- **Web Views**: `Views/Users/Index.cshtml`, `Views/Users/Create.cshtml`, `Views/Users/Edit.cshtml`.

### 8.3 Role & Privilege Management Module
- **Endpoints**: `/api/roles` (GET/POST), `/api/roles/{id}` (GET/DELETE), `/api/roles/clone` (POST), `/api/roles/{userId}/assign/{roleId}` (PATCH).
- **Services**: `IRoleService` -> `RoleService` (API and Web).
- **Repositories**: `IRoleRepository` -> `RoleRepository` (API).
- **Stored Procedures**: `dbo.sp_SaveRole`, `dbo.sp_GetRoles`, `dbo.sp_GetRoleById`, `dbo.sp_DeleteRole`, `dbo.sp_CloneRole`, `dbo.sp_AssignRole`.
- **Web Views**: `Views/Roles/Index.cshtml`, `Views/Roles/Create.cshtml`, `Views/Roles/Edit.cshtml`, `Views/Roles/Clone.cshtml`.

### 8.4 Navigation Menus Module
- **Endpoints**: `/api/menus` (GET/POST), `/api/menus/mine` (GET), `/api/menus/{id}` (DELETE).
- **Services**: `IMenuService` -> `MenuService` (API and Web).
- **Repositories**: `IMenuRepository` -> `MenuRepository` (API).
- **Stored Procedures**: `dbo.sp_GetMenus`, `dbo.sp_GetUserMenus`, `dbo.sp_SaveMenu`, `dbo.sp_DeleteMenu`.
- **Web Views**: `Views/Menus/Index.cshtml`, `Views/Menus/Create.cshtml`, `Views/Menus/Edit.cshtml`, `Views/Shared/Components/SidebarMenu/Default.cshtml`.

### 8.5 Granular Role Permission Matrix Module
- **Endpoints**: `/api/permissions/types` (GET), `/api/permissions/matrix/{roleId}` (GET), `/api/permissions/assign` (POST), `/api/permissions/check` (GET).
- **Services**: `IPermissionService` -> `PermissionService` (API and Web).
- **Repositories**: `IPermissionRepository` -> `PermissionRepository` (API).
- **Stored Procedures**: `dbo.sp_GetPermissionTypes`, `dbo.sp_GetRolePermissions`, `dbo.sp_AssignPermissions`, `dbo.sp_UserHasPermission`.
- **Web Views**: `Views/Permissions/Index.cshtml` (Interactive matrix with Grant All / Revoke All and AJAX save).

### 8.6 User Profile & Preferences Module
- **Endpoints**: `/api/profile` (GET/PUT), `/api/profile/preferences` (GET/PUT), `/api/profile/change-password` (POST).
- **Services**: `IProfileService` -> `ProfileService` (API and Web).
- **Repositories**: `IProfileRepository` -> `ProfileRepository` (API).
- **Stored Procedures**: `dbo.sp_GetProfile`, `dbo.sp_UpdateProfile`.
- **Web Views**: `Views/Profile/Index.cshtml`, `Views/Profile/Edit.cshtml`, `Views/Profile/ChangePassword.cshtml`, `Views/Settings/Index.cshtml`.

### 8.7 Executive Dashboard Module
- **Endpoints**: `/api/dashboard` (GET).
- **Services**: `IDashboardService` -> `DashboardService` (API and Web).
- **Repositories**: `IDashboardRepository` -> `DashboardRepository` (API).
- **Stored Procedures**: `dbo.sp_GetDashboard`.
- **Web Views**: `Views/Dashboard/Index.cshtml` (4 StatCards + Recent Security Logins table + System Audit Feed table).

### 8.8 System Audit Logs Module
- **Endpoints**: `/api/auditlogs` (GET).
- **Services**: `IAuditService` -> `AuditService` (API and Web).
- **Repositories**: `IAuditLogRepository` -> `AuditLogRepository` (API).
- **Stored Procedures**: `dbo.sp_InsertAuditLog`, `dbo.sp_GetAuditLogs`.
- **Web Views**: Integrated into Dashboard and Admin search screens.

---

## 9. Core Layer Implementations (Full Code Listing)

### 9.1 `ApiResponse<T>` & `PagedResult<T>`
```csharp
// Common/ApiResponse.cs (Shared contract across both API and Web)
namespace MyApp.Api.Common; // (or MyApp.Web.Common in Web project)

public class PaginationMeta
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public PaginationMeta? Pagination { get; set; }
    public string? TraceId { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null, PaginationMeta? pagination = null) => new()
    {
        Success = true,
        StatusCode = 200,
        Message = message,
        Data = data,
        Pagination = pagination
    };

    public static ApiResponse<T> Fail(string message, int statusCode = 400, List<string>? errors = null) => new()
    {
        Success = false,
        StatusCode = statusCode,
        Message = message,
        Errors = errors ?? [message]
    };
}
```

### 9.2 ADO.NET `SqlDataAccess` & Connection Factory
```csharp
// Data/SqlDataAccess.cs (MyApp.Api — The single data engine used by all repositories)
using System.Data;
using Microsoft.Data.SqlClient;

namespace MyApp.Api.Data;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;
    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing.");
    }
    public SqlConnection CreateConnection() => new(_connectionString);
}

public class SqlDataAccess
{
    private readonly ISqlConnectionFactory _factory;
    public SqlDataAccess(ISqlConnectionFactory factory) => _factory = factory;

    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        return conn;
    }

    public static SqlCommand CreateCommand(SqlConnection connection, string procedureName, SqlTransaction? tx = null)
    {
        return new SqlCommand(procedureName, connection, tx)
        {
            CommandType = CommandType.StoredProcedure
        };
    }

    public static SqlParameter AddParam(SqlCommand command, string name, object? value)
    {
        var p = command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return p;
    }

    public async Task<int> ExecuteNonQueryAsync(string procedureName, Action<SqlCommand>? paramConfig = null, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = CreateCommand(conn, procedureName);
        paramConfig?.Invoke(cmd);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string procedureName, Action<SqlCommand>? paramConfig = null, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = CreateCommand(conn, procedureName);
        paramConfig?.Invoke(cmd);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result == DBNull.Value || result == null ? default : (T)result;
    }

    public async Task<List<T>> ExecuteReaderAsync<T>(string procedureName, Action<SqlCommand>? paramConfig, Func<SqlDataReader, T> mapper, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = CreateCommand(conn, procedureName);
        paramConfig?.Invoke(cmd);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(mapper(reader));
        }
        return list;
    }

    public async Task<T?> ExecuteReaderSingleAsync<T>(string procedureName, Action<SqlCommand>? paramConfig, Func<SqlDataReader, T> mapper, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = CreateCommand(conn, procedureName);
        paramConfig?.Invoke(cmd);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return mapper(reader);
        }
        return default;
    }
}
```

### 9.3 Typed `ApiClient` (HTTP Communication)
```csharp
// ApiClient/ApiClient.cs (MyApp.Web — Typed HTTP client with automatic refresh token rotation)
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MyApp.Web.Common;

namespace MyApp.Web.ApiClient;

public class ApiClient : IApiClient
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private async Task EnsureValidTokenAsync(CancellationToken ct)
    {
        var session = _accessor.HttpContext?.Session;
        if (session is null) return;

        var token = session.GetString(SessionKeys.AccessToken);
        var expiryRaw = session.GetString(SessionKeys.AccessTokenExpiry);
        if (string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = null;
            return;
        }

        DateTime expiry = DateTime.MinValue;
        if (!string.IsNullOrEmpty(expiryRaw))
            DateTime.TryParse(expiryRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out expiry);

        if (expiry > DateTime.UtcNow.Add(RefreshSkew))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return;
        }

        var refreshToken = session.GetString(SessionKeys.RefreshToken);
        if (string.IsNullOrEmpty(refreshToken))
        {
            _http.DefaultRequestHeaders.Authorization = null;
            return;
        }

        await _refreshLock.WaitAsync(ct);
        try
        {
            token = session.GetString(SessionKeys.AccessToken);
            expiryRaw = session.GetString(SessionKeys.AccessTokenExpiry);
            if (!string.IsNullOrEmpty(expiryRaw) &&
                DateTime.TryParse(expiryRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out expiry) &&
                expiry > DateTime.UtcNow.Add(RefreshSkew))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return;
            }

            var result = await _http.PostAsJsonAsync("api/auth/refresh", new { RefreshToken = refreshToken }, JsonOptions, ct);
            if (!result.IsSuccessStatusCode)
            {
                session.Clear();
                _http.DefaultRequestHeaders.Authorization = null;
                return;
            }

            var refreshed = await result.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(JsonOptions, ct);
            if (refreshed is { Success: true, Data: not null })
            {
                session.SetString(SessionKeys.AccessToken, refreshed.Data.AccessToken);
                session.SetString(SessionKeys.RefreshToken, refreshed.Data.RefreshToken);
                session.SetString(SessionKeys.AccessTokenExpiry, refreshed.Data.AccessTokenExpiry.ToString("o"));
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.Data.AccessToken);
            }
        }
        finally { _refreshLock.Release(); }
    }

    public async Task<ApiResponse<T>?> GetAsync<T>(string path, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        return await _http.GetFromJsonAsync<ApiResponse<T>>(path, JsonOptions, ct);
    }

    public async Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(string path, TRequest? body, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        var response = await _http.PostAsJsonAsync(path, body, JsonOptions, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiResponse<TResponse>?> PutAsync<TRequest, TResponse>(string path, TRequest? body, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        var response = await _http.PutAsJsonAsync(path, body, JsonOptions, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions, ct);
    }

    public async Task<ApiResponse<object>?> DeleteAsync(string path, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(ct);
        var response = await _http.DeleteAsync(path, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions, ct);
    }
}
```

### 9.4 Global Exception Middlewares (API & Web)
```csharp
// Middlewares/GlobalExceptionMiddleware.cs (MyApp.Api — JSON ApiResponse Exception Interceptor)
using System.Net;
using System.Text.Json;
using MyApp.Api.Common;
using MyApp.Api.Exceptions;
using Serilog;

namespace MyApp.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    public GlobalExceptionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled API exception on {Path}", context.Request.Path);
            context.Response.ContentType = "application/json";

            int statusCode = (int)HttpStatusCode.InternalServerError;
            string message = "An unexpected error occurred. Please try again or contact support.";
            List<string>? errors = null;

            if (ex is AppException appEx)
            {
                statusCode = appEx.StatusCode;
                message = appEx.Message;
                if (appEx is ValidationException valEx)
                    errors = valEx.Errors.ToList();
            }

            context.Response.StatusCode = statusCode;
            var response = new ApiResponse<object>
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Errors = errors ?? [message],
                TraceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }
}

// Middlewares/GlobalExceptionMiddleware.cs (MyApp.Web — MVC Error Redirect & Ref Number Interceptor)
namespace MyApp.Web.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    public GlobalExceptionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex)
        {
            var requestId = context.TraceIdentifier;
            var refNumber = $"REF-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            Serilog.Log.Error(ex, "Unhandled Web exception. RequestId={RequestId}, ReferenceNumber={ReferenceNumber}", requestId, refNumber);

            if (context.Request.Headers.XRequestedWith == "XMLHttpRequest" ||
                context.Request.Headers.Accept.ToString().Contains("application/json"))
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 500;
                var json = $"{{\"success\":false,\"statusCode\":500,\"message\":\"An unexpected error occurred. Please contact support referencing {refNumber}.\",\"traceId\":\"{requestId}\"}}";
                await context.Response.WriteAsync(json);
                return;
            }

            context.Response.Redirect($"/Error?requestId={Uri.EscapeDataString(requestId)}&referenceNumber={Uri.EscapeDataString(refNumber)}");
        }
    }
}
```

### 9.5 Dependency Injection Registration Extensions
```csharp
// Extensions/ServiceCollectionExtensions.cs (MyApp.Api)
using MyApp.Api.Data;
using MyApp.Api.Repository.Implementations;
using MyApp.Api.Repository.Interfaces;
using MyApp.Api.Security;
using MyApp.Api.Services.Implementations;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<SqlDataAccess>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // Repository registrations
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // Service registrations
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
```

### 9.6 Program.cs (API & Web Pipelines)
```csharp
// Program.cs (MyApp.Api)
using System.Text;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MyApp.Api.Configurations;
using MyApp.Api.Extensions;
using MyApp.Api.Middlewares;
using MyApp.Api.Security;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddApplicationServices(builder.Configuration);

var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
var jwtSettings = jwtSection.Get<JwtSettings>() ?? throw new InvalidOperationException("Jwt configuration section missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.UseCors("WebFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

```csharp
// Program.cs (MyApp.Web)
using MyApp.Web.Extensions;
using MyApp.Web.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddWebServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else { app.UseDeveloperExceptionPage(); }

app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
```

---

## 10. Downloadable Solution Archive Instruction

The full source code of this two-project Enterprise ASP.NET Core MVC (.NET 8) application is available in your workspace root. In addition to this single-file technical reference document (`MyApp_Enterprise_MVC_Solution_Bundle.md`), a standalone ZIP archive (`MyApp_Enterprise_MVC_Solution.zip`) has been created containing:
- `MyApp.sln`
- `MyApp.Api/` (ASP.NET Core 8 Web API Project)
- `MyApp.Web/` (ASP.NET Core 8 MVC Project)
- `database/` (SQL Server Schema, Seed Data, and Stored Procedures 01–09)
- `README.md`

You can download `MyApp_Enterprise_MVC_Solution_Bundle.md` directly from the file viewer or download `MyApp_Enterprise_MVC_Solution.zip` to run and deploy the solution locally.
