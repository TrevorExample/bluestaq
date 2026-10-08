# BlueStaq API

## Running the API

Run from the repository root with `dotnet run --project src/BlueStaq.Api`.
Use the HTTPS URL printed at startup. Login sets a secure, HTTP-only session
cookie; retain and send this cookie on subsequent requests.

## Bootstrap User

Before the first login, configure an initial user through development user secrets:

```powershell
dotnet user-secrets set "BootstrapUser:Email" "you@example.com" --project src/BlueStaq.Api
dotnet user-secrets set "BootstrapUser:DisplayName" "Your name" --project src/BlueStaq.Api
dotnet user-secrets set "BootstrapUser:Password" "<your-password-at-least-12-characters>" --project src/BlueStaq.Api
```

Alternatively set `BootstrapUser__Email` and `BootstrapUser__Password` environment
variables. Use `BootstrapUser__DisplayName` to set the display name as well.
These settings do not need to appear in `appsettings.json`: ASP.NET Core reads
configuration from user secrets in Development and from environment variables.

The startup code in `Program.cs` creates the database and tables if needed,
then creates the bootstrap user only if the email does not already exist.
It trims and lowercases the email. The password must contain at least 12
characters; the display name defaults to the email when omitted.
Passwords are hashed using ASP.NET Core's password hasher. Remove the bootstrap
settings after creating the user; changing them does not reset an existing password.

## Database

SQLite stores data in `bluestaq.db`. Override the location with
`ConnectionStrings__DefaultConnection`. Startup uses `EnsureCreated` for this
initial schema; future schema changes will need a migration strategy.

The team-based schema contains:

| Table | Columns |
| --- | --- |
| Users | Id (PK), Email (unique), DisplayName, PasswordHash |
| Teams | Id (PK), Name, CreatedAt (UTC) |
| TeamMembers | TeamId (PK, FK), UserId (PK, FK), Role |
| Notes | Id (PK), TeamId (FK), CreatedByUserId (FK), Title, Content, CreatedAt, UpdatedAt |

TeamMembers uses a composite primary key so each user can join each team once,
while joining multiple teams. Roles are stored as text: team creators are
`Owner`, and other memberships default to `Member`. Both roles currently have
the same note permissions. Users and teams expose membership collections,
and teams expose their notes through EF Core navigation properties.
Login responses include DisplayName, and team responses include CreatedAt.
The password hash and note timestamps support the existing login and note APIs.

This revised schema applies to newly created databases. An existing database
from the earlier scaffold needs a schema migration before running this version;
`EnsureCreated` does not upgrade existing tables.

## Endpoints and Authorization

| Method | Endpoint | JSON body |
| --- | --- | --- |
| POST | /api/auth/login | `{"email":"you@example.com","password":"..."}` |
| GET | /api/teams | |
| POST | /api/teams | `{"name":"My team"}` |
| GET | /api/teams/{teamId}/notes | |
| POST | /api/teams/{teamId}/notes | `{"title":"Title","content":"Text"}` |
| GET | /api/notes/{id} | |
| PUT | /api/notes/{id} | `{"title":"Updated title","content":"Updated text"}` |
| DELETE | /api/notes/{id} | |

All routes except login require authentication. An authenticated user can create
a team without belonging to an existing team. Team creators automatically become
members with the `Owner` role.
Members can read, create, update, and delete their team's notes. Missing resources
and resources outside a user's teams return 404. Invalid payloads return 400,
invalid credentials or missing sessions return 401, and successful deletes return
204. Login is limited to 10 requests per minute per client IP; requests exceeding
the limit return HTTP 429 (Too Many Requests).

Registration, inviting team members, and password reset are not implemented yet.
`UsersController` is currently an empty placeholder and exposes no endpoints.

## Application Structure

`NotesController` delegates note operations and membership checks to `NoteService`.
`TeamsController` uses `TeamService` to list and create teams. Both services depend
on `IAppStore`, whose `AppStore` implementation accesses SQLite through EF Core.

Request and response DTOs are defined as `public sealed record` types in
`BlueStaq.Application/DTOs/Requests.cs`. Services map database entities to response
DTOs, such as `NoteResponse`.

## Integration Tests

Run from the repository root:

```powershell
dotnet test tests/BlueStaq.Tests
```

The tests exercise HTTP endpoints through a test API and use an isolated temporary
SQLite database for each test. Shared setup in `ApiTestBase.cs` supplies these
bootstrap settings:

```csharp
builder.UseSetting("BootstrapUser:Email", "owner@example.com");
builder.UseSetting("BootstrapUser:DisplayName", "Team owner");
builder.UseSetting("BootstrapUser:Password", "Test-password-123!");
```

API startup seeds the account. Before each test, `InitializeAsync()` logs in as
`owner@example.com` and creates **Team1** through the API, making that user its
Owner. Notes and database tests reuse Team1; team tests can create additional
teams. `Dispose()` shuts down the test API and deletes the temporary database.
This setup does not seed the normal application database.

| Test Class | Coverage |
| --- | --- |
| AuthControllerTests | Successful login, returned user profile, and invalid credentials |
| TeamsControllerTests | Anonymous requests, team creation and listing, invalid names, and membership in multiple teams |
| NotesControllerTests | Full note lifecycle, anonymous requests, invalid input, missing resources, shared-team access, and access restrictions |
| DatabaseTests | Duplicate membership rejection and user/team foreign key constraints |

`NoteLifecyclePersistsChanges` covers creating, listing, retrieving, updating,
and deleting notes in one test, including reading updated content from another
session.



# ManNotes
# BlueStaq API Overview

## Rate Limiting

The rate limiter returns **HTTP 429 — Too Many Requests** when the request limit is reached.

## Database and Bootstrap User

The application uses **SQLite** because it is easy to set up and suitable for example code.

The test bootstrap settings are configured in `ApiTestBase.cs`:

```csharp
builder.UseSetting("BootstrapUser:Email", "owner@example.com");
builder.UseSetting("BootstrapUser:DisplayName", "Team owner");
builder.UseSetting("BootstrapUser:Password", "Test-password-123!");
```

The API startup code in `Program.cs` reads these settings and creates the user if the email does not already exist.

Before each test, the shared setup logs in as `owner@example.com` and creates a team named **Team1**. The account becomes the team’s Owner.

## Controllers and Services

### AuthController

`AuthController` provides the login method used to authenticate users.

### NotesController and NoteService

`NotesController` uses `NoteService` to manage notes.

`NoteService` handles:

- Checking team membership.
- Finding required notes.
- Listing notes.
- Creating notes.
- Retrieving individual notes.
- Updating notes.
- Deleting notes.

It maps the `Note` database entity to a `NoteResponse` DTO for API responses.

### TeamsController and TeamService

`TeamsController` uses `TeamService` to list the current user’s teams and create new teams. Each team has a `Guid` identifier.

The controller requires authentication through the `[Authorize]` attribute. An authenticated user can create a team without already belonging to one. Creating the team automatically makes that user its Owner.

### UsersController

`UsersController` is currently a placeholder with no actions.

The `User` entity contains the ID, email, display name, password hash, team memberships, and created notes. Passwords are stored as hashes.

### IAppStore

`NoteService` and `TeamService` use `IAppStore`, an interface that defines data access operations.

`AppStore` implements that interface using **Entity Framework Core and SQLite**. This keeps database operations separate from the services’ application rules.

## API Integration Tests

These tests send HTTP requests through the API and use isolated temporary SQLite databases.

### ApiTestBase

`ApiTestBase` provides shared configuration, login helpers, and setup. The API startup seeds the bootstrap user, and the test setup creates **Team1**.

Its `Dispose()` method shuts down the test API and deletes the temporary database after each test.

### AuthControllerTests

These tests verify:

- Successful login.
- The returned user profile.
- Rejection of invalid credentials.

### DatabaseTests

These tests verify membership primary keys and foreign keys using **Team1**.

They check that duplicate memberships are rejected and that memberships cannot reference nonexistent users or teams.

### NotesControllerTests

These tests cover the complete note lifecycle:

- Creating notes.
- Listing notes.
- Retrieving notes.
- Updating notes.
- Deleting notes.

They also check anonymous requests, invalid input, missing resources, shared-team access, and access restrictions for users outside the team.

A future improvement would be to add individual tests for each note action while retaining the complete lifecycle test. For now, the generated lifecycle test remains.

### TeamsControllerTests

These tests verify:

- Rejection of anonymous requests.
- Visibility of newly created teams.
- Rejection of invalid team names.
- Membership in multiple teams.

## Request and Response DTOs

In `BlueStaq.Application`, `Requests.cs` defines these types as `public sealed record`:

- `LoginRequest`
- `CreateTeamRequest`
- `SaveNoteRequest`
- `UserResponse`
- `TeamResponse`
- `NoteResponse`

These records are already data models used to transfer data into and out of the API. Records provide concise declarations, generated properties, and value-based equality. We can change them to classes later if our requirements call for different behavior.