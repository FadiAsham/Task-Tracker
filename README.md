# Task Tracker

A full-stack task management application built with ASP.NET Core 10, Angular 22 (TypeScript/SCSS), and PostgreSQL 18. Create, read, update, and delete tasks with status, priority, and optional due dates.

## Prerequisites

- .NET 10 SDK (the repo accepts newer .NET 10 feature bands).
- Node.js 24.15+ and npm.
- Docker Desktop with Docker Compose v2 for the default PostgreSQL and Keycloak setup.

## Run locally

Run the following commands from the repository root in PowerShell.

### 1. Start PostgreSQL and Keycloak

From the repository root, start Docker Desktop, copy the environment template, and edit `.env`. Choose separate values for `POSTGRES_PASSWORD` and `KEYCLOAK_ADMIN_PASSWORD`; do not leave the example placeholders:

```powershell
Copy-Item .env.example .env
# Edit .env with your own POSTGRES_PASSWORD and KEYCLOAK_ADMIN_PASSWORD.
docker compose up -d --wait
docker compose ps
```

PostgreSQL is available at `localhost:5433` by default. Keycloak is at `http://localhost:8080`; its Admin Console is at `http://localhost:8080/admin/`. On a fresh Keycloak data volume, sign in as `admin` using the `KEYCLOAK_ADMIN_PASSWORD` from `.env`. Both services bind to loopback and persist data in Docker volumes. `.env` is ignored by Git and is not loaded by .NET. The bootstrap admin password is only applied when Keycloak initializes an empty data volume; it does not reset an existing admin password.

Compose runs in the background with `-d`; view service logs with `docker compose logs -f`. To stop the services without deleting data, run `docker compose down`. Do not use `docker compose down -v` unless you intend to delete the database and Keycloak data. Keycloak uses development mode and must not be exposed to untrusted networks or used in production. Use the same Compose Keycloak instance at `localhost:8080` for the rest of this guide; a separately launched Keycloak container has separate credentials and data and cannot use the same host port simultaneously.

### 2. Configure Keycloak

Create the `TaskTracker-realm`, the confidential `TaskTracker-backend` client, and a Task Tracker user in the Keycloak Admin Console. Register the exact API redirect URLs and save the client secret in .NET user-secrets. Follow [the Keycloak setup guide](docs/SSO.md) before starting the API; sign-in will fail until the realm and client exist.

### 3. Configure and start the API

```powershell
dotnet restore --locked-mode
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:Tasks" "Host=localhost;Port=5433;Database=tasktracker;Username=tasktracker;Password=YOUR_LOCAL_PASSWORD" --project backend/TaskTracker.Api
dotnet user-secrets set "Keycloak:ClientSecret" "YOUR_KEYCLOAK_CLIENT_SECRET" --project backend/TaskTracker.Api
dotnet ef database update --project backend/TaskTracker.Api
dotnet run --project backend/TaskTracker.Api
```

Replace both secret placeholders with the values for your local database and the `TaskTracker-backend` client. Match the PostgreSQL port, username, database, and password to your setup. For EF commands outside Development, provide the connection using `$env:ConnectionStrings__Tasks` instead. The app deliberately does not apply migrations on startup, keeping schema changes explicit.

API: **http://localhost:5080**. Development API docs: **http://localhost:5080/docs**. The database starts empty; create the first task through the UI.

### 4. Start Angular in another terminal

```powershell
cd frontend
npm ci
npm start
```

Open **http://localhost:4200** and choose **Sign in with Keycloak**. Angular proxies API and OIDC callback routes to port 5080, so no development CORS configuration is necessary. The frontend is client-rendered; its production host must support fallback to `index.html` and proxy `/api` and the OIDC callback routes to the backend.

## Build verification

Run from the project root:

~~~powershell
dotnet build
npm run build --prefix frontend
~~~

## API documentation

Open **http://localhost:5080/docs** while the API is running in Development to browse and try all endpoints. The explorer uses Scalar.AspNetCore with the existing generated OpenAPI contract. See [the complete endpoint guide](docs/API.md) for request/response examples, validation, errors, and a PowerShell walkthrough. Restart the API after updating the code.

## Design and dependencies

`backend/TaskTracker.Api` contains one API project. Controllers handle HTTP, a task service handles use cases, DTOs define the API boundary, and EF configurations own persistence constraints. No generic repository, mapping framework, or CQRS library is needed for this scope. EF reads use no tracking; operations are asynchronous and accept cancellation tokens. Composite indexes support list ordering and status filtering.

`frontend` uses standalone Angular components, lazy routes, typed reactive forms, HttpClient, signals for view state, and RxJS cancellation for list requests. Shared form behavior handles both create and edit. SCSS provides a responsive navy/orange interface with accessible labels, focus states, loading/empty/error states, and delete confirmation. Wide task tables scroll inside their panel on narrow screens.

Npgsql is the EF PostgreSQL provider. Built-in OpenAPI exposes the contract. Both NuGet and npm dependency lock files are included.

## Usage walkthrough

1. Create a task with title, description, priority, and due date.
2. Refresh to demonstrate database persistence.
3. Edit the task to In progress; filter the list to that status.
4. Submit an empty title to show validation.
5. Stop the API and try loading/saving to show a recoverable error; restart it and retry.
6. Delete the task, first canceling and then accepting the confirmation.
7. Explore the responsive layout and API documentation.

## Scope and tradeoffs

This is a local shared task tracker. Keycloak SSO protects task endpoints. User ownership, deployments, and assignments remain outside the current scope. Updates use last-write-wins; a concurrently removed record can return a conflict during an update. Pagination is offset-based; concurrent inserts can shift page boundaries.

See [SSO setup](docs/SSO.md) for Keycloak client configuration, sign-in, sign-out, and authenticated API requests.

## Isolated native PostgreSQL option (Windows)

If Docker is unavailable and you prefer not to use an existing database service, the PostgreSQL 18 native tools can run a development cluster inside the ignored `.local` directory. The following uses a loopback-only instance on port 55432 with trust authentication, strictly for local development:

```powershell
New-Item -ItemType Directory -Force .local | Out-Null
# First setup only; omit initdb if .local/pgdata already exists.
initdb -D .local/pgdata -U tasktracker -A trust --encoding=UTF8 --locale=C
pg_ctl -D .local/pgdata -l .local/postgres.log -o '-h 127.0.0.1 -p 55432' -w start
$env:ConnectionStrings__Tasks='Host=127.0.0.1;Port=55432;Database=tasktracker;Username=tasktracker'
dotnet ef database update --project backend/TaskTracker.Api
dotnet run --project backend/TaskTracker.Api
```

Stop the API and Angular with Ctrl+C, then stop this database using `pg_ctl -D .local/pgdata -m fast -w stop`. This does not affect the installed PostgreSQL Windows service.


