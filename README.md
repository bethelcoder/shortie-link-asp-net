# Shortie Fullstack (.NET + React)

Portfolio-ready full-stack URL shortener tailored for Graduate .NET developer interviews.

## Tech Stack

Backend
- ASP.NET Core 9 Web API
- C#
- Entity Framework Core + SQL Server
- ASP.NET Identity
- JWT (access + refresh tokens)
- FluentValidation
- Serilog
- Swagger
- xUnit

Frontend
- React (Vite)
- Axios

DevOps
- Docker + Docker Compose
- GitHub Actions CI

## Features

Authentication
- Register
- Login
- JWT access token
- Refresh token rotation
- Logout token revocation
- Password hashing via ASP.NET Identity
- Role-based authorization (`Admin`, `User`)

URL Management
- Create short URL
- Custom alias
- Expiration date
- Update URL
- Delete URL
- List own URLs
- Admin list-all endpoint
- QR code generation

Analytics
- Click count
- Browser
- Device
- Country (header-based fallback)
- Referrer
- Last accessed

## Project Structure

- [ShortieFullstack.sln](ShortieFullstack.sln)
- [Shortie.Api](Shortie.Api)
- [Shortie.Api.Tests](Shortie.Api.Tests)
- [shortie-web](shortie-web)
- [docker-compose.yml](docker-compose.yml)
- [.github/workflows/ci.yml](.github/workflows/ci.yml)

## Quick Start (Local)

Recommended first run path:
- Use Docker Compose first. It avoids local SQL Server installation issues.
- After Docker works, optionally switch to local SQL Server setup.

### 1) Backend

Update [Shortie.Api/appsettings.json](Shortie.Api/appsettings.json):
- `ConnectionStrings:DefaultConnection`
- `Jwt:Key` (must be strong and long)

Run API:

```powershell
dotnet run --project Shortie.Api
```

API docs:
- `http://localhost:<port>/swagger`
- `https://localhost:<port>/swagger`

### 2) Frontend

Create [shortie-web/.env](shortie-web/.env):

```env
VITE_API_BASE_URL=http://localhost:5000
```

Run web app:

```powershell
cd shortie-web
npm install
npm run dev
```

## Database Migrations + Seeding

- EF migration files are in [Shortie.Api/Data/Migrations](Shortie.Api/Data/Migrations).
- Startup applies migrations with `Database.Migrate()`.
- Startup seeds:
	- Roles: `Admin`, `User`
	- Demo users
	- Sample short links

Seeded accounts:
- Admin: `admin@shortie.dev` / `Admin123!`
- User: `demo@shortie.dev` / `Demo123!`

## Build, Lint, Test

From repo root:

```powershell
dotnet build ShortieFullstack.sln
dotnet test ShortieFullstack.sln
```

Frontend checks:

```powershell
cd shortie-web
npm ci
npm run lint
npm run build
```

## Docker Run (Full Stack)

From repo root:

```powershell
docker compose up --build
```

Services:
- Frontend: `http://localhost:3000`
- API: `http://localhost:5000/swagger`
- SQL Server: `localhost:1433`

Stop:

```powershell
docker compose down
```

## First-Time Setup Checklist

1. Clone/open the repository and move to the root folder.
2. Set a real JWT key:
	 - Update Jwt__Key in [docker-compose.yml](docker-compose.yml) for Docker mode.
	 - Update Jwt:Key in [Shortie.Api/appsettings.json](Shortie.Api/appsettings.json) for local mode.
3. Run Docker mode first:
	 - docker compose up --build
4. Open Swagger:
	 - http://localhost:5000/swagger
5. Login with seeded user:
	 - admin@shortie.dev / Admin123!
	 - demo@shortie.dev / Demo123!

## SQL Server Access

Docker mode SQL connection details:
- Host: localhost
- Port: 1433
- Username: sa
- Password: Your_strong_password123
- Database: ShortieDb

Connect with SSMS/Azure Data Studio:
- Server: localhost,1433
- Authentication: SQL Login
- User: sa
- Password: Your_strong_password123
- Trust server certificate: enabled

Connect using sqlcmd (inside container):

	docker exec -it shortie-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "Your_strong_password123"

Then run queries, for example:

	SELECT name FROM sys.tables;
	GO

## SQL Schema Creation

Everything is already in place.

- Schema is created automatically by EF Core migrations on API startup.
- Startup calls Database.Migrate, so tables are created/updated when the API starts.
- Initial migration files are already committed in [Shortie.Api/Data/Migrations](Shortie.Api/Data/Migrations).

If you ever need to create a new schema migration manually:

	dotnet ef migrations add YourMigrationName --project .\Shortie.Api\Shortie.Api.csproj --startup-project .\Shortie.Api\Shortie.Api.csproj --output-dir Data\Migrations

If you need to apply migrations manually:

	dotnet ef database update --project .\Shortie.Api\Shortie.Api.csproj --startup-project .\Shortie.Api\Shortie.Api.csproj

Note:
- For local non-Docker SQL mode, install SQL Server (or LocalDB) and update DefaultConnection in [Shortie.Api/appsettings.json](Shortie.Api/appsettings.json).

## CI Pipeline

GitHub Actions workflow: [.github/workflows/ci.yml](.github/workflows/ci.yml)

Pipeline runs on push/PR:
- .NET restore/build/test
- Frontend npm ci/lint/build

## API Notes

Key endpoints:
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/urls`
- `GET /api/urls/admin/all` (Admin only)
- `POST /api/urls`
- `PUT /api/urls/{id}`
- `DELETE /api/urls/{id}`
- `GET /api/urls/{id}/analytics`
- `GET /api/urls/{id}/qrcode`
- `GET /r/{code}`

## Interview Prep

See [Interview.md](Interview.md) for:
- project pitch
- architecture talking points
- security explanations
- common interview Q&A
- demo walkthrough
