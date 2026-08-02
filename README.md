# Enterprise ASP.NET Core MVC Application (.NET 8)

A production-ready Enterprise **ASP.NET Core MVC (.NET 8)** application built following Microsoft best practices, SOLID principles, Clean Code, and an Interface-based Service architecture.

## Architecture Overview

```
Solution (MyApp.sln)
│
├── MyApp.Api     -> Web API backend (ADO.NET + Stored Procedures, session-token auth)
│
└── MyApp.Web     -> Traditional ASP.NET Core MVC (.NET 8) front end (cookie + session auth)
```

### Key Architectural Constraints & Rules Followed

1. **Two Projects Only**:
   - `MyApp.Api`: A standalone ASP.NET Core 8 Web API project.
   - `MyApp.Web`: A traditional ASP.NET Core 8 MVC application (not Razor Pages).
2. **Zero Direct Database Access from Web**:
   - `MyApp.Web` **never** connects to SQL Server directly. Every data request goes through `MyApp.Api` using `IHttpClientFactory` via a centralized `IApiClient` service.
3. **Strict Layered Separation (Thin Controllers)**:
   - **Controllers** receive HTTP requests and call Service interfaces only. Zero business logic or database calls exist inside Controllers.
   - **Service Layer** implements all business logic, validation coordination, audit logging **and database access**. Every service implements an interface (`IAuthService`, `IUserService`, `IRoleService`, `IMenuService`, `IPermissionService`, `IProfileService`, `IDashboardService`, `IAuditService`, `ICurrentUserService`).
   - **No Repository pattern / Repository folders.** The API Service is responsible for business logic and database operations using a single centralized ADO.NET data-access component (`SqlDataAccess`).
4. **ADO.NET + Stored Procedures Only**:
   - All database interaction uses `SqlConnection`, `SqlCommand`, `SqlParameter`, and execute helper methods (`ExecuteReaderAsync`, `ExecuteNonQueryAsync`, `ExecuteScalarAsync`).
   - No Entity Framework, EF Core, Dapper, LINQ to SQL, raw SQL strings, or JWT are used. Every database query executes a stored procedure from `database/`.
5. **Session Token + Cookie Authentication (no JWT)**:
   - Login issues an **opaque session token** (256-bit random). The database stores only its SHA-256 hash; the token is returned exactly once and held in the Web's server-side session, forwarded to the API on the `X-Api-Token` header. It is never exposed to browser JavaScript.
   - The MVC layer uses secure **HttpOnly / Secure / SameSite=Strict cookies** plus a 30-minute idle session timeout.
   - Full support for login, logout, session timeout, secure cookies, password hashing (PBKDF2-SHA256, 210k iterations), password change (with current-password verification and history check), password reset (single-use token), account lockout (5 attempts / 15 minutes), role authorization and per-permission authorization.
   - **Session Management**: users can review active sessions (IP, browser, started/expiry) and revoke individual sessions or sign out all other devices.
6. **Centralized Global Exception Handling**:
   - Unhandled exceptions are intercepted by `GlobalExceptionMiddleware` in both API and Web projects.
   - In `MyApp.Api`, exceptions are returned as standardized JSON envelopes (`ApiResponse<T>`) with `TraceId` and proper HTTP status codes.
   - In `MyApp.Web`, unhandled page exceptions redirect users to `/Error`, displaying a friendly message, the HTTP status code, a generated Reference Number (`REF-yyyyMMdd-XXXX`), and the Request Id while hiding technical stack traces. Never exposed: stack traces, SQL errors, connection strings, internal exception details, server paths.
7. **Unified API Response Model**:
   - Every API returns `ApiResponse<T>` with standard properties: `Success`, `StatusCode`, `Message`, `Data`, `Errors`, `Pagination`, and `TraceId`.

## Request Flow

```
Browser
    ↓
MyApp.Web MVC Controller
    ↓
Web Service Interface (IUserService, IAuthService, ...)
    ↓
Web Service Implementation
    ↓
ApiClient (typed HttpClient via IHttpClientFactory, X-Api-Token header)
    ↓ HTTP
MyApp.Api Controller
    ↓
API Service Interface
    ↓
API Service Implementation  (business logic + audit + validation)
    ↓
SqlDataAccess (centralized ADO.NET helper)
    ↓
Stored Procedure
    ↓
SQL Server
```

## Project Structures

### MyApp.Api
```
MyApp.Api/
├── Controllers/         # Thin API controllers calling Service interfaces
├── Services/
│   ├── Interfaces/      # IUserService, IRoleService, etc.
│   └── Implementations/ # UserService, RoleService, etc. (business logic + ADO.NET via SqlDataAccess)
├── Models/              # Domain entities
├── DTOs/                # Request & response data transfer objects
├── Data/                # ISqlConnectionFactory, SqlConnectionFactory, SqlDataAccess, SqlDataReaderExtensions
├── Security/            # SessionTokenService, SessionTokenAuthenticationHandler, PasswordHasher
├── Helpers/             # PasswordPolicyHelper, SecurityHelper
├── Utilities/           # DateTimeUtility, StringUtility
├── Extensions/          # ServiceCollectionExtensions
├── Exceptions/          # AppException, NotFoundException, UnauthorizedException, etc.
├── Middlewares/         # GlobalExceptionMiddleware
├── Filters/             # ValidateModelAttribute, AuthorizePermissionAttribute
├── Validators/          # Input validation helpers
├── Configurations/      # DatabaseSettings, SecuritySettings
├── Common/              # ApiResponse<T>, PagedResult<T>, UserStatus
└── Program.cs
```

### MyApp.Web
```
MyApp.Web/
├── Controllers/         # Auth, Dashboard, Users, Roles, Menus, Permissions, Profile,
│                        # Settings, AuditLogs, Administration, Error, Home
├── Views/
│   ├── Shared/          # _Layout, _AuthLayout, _Navbar (theme switcher), _Sidebar, _Footer, _Alerts, _Pagination
│   ├── Administration/  # Central hub with Users / Roles / Menus / Permissions / Settings tabs
│   ├── AuditLogs/       # Filterable, paginated audit trail
│   ├── Dashboard/       # KPI cards, recent logins, activity feed
│   ├── Users/           # Index, Create, Edit
│   ├── Roles/           # Index, Create, Edit, Clone
│   ├── Menus/           # Index, Create, Edit (unlimited nested hierarchy)
│   ├── Permissions/     # Interactive role privilege matrix
│   ├── Profile/         # Index (incl. Active Sessions), Edit, ChangePassword
│   ├── Settings/        # Theme (Light/Dark/Glass), language, sidebar preference
│   ├── Error/           # Index (friendly page), 404, AccessDenied
│   └── Auth/            # Login, ForgotPassword, ResetPassword
├── Models/              # UI Models
├── ViewModels/          # MVC Page ViewModels
├── DTOs/                # DTOs matching API request/response payloads
├── Services/
│   ├── Interfaces/      # Web Service interfaces
│   └── Implementations/ # Web Services calling ApiClient
├── ApiClient/           # Centralized IApiClient / ApiClient (IHttpClientFactory, session-token header)
├── Helpers/             # CookieHelper, SessionHelper, HtmlHelperExtensions
├── Extensions/          # ServiceCollectionExtensions
├── Common/              # ApiResponse<T>, PaginationMeta, SessionKeys
├── Filters/             # AuthGuardFilter, AdminOnlyFilter
├── Middlewares/         # GlobalExceptionMiddleware
├── Components/          # SidebarMenuViewComponent
├── TagHelpers/          # PermissionTagHelper, PaginationTagHelper
├── wwwroot/             # site.css (Light/Dark/Glass theme engine), site.js
└── Program.cs
```

## Modules

- **Authentication** — Login, Logout, Password Reset, Change Password, Account Lockout, Session Management
- **Users** — Create / Read / Update / Delete / Activate / Deactivate / Search / Filter / Sort / Pagination / Role Assignment
- **Roles** — Create / Edit / Delete / Activate / Deactivate / Assign Users / Clone Roles
- **Menus & Permissions** — Parent/Child menu tree (unlimited nesting), ordering, icons, role-based menus, permission matrix (View / Add / Edit / Delete / Print / Export / Approve / Reject)
- **Profile** — Profile management, password change, user preferences (theme / language / sidebar)
- **Dashboard** — KPI statistics (users, active, locked, roles, menus), recent logins, activity feed
- **Audit** — User, Module, Action, Old Value, New Value, IP Address, Browser, Date/Time (filterable + paginated)
- **Administration Hub** — central screen with tabs for Users, Roles, Menus, Permissions, Settings

## UI / Theme Engine

- Bootstrap 5, responsive MVC views, AJAX/Fetch, modal forms, confirmation dialogs, toast notifications, loading indicators, search and pagination.
- **Glassmorphism theme** with frosted-glass cards, glass navigation, and a vibrant mesh background.
- Navbar theme switcher: **Light / Dark / Glass**. The selected theme is stored in the user's server-side preferences and restored automatically on the next login.

## Database

All scripts live in `database/`:

```
database/
├── 01_Schema.sql             # Tables (Users, Roles, Menus, PermissionTypes, RoleMenuPermissions,
│                             #  UserMenuPermissions, SessionTokens, PasswordHistory, PasswordResetTokens,
│                             #  UserPreferences, AuditLogs)
├── 02_SeedData.sql           # Administrator role, menus, full admin permissions, default admin user
├── 03_SP_Auth.sql            # Login, lockout, change/reset password, session-token procedures
├── 04_SP_Users.sql           # User CRUD, search/filter/sort/pagination, preferences
├── 05_SP_Roles.sql           # Role CRUD, clone, assign
├── 06_SP_Menus.sql           # Menu tree, role-based menus, CRUD
├── 07_SP_Permissions.sql     # Permission types, matrix, assign (TVP), per-cell check
├── 08_SP_Dashboard_Audit.sql # Dashboard KPIs + activity, audit insert/search
└── 09_SP_Profile.sql         # Profile read/update
```

Default admin account (from seed): **admin / ChangeMe123!** — change it immediately after first login.
