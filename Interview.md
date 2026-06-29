# Interview Prep Guide - Shortie Fullstack (.NET + React)

Use this document to prepare for graduate .NET interviews using this project as your portfolio case study.

## 1) 60-Second Pitch

I built a full-stack URL shortener using ASP.NET Core 9 and React.
On the backend, I used Entity Framework Core with SQL Server, ASP.NET Identity, JWT authentication, FluentValidation, Serilog logging, and Swagger.
I implemented URL creation, custom aliases, expiration, deletion, updates, QR code generation, and click analytics.
I also added refresh-token rotation hardening, role-based authorization, xUnit tests, Docker Compose for local infrastructure, and GitHub Actions CI for build/test/lint automation.

## 2) Architecture Talking Points

- API: ASP.NET Core Web API with layered folders (Controllers, Data, Entities, Services, Validators).
- Auth: ASP.NET Identity for user/password management and password hashing.
- Access control: JWT bearer authentication plus role policies (Admin/User).
- Data: EF Core with migrations and startup seeding.
- Analytics: click events tracked with browser/device/referrer/country fields.
- Frontend: React + Axios consuming authenticated API endpoints.
- DevOps: Dockerized SQL Server + API + frontend; CI pipeline validates quality on push/PR.

## 3) Security Points to Explain

- Password hashing and user lifecycle handled by ASP.NET Identity.
- JWT includes identity and role claims.
- Refresh tokens are stored hashed (not plaintext).
- Refresh token rotation is implemented: each refresh invalidates prior token and issues a new one.
- Reuse detection behavior: if a revoked token is reused, active session family is revoked.
- URL management endpoints are protected by role-aware policies.

## 4) Common Questions and Strong Answers

### Q: Why ASP.NET Identity instead of custom auth tables?
Identity reduces security mistakes by providing battle-tested user management, hashing, and token workflows.

### Q: How did you handle refresh token security?
I use one-time refresh tokens with rotation, store token hashes in the DB, revoke on logout, and detect token reuse to revoke active token family.

### Q: How do you manage schema changes?
I use EF Core migrations and apply them at startup using `Database.Migrate()`.

### Q: How do you ensure code quality?
I use xUnit tests for backend logic, ESLint for frontend checks, and GitHub Actions CI to enforce build/test/lint on every push and PR.

### Q: Why Docker Compose?
It provides a reproducible local environment for SQL Server, API, and frontend with consistent configuration for development and demos.

## 5) Demo Flow for Interviewers

1. Register or login with seeded demo account.
2. Create a short URL with custom alias and optional expiration.
3. Open redirect link and show it resolves.
4. View analytics (click count, browser/device/referrer/country, last accessed).
5. Show QR code endpoint and image output.
6. Mention Admin-only endpoint (`GET /api/urls/admin/all`).
7. Show CI pipeline file and Docker Compose setup.

## 6) Seeded Demo Accounts

- Admin: `admin@shortie.dev` / `Admin123!`
- User: `demo@shortie.dev` / `Demo123!`

(Use only for local/demo; change for production.)

## 7) Practice Checklist

- Explain OOP concepts used (encapsulation in services, separation of concerns by layers).
- Explain why validators are separate from controllers.
- Explain difference between authentication and authorization.
- Explain why migration-based schema is better than EnsureCreated for team projects.
- Explain one improvement you would make next (e.g., integration tests, rate limiting, caching).

## 8) Suggested Follow-up Enhancements (if asked)

- Add integration tests with Testcontainers for SQL Server.
- Add per-user/API rate limiting and abuse controls.
- Add observability with structured request tracing and dashboards.
- Add secure secrets handling with Azure Key Vault for deployment.
