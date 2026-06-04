# SE Sport Agent Guidelines

## Setup
1. Copy `.env.example` to `.env` (adjust if needed)
2. Start PostgreSQL: `docker compose up -d` (run in WSL if Docker is only
available there)
3. Run database migrations:
   - PowerShell: `.\database\run-migrations.ps1` (run in WSL if Docker is only
     available there)
   - Bash: `./database/run-migrations.sh` (run in WSL if Docker is only
     available there)

## Building
- Build solution: `dotnet build`

## Running the Web Application
- After setup, run: `dotnet run --project src/SESport.Web`
- The web app will be available at http://localhost:5009

## Running Tests
- Run all tests: `dotnet test`
- To run tests for a specific project: `dotnet test tests/SESport.Core.Tests`

## Import Tools
Several console applications are available in the `tools` directory for data
import:
- `SESport.ImportEntities`: Imports entities from JSON/EPG data
- `SESport.ImportEpg`: Imports TV broadcast data
- `SESport.AIActivitySearch`: Performs AI-assisted activity search
- `SESport.ImportSmokeTest`: Verifies import functionality
- Run with: `dotnet run --project <tool-project-path>`

## Notes
- The solution targets .NET 10.0 SDK
- Database connection string defaults to
  Host=localhost;Port=5432;Database=sesport;Username=sesport;Password=sesport
- The web app uses Npgsql for PostgreSQL data access
- Ensure PostgreSQL is running and migrated before running the web app or
  import tools (PostgreSQL must be started via Docker in WSL if Docker is only
  available there)
- Hard rule: No lines in any file should exceed 80 characters wide unless it's
  required for the file to work.
