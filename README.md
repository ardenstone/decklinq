# Decklinq

Decklinq is a full-stack flashcard and study-deck application built with a .NET backend, a Next.js frontend, and PostgreSQL for persistence. It supports user authentication, deck management, card management, archive behavior, and a simple public/private deck model for study workflows.

## Overview

The project is organized into:

- `backend/` — ASP.NET Core Web API
- `frontend/` — Next.js application
- `docker-compose.yml` — local PostgreSQL + supporting services
- `backend.Tests/` — automated tests for API and business logic

## Stack and versions

The project is currently configured for:

- .NET: 10.0 (`net10.0` target framework)
- Node.js: 20.x (`node:20-alpine` in Docker)
- PostgreSQL: 16.x (`postgres:16` image)
- Next.js: 16.3.8
- React: 19.2.8
- ASP.NET Core: 10.0.12

## Project features

- JWT-based authentication and authorization
- User registration/login flow
- Deck CRUD operations
- Card creation and management within decks
- Deck/public visibility controls
- Archive support for cards while preserving history
- EF Core with PostgreSQL persistence
- API tests covering authentication and persistence behavior

## Local development setup

### Prerequisites

- .NET 10 SDK
- Node.js 20 LTS
- Docker Desktop or Docker engine with Compose
- Git

### Start PostgreSQL

From the project root:

```bash
docker compose up -d postgres pgweb
```

This starts:

- PostgreSQL on `localhost:5432`
- pgweb on `http://localhost:8081`

### Run the backend

```bash
cd backend
dotnet restore
dotnet run
```

The API runs on:

- `http://localhost:8080`

The backend uses the database connection string in `DB_CONNECTION_URL` or falls back to a local PostgreSQL connection string.

### Run the frontend

In a second terminal:

```bash
cd frontend
npm install
npm run dev
```

The frontend runs on:

- `http://localhost:3000`

### Run everything with Docker Compose

From the project root:

```bash
docker compose up --build
```

This starts the PostgreSQL database, backend, frontend, and pgweb together.

## Environment and URLs

Default development endpoints:

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:8080`
- PostgreSQL: `localhost:5432`
- pgweb UI: `http://localhost:8081`

### Default database credentials

- Database: `decklinq`
- User: `postgres`
- Password: `postgres`

## Project structure

```text
decklinq/
├── backend/
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Services/
│   ├── Program.cs
│   └── backend.csproj
├── backend.Tests/
├── frontend/
│   ├── src/
│   ├── package.json
│   └── next.config.ts
├── docker-compose.yml
├── Dockerfile
├── entrypoint.sh
├── decklinq.slnx
└── README.md
```

## Typical workflow

1. Start PostgreSQL.
2. Run the backend API.
3. Start the Next.js frontend.
4. Register/login through the frontend.
5. Create decks and cards for study sessions.

## Testing

Run the backend test suite from the project root:

```bash
dotnet test decklinq.slnx --nologo -v q
```

## Notes

- The backend uses EF Core and auto-applies migrations at startup.
- JWT secrets are configured with local development defaults for the app's current MVP setup.
- Docker-based development is the easiest way to get the Postgres dependency up and running quickly.
