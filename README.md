# Enterprise ASP.NET Core MVC Application (.NET 8)

A production-ready Enterprise **ASP.NET Core MVC (.NET 8)** application designed and built following Microsoft best practices, SOLID principles, Clean Code, and an Interface-based Architecture.

## Architecture Overview

```
Solution (MyApp.sln)
│
├── MyApp.Api     -> Web API backend (ADO.NET + Stored Procedures)
│
└── MyApp.Web     -> Traditional ASP.NET Core MVC (.NET 8) front end
```

### Key Architectural Constraints & Rules Followed

1. **Two Projects Only**:
   - `MyApp.Api`: A standalone ASP.NET Core 8 Web API project.
   - `MyApp.Web`: A traditional ASP.NET Core 8 MVC application (not Razor Pages).
2. **Zero Direct Database Access from Web**:
   - `MyApp.Web` **never** connects to SQL Server directly. Every data request goes through `MyApp.Api` using `IHttpClientFactory` via a centralized `IApiClient` service.
3. **Strict Layered Separation (Thin Controllers)**:
   - **Controllers** receive HTTP requests and call Service interfaces only. Zero business logic or repository calls exist inside Controllers.
   - **Service Layer** implements all business logic, validation coordination, and audit logging. Every service implements an interface (`IAuthService`, `IUserService`, `IRoleService`, `IMenuService`, `IPermissionService`, `IProfileService`, `IDashboardService`, `IAuditService`, `ICurrentUserService`).
   - **Repository Layer** in `MyApp.Api` implements all database operations (`IAuthRepository`, `IUserRepository`, `IRoleRepository`, `IMenuRepository`, `IPermissionRepository`, `IProfileRepository`, `IDashboardRepository`, `IAuditLogRepository`). Repositories contain zero business logic.
4. **ADO.NET + Stored Procedures Only**:
   - All database interaction uses `SqlConnection`, `SqlCommand`, `SqlParameter`, and execute helper methods (`ExecuteReaderAsync`, `ExecuteNonQueryAsync`, `ExecuteScalarAsync`).
   - No Entity Framework, EF Core, Dapper, LINQ to SQL, or raw SQL strings are used. Every database query executes a stored procedure from `database/`.
5. **Centralized Global Exception Handling**:
   - Unhandled exceptions are intercepted by `GlobalExceptionMiddleware` in both API and Web projects.
   - In `MyApp.Api`, exceptions are returned as standardized JSON envelopes (`ApiResponse<T>`) with `TraceId` and proper HTTP status codes.
   - In `MyApp.Web`, unhandled page exceptions redirect users to `/Error`, displaying friendly messages, a generated Reference Number (`REF-yyyyMMdd-XXXX`), and the Request Id while hiding technical stack traces.
6. **Unified API Response Model**:
   - Every API returns `ApiResponse<T>` with standard properties: `Success`, `StatusCode`, `Message`, `Data`, `Errors`, `Pagination`, and `TraceId`.

## Project Structures

### MyApp.Api
```
src/MyApp.Api/
├── Controllers/         # Thin API controllers calling Services
├── Repository/
│   ├── Interfaces/      # IUserRepository, IRoleRepository, etc.
│   └── Implementations/ # UserRepository, RoleRepository, etc.
├── Services/
│   ├── Interfaces/      # IUserService, IRoleService, etc.
│   └── Implementations/ # UserService, RoleService, etc.
├── Models/              # Domain entities
├── DTOs/                # Request & response data transfer objects
├── Data/                # ISqlConnectionFactory, SqlDataAccess, SqlDataReaderExtensions
├── Helpers/             # PasswordPolicyHelper, SecurityHelper
├── Utilities/           # DateTimeUtility, StringUtility
├── Extensions/          # ServiceCollectionExtensions
├── Exceptions/          # AppException, NotFoundException, UnauthorizedException, etc.
├── Middlewares/         # GlobalExceptionMiddleware
├── Filters/             # ValidateModelAttribute, AuthorizePermissionAttribute
├── Validators/          # Input validation helpers
├── Security/            # JwtTokenGenerator, PasswordHasher, JwtSettings
├── Configurations/      # DatabaseSettings, SecuritySettings
├── Common/              # ApiResponse<T>, PagedResult<T>, UserStatus
└── Program.cs
```

### MyApp.Web
```
src/MyApp.Web/
├── Controllers/         # AuthController, DashboardController, UsersController, RolesController,
│                        # MenusController, PermissionsController, ProfileController, SettingsController,
│                        # ErrorController, HomeController
├── Views/
│   ├── Shared/          # _Layout, _AuthLayout, _Navbar, _Sidebar, _Footer, _Alerts, _Pagination
│   ├── Dashboard/       # Index.cshtml
│   ├── Users/           # Index.cshtml, Create.cshtml, Edit.cshtml
│   ├── Roles/           # Index.cshtml, Create.cshtml, Edit.cshtml, Clone.cshtml
│   ├── Menus/           # Index.cshtml, Create.cshtml, Edit.cshtml
│   ├── Permissions/     # Index.cshtml (Interactive Role Privilege Matrix)
│   ├── Profile/         # Index.cshtml, Edit.cshtml, ChangePassword.cshtml
│   ├── Settings/        # Index.cshtml
│   ├── Error/           # Index.cshtml, 404.cshtml, AccessDenied.cshtml
│   └── Auth/            # Login.cshtml, ForgotPassword.cshtml, ResetPassword.cshtml
├── Models/              # UI Models (AuthModels, DashboardModels, ProfileModels, AuditModels, etc.)
├── ViewModels/          # MVC Page ViewModels (UserListViewModel, RoleFormViewModel, etc.)
├── DTOs/                # DTOs matching API request/response payloads
├── Services/
│   ├── Interfaces/      # Web Service interfaces
│   └── Implementations/ # Web Services calling ApiClient
├── ApiClient/           # Centralized IApiClient / ApiClient (IHttpClientFactory + token refresh)
├── Helpers/             # CookieHelper, SessionHelper, HtmlHelperExtensions
├── Extensions/          # ServiceCollectionExtensions
├── Common/              # ApiResponse<T>, PagedResult<T>, SessionKeys
├── Filters/             # AuthGuardFilter, AdminOnlyFilter
├── Middlewares/         # GlobalExceptionMiddleware
├── Components/          # SidebarMenuViewComponent
├── TagHelpers/          # PermissionTagHelper, PaginationTagHelper
├── wwwroot/             # CSS, JS, theme variables
└── Program.cs
```

## Request Flow
```
Browser -> MVC Controller -> Service Interface (Web) -> Service Implementation (Web)
        -> ApiClient (typed HttpClient) -> Web API -> API Controller
        -> Service Interface (API) -> Service Implementation (API)
        -> Repository Interface -> Repository Implementation
        -> ADO.NET (SqlDataAccess) -> Stored Procedure -> SQL Server
```
