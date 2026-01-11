# Enterprise Application - Lawgate Projects

## Overview
Production-ready enterprise application with React frontend, .NET backend, and PostgreSQL database, optimized for Azure deployment.

## Project Structure
```
.
├── frontend/          # React + Tailwind CSS
├── backend/           # .NET 8 Web API
├── database/          # PostgreSQL migrations & seeds
├── docker/            # Docker configurations
└── docs/              # Project-wide documentation
```

## Quick Start

### Prerequisites
- Node.js 20+ and npm
- .NET 8 SDK
- Docker Desktop
- PostgreSQL (or use Docker)

### Initial Setup
```powershell
# 1. Start PostgreSQL (via Docker)
docker-compose up -d

# 2. Setup Backend
cd backend
dotnet restore
dotnet ef database update
dotnet run

# 3. Setup Frontend
cd ../frontend
npm install
npm run dev
```

### Database Recreation (After Long Break)
```powershell
# This is THE solution to your database recreation problem!
cd database
./recreate-database.ps1
```

## Technology Stack
- **Frontend**: React 18, Tailwind CSS, Vite, React Router, Axios
- **Backend**: .NET 8, Entity Framework Core, JWT Auth, Swagger
- **Database**: PostgreSQL 16
- **Deployment**: Azure App Service, Azure Database for PostgreSQL
- **DevOps**: Docker, GitHub Actions

## Documentation
- [Frontend Documentation](./frontend/docs/)
- [Backend Documentation](./backend/docs/)
- [Database Documentation](./database/docs/)
- [Azure Deployment Guide](./docs/azure-deployment.md)

## Environment Variables
Copy `.env.example` files in each directory and configure:
- `frontend/.env.local`
- `backend/appsettings.Development.json`

## License
Proprietary - All rights reserved
