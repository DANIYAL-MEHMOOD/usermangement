# MyApp — Phase 1: Foundation

Enterprise ASP.NET Core 8 solution scaffold: ADO.NET + stored procedures only
(no EF/Dapper), JWT auth, RBAC with dynamic menus, and a Razor Pages front
end that talks to the API exclusively over HTTP.

## What's in this phase

```
database/
  01_Schema.sql              All tables, keys, indexes
  02_SeedData.sql             Permission types, Administrator role, root menus, admin user
  03_SP_Auth.sql               Login, lockout, password change/history, reset tokens, refresh tokens
  04_SP_Users.sql              User CRUD, search/sort/paginate, preferences
  05_SP_Roles.sql              Role CRUD, clone, assign
  06_SP_Menus.sql               Menu CRUD, per-user menu tree (sp_GetUserMenus)
  07_SP_Permissions.sql         Permission matrix read + bulk assign (TVP), single-permission check
  08_SP_Dashboard_Audit.sql      Dashboard stats, audit log insert/search

src/
  MyApp.Domain/                Entities, enums — no dependencies
  MyApp.Application/            DTOs, repository interfaces, ApiResponse<T> envelope
  MyApp.Infrastructure/          ADO.NET data access (SqlDataAccess), repository implementations,
                                   PBKDF2 password hasher, JWT generator
  MyApp.Api/                     Controllers (Auth, Users, Roles, Menus, Permissions, Dashboard,
                                   AuditLogs), JWT bearer auth, Serilog, global exception middleware,
                                   IP rate limiting, Swagger
  MyApp.Web/                     Razor Pages front end — Login + Dashboard placeholder, typed
                                   HttpClient (IApiClient) that is the ONLY way the Web project
                                   reaches data; it never opens a SqlConnection itself
```

## Setup

1. **Database** — run the `database/*.sql` files in order (01 → 08) against a SQL
   Server 2019+ instance. `02_SeedData.sql` inserts an `admin` user with a
   placeholder password hash/salt (`0x00`) that **will not authenticate** —
   before go-live, hash a real password with `PasswordHasher` (see
   `MyApp.Infrastructure/Security/PasswordHasher.cs`) and update that row, or
   add a one-time seeding endpoint/console command that calls `sp_ChangePassword`.

2. **Configuration** — `src/MyApp.Api/appsettings.json`:
   - `ConnectionStrings:DefaultConnection` → your SQL Server instance
   - `Jwt:SigningKey` → replace the placeholder with a real secret (store it in
     `dotnet user-secrets` or a key vault, not in source control, for real
     deployments)
   - `Cors:AllowedOrigins` → the Web project's URL

   `src/MyApp.Web/appsettings.json`:
   - `ApiBaseUrl` → the API project's URL

3. **Run**
   ```
   dotnet restore
   dotnet run --project src/MyApp.Api    # Swagger at /swagger
   dotnet run --project src/MyApp.Web    # Login page at /Login
   ```

## Design notes

- **Every** database call goes through a stored procedure via `SqlDataAccess`
  (`MyApp.Infrastructure/Data`) — parameterized `SqlCommand`s only, no string-built SQL.
- **RBAC model**: `Users.RoleId` (single role per user, matching the spec's
  "Assign Role" language) → `RoleMenuPermissions` (role × menu × permission-type
  grants) → `UserMenuPermissions` (optional per-user override, grant or deny,
  for exceptions without creating a one-off role).
- **Menus** are a self-referencing tree (`ParentMenuId`) with unlimited depth;
  `sp_GetUserMenus` returns only menus the caller's role/overrides grant VIEW
  on, including ancestor nodes so the sidebar tree never has orphaned children.
- **Passwords**: PBKDF2-SHA256, 210k iterations, 128-bit salt — hash/salt
  stored as `VARBINARY`, never compared in T-SQL. History table blocks reuse
  of recent passwords; `PasswordExpiryDate` and `MustChangePassword` drive
  forced rotation.
- **Auth flow**: short-lived JWT access token (15 min default) + opaque
  refresh token stored server-side (rotated on every refresh, revocable).
  Account lockout after N failed attempts, auto-unlocked after a cooldown.
- **Every** API response uses `ApiResponse<T>` (`Success`, `Message`, `Data`,
  `Errors`, `Pagination`) so the front end has one shape to parse everywhere.

# MyApp — Phase 1 + Phase 2

Enterprise ASP.NET Core 8 solution scaffold: ADO.NET + stored procedures only
(no EF/Dapper), JWT auth with silent refresh, RBAC with dynamic menus, and a
Razor Pages front end — now with working Users, Roles, Permission Matrix, and
Menu Management screens — that talks to the API exclusively over HTTP.

## What's in this build

```
database/                      (Phase 1 — unchanged)
  01_Schema.sql ... 08_SP_Dashboard_Audit.sql

src/
  MyApp.Domain/                (Phase 1 — unchanged)
  MyApp.Application/            (Phase 1 — unchanged)
  MyApp.Infrastructure/          (Phase 1 — unchanged)
  MyApp.Api/                     (Phase 1, +1 fix — see below)

  MyApp.Web/                     Phase 2 additions:
    Filters/                      AuthGuardFilter (redirect to /Login if no session
                                    token), AdminOnlyFilter (redirect non-admins away
                                    from Roles/Permissions/Menus)
    Models/                        Web-local DTOs mirroring the API's request/response
                                    shapes (kept separate from MyApp.Application on
                                    purpose — the Web project's only line to the
                                    backend is HTTP, never a shared assembly)
    Services/ApiClient.cs           Silent JWT refresh: checks the access token's
                                    expiry before every call and transparently
                                    exchanges the refresh token when it's within 30s
                                    of expiring, so pages never see a stray 401
    ViewComponents/SidebarMenu/     Renders the sidebar from GET /api/menus/mine —
                                    recursive, unlimited depth, collapsible, searchable
    Pages/Users/Index.cshtml         Search, filter (role/status/department), sort,
                                    paginate, create/edit modal, activate/deactivate/
                                    delete — all via AJAX (fetch), no full page reloads
    Pages/Roles/Index.cshtml         List, create/edit, clone (copies full permission
                                    set), delete (blocked for system roles / roles
                                    still holding users)
    Pages/Permissions/Index.cshtml   The permission matrix — one screen, per-role menu
                                    x permission-type grid, full-replace save
    Pages/Menus/Index.cshtml         Recursive tree CRUD — add child, edit, delete
                                    (blocked while children exist), ordering, icons
    Pages/Settings/Theme.cshtml       Light/dark/auto, saved server-side, restored on
                                    next login on any device
    Pages/Login, Logout, ForgotPassword, ResetPassword, ChangePassword, Dashboard
```

### One Phase 1 fix included here
`RolesController` was entirely `[Authorize(Roles = "Administrator")]`, which meant
a non-admin viewing the Users screen would get a 403 just loading the role filter
dropdown. Read endpoints (`GET /api/roles`, `GET /api/roles/{id}`) are now open to
any signed-in user; Save/Delete/Clone/AssignToUser stay Administrator-only.

## Design notes specific to Phase 2

- **JWT never reaches the browser.** Every AJAX call from a page's JS goes to a
  named handler on that same Razor Page (`?handler=Search`, `?handler=Save`, etc.),
  which runs server-side, calls `IApiClient` (attaching the JWT from `Session`
  there), and returns the API's JSON straight through. Client-side JS only ever
  talks to its own Razor Page.
- **CSRF**: `AddAntiforgery` is configured with a header name
  (`X-CSRF-TOKEN`); `_Layout.cshtml` emits the token in a meta tag, and
  `wwwroot/js/site.js`'s `callApi()` helper attaches it automatically on every
  non-GET request. Razor Pages' built-in `AutoValidateAntiforgeryTokenAttribute`
  convention enforces it on every POST handler without extra wiring per page.
- **Case sensitivity**: the API serializes camelCase (ASP.NET Core default);
  `ApiClient` explicitly uses `JsonSerializerDefaults.Web` everywhere so nothing
  silently deserializes to null. If you add new pages, remember to pass the same
  options for any raw `JsonSerializer` calls you write yourself.
- **Full-replace, not diff**: both the Permission Matrix save and
  `sp_AssignPermissions` are full-replace by design — the screen sends every
  currently-checked cell, not just what changed, matching the stored procedure's
  delete-then-reinsert semantics.

## Not yet built (later phases)

- Phase 4: audit log viewer screen, notification center (currently a static
  "no notifications" dropdown), profile picture upload, actually sending the
  forgot-password email (the endpoint exists and returns success either way,
  but nothing sends mail yet), skeleton-loading polish on the remaining pages
- Hardening before go-live: replace the placeholder admin password hash, move
  `Jwt:SigningKey` out of `appsettings.json`, and run everything through a real
  `dotnet build` — no .NET SDK was available in the environment this was built
  in, so nothing here has been compiler-checked, only manually cross-referenced
  (stored procedure names, brace/tag balance, DTO shapes)

