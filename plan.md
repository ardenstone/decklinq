Here is the updated project plan for DeckLinq using .NET 10, aligned to the repository structure and architecture we want to build.

---

## Project Structure

```text
/decklinq
│
├── /backend                     # ASP.NET Core Minimal API (C# / .NET 10)
│   ├── /Controllers/           # Optional endpoint groupings or route handlers
│   ├── /Models/                # EF Core entities and SRS domain models
│   ├── /Data/                  # DbContext, configuration, migrations
│   ├── /Services/              # Business logic: auth, SRS, Jev grading, imports
│   ├── /Extensions/            # Dependency injection and middleware setup
│   ├── /Program.cs             # App bootstrap, DI, routes, middleware
│   └── /appsettings*.json      # Environment-specific config
│
├── /frontend                   # Next.js app (TypeScript / App Router)
│   ├── /app/                   # Pages, layouts, route segments
│   ├── /components/            # Shared UI components
│   ├── /lib/                   # API clients and helpers
│   ├── /store/                 # Zustand state
│   ├── /types/                 # Shared frontend types
│   └── package.json
│
├── docker-compose.yml          # Local orchestration for backend, frontend, and PostgreSQL
├── Dockerfile                  # .NET 10 backend container with Flyway support
├── README.md
└── .env.example
```

---

## Part 1: Backend Development Plan (.NET 10 / ASP.NET Core)

### 1. Architecture & Core Design

- **Framework:** .NET 10 ASP.NET Core using the Minimal API pattern.
- **Pattern:** Clean service-layer architecture with route handlers separated from business logic.
- **Database Access:** Entity Framework Core with PostgreSQL provider (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- **Authentication:** JWT-based authentication with secure refresh-token handling or cookie-based auth for session flows if desired.
- **Configuration:** Environment-driven config via `appsettings.json`, `appsettings.Development.json`, and Docker environment variables.

### 2. Backend Folder Responsibilities

- **`backend/Program.cs`**: App startup, route registration, JSON configuration, middleware, dependency injection, and health checks.
- **`backend/Controllers/`**: Optional endpoint grouping for auth, decks, cards, and reviews if the team prefers controller-based routing over pure minimal API functions.
- **`backend/Models/`**: Domain entities such as `User`, `Deck`, `Card`, `ReviewLog`, and SRS-related types.
- **`backend/Data/`**: `AppDbContext`, EF Core migrations, model configuration, and database initialization logic.
- **`backend/Services/`**: Auth service, review/SRS engine, Jev AI grading client, import/export service, and data access helpers.
- **`backend/Extensions/`**: Startup helpers for CORS, auth, DB setup, and service registration.

### 3. Database Schema Design

- **`Users`**: Id, Username, PasswordHash, CreatedAt.
- **`Decks`**: Id, Title, Description, IsPublic, UserId, CreatedAt, UpdatedAt.
- **`Cards`**: Id, DeckId, FrontContent, BackContent, Hints, IsLaTeX, CreatedAt.
- **`ReviewLogs`**: Id, CardId, UserId, Score (1-4), Interval, EaseFactor, ReviewedAt.
- **Optional future fields**: `LastReviewedAt`, `NextReviewAt`, `StudyMode`, `DeckCategory`, `SourceImportId`.

### 4. Key Services & Modules

- **SRS Engine:** Implements spaced repetition interval calculations based on SM-2 or a close derivative.
- **Jev AI Grading Client:** Typed HTTP client that calls the TypeSafe Jev API for semantic evaluation of free-text answers.
- **Import/Export Service:** Handles JSON and Anki/Quizlet CSV ingestion and deck export flows.
- **Auth Service:** User registration, login, password hashing, token issuance, and role checks.
- **Review Service:** Applies score updates, recalculates card scheduling, and stores review history.

### 5. API Endpoint Structure

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/decks`
- `POST /api/decks`
- `GET /api/decks/{id}`
- `PUT /api/decks/{id}`
- `DELETE /api/decks/{id}`
- `GET /api/decks/{id}/cards`
- `POST /api/decks/{id}/cards`
- `PUT /api/cards/{id}`
- `DELETE /api/cards/{id}`
- `POST /api/cards/{id}/review`
- `GET /api/users/me`

### 6. .NET 10 Implementation Notes

- Use the .NET 10 SDK and ASP.NET Core runtime tags in Dockerfiles and CI pipelines.
- Prefer `MapGroup("/api")` for route grouping and keep logic in services for testability.
- Integrate health checks for database and external API dependencies.
- Add structured logging and request correlation IDs for observability.

---

## Part 2: Frontend Development Plan (Next.js / TypeScript)

### 1. Architecture & Tech Stack

- **Framework:** Next.js (App Router) using TypeScript.
- **Styling:** Tailwind CSS + Lucide Icons.
- **State Management:** Zustand for auth/session state and active study settings.
- **Data Fetching:** TanStack Query for caching, background sync, optimistic updates, and study-session state.
- **API Layer:** Centralized fetch wrappers around the backend routes.

### 2. Core Views & Pages

- **Dashboard (`/app/dashboard/page.tsx`)**: Overview of user decks, quick-access study buttons, and streak/progress summaries.
- **Deck Workspace (`/app/decks/[id]/page.tsx`)**: Displays cards, edit actions, deck metadata, and card creation modals.
- **Study Session (`/app/decks/[id]/study/page.tsx`)**: Card-flip flow, keyboard shortcuts, written-answer grading, and review feedback.
- **Public Deck Marketplace (`/app/explore/page.tsx`)**: Search, browse, and clone public decks.
- **Auth Screens**: Login, registration, and protected-route redirects.

### 3. Frontend State Strategy

- Keep the app stateless where possible and fetch server data with React Query.
- Use Zustand for user session information and global UI preferences.
- Use route guards to redirect unauthenticated users to the login flow.

---

## Part 3: .NET 10 Dockerfile with Flyway Integration

To bundle Flyway migrations directly into the .NET 10 backend container, we use a multi-stage Docker build. The runtime image includes both the ASP.NET Core app and the Flyway CLI so migrations can run automatically before app startup.

### `Dockerfile`

```dockerfile
# Stage 1: Build the .NET application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["backend/backend.csproj", "backend/"]
RUN dotnet restore "backend/backend.csproj"

COPY . .
WORKDIR "/src/backend"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime image with .NET and Flyway CLI
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    openjdk-17-jre-headless \
    curl \
    bash \
    && rm -rf /var/lib/apt/lists/*

COPY --from=flyway/flyway:10.17.0 /flyway /opt/flyway
ENV PATH="/opt/flyway:${PATH}"

COPY --from=build /app/publish .
COPY ./backend/Data/Migrations /app/migrations
COPY ./entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["/app/entrypoint.sh"]
```

### Supporting `entrypoint.sh` Script

```bash
#!/bin/bash
set -e

echo "Running database migrations via Flyway..."
flyway \
  -url="${DB_CONNECTION_URL}" \
  -user="${DB_USER}" \
  -password="${DB_PASSWORD}" \
  -locations="filesystem:/app/migrations" \
  migrate

echo "Migrations complete. Starting DeckLinq API..."
exec dotnet backend.dll
```

---

## Docker Compose Setup

```yaml
version: "3.9"

services:
  postgres:
    image: postgres:16
    container_name: decklinq-db
    environment:
      POSTGRES_DB: decklinq
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  backend:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      DB_CONNECTION_URL: "Host=postgres;Database=decklinq;Username=postgres;Password=postgres"
      DB_USER: postgres
      DB_PASSWORD: postgres
      ASPNETCORE_ENVIRONMENT: Development
    depends_on:
      - postgres
    ports:
      - "8080:8080"

  frontend:
    image: node:20-alpine
    working_dir: /app
    command: sh -c "npm install && npm run dev"
    volumes:
      - ./frontend:/app
    ports:
      - "3000:3000"
    depends_on:
      - backend

volumes:
  postgres_data:
```

---

## Recommended Next Steps

1. Initialize the .NET 10 backend project under `backend/`.
2. Scaffold the Next.js frontend under `frontend/`.
3. Create the PostgreSQL schema and EF Core migration pipeline.
4. Implement the auth and deck-management endpoints.
5. Build the study loop and review scoring engine.
6. Add Docker compose orchestration and run the full stack locally.

This keeps the project aligned with the repository layout while still following the original DeckLinq product plan and using .NET 10 as the backend target.
