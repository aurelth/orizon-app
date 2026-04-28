# 🌅 Orizon

> **Your day, before it begins.**

Orizon is an AI-powered morning briefing app that runs automatically every day at **6:00 AM** and delivers a consolidated daily summary including Gmail emails (AI-summarized), Google Calendar meetings, Trello tasks, and local weather forecast — all in one clean dashboard and email digest.

![CI Backend](https://github.com/SEU_USUARIO/orizon-app/actions/workflows/ci-backend.yml/badge.svg)
![CI Frontend](https://github.com/SEU_USUARIO/orizon-app/actions/workflows/ci-frontend.yml/badge.svg)
![License](https://img.shields.io/badge/license-Proprietary-red)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)
![Angular](https://img.shields.io/badge/Angular-21-DD0031)

---

## ✨ Features

- **📧 Gmail digest** — AI-summarized emails from the last 24h, categorized by priority
- **📅 Calendar overview** — Today's meetings with time, participants and Meet links
- **📌 Trello tasks** — Cards grouped by board, showing "Today" and "In Progress" columns with time tracking
- **🌤️ Weather forecast** — Hourly precipitation, min/max temperature, wind and smart alerts
- **🤖 AI suggestions** — Claude-powered cross-context insights combining all data sources
- **📩 Email digest** — Beautiful HTML email delivered at 6AM via SendGrid
- **🔔 Real-time notifications** — SignalR push when briefing is ready
- **🌍 Travel mode** — Automatic location switching when traveling
- **🌙 Dark theme** — Dark by default, switchable to light in settings

---

## 🏗️ Architecture

Orizon follows **Clean Architecture** principles with clear separation of concerns:

```
Orizon.Domain          # Entities, enums, domain rules — no dependencies
Orizon.Application     # Use cases, interfaces, DTOs — depends on Domain
Orizon.Infrastructure  # EF Core, external APIs, Redis — depends on Application
Orizon.API             # ASP.NET Core Web API — depends on Infrastructure
Orizon.Worker          # Hangfire background jobs — depends on Infrastructure
```

---

## 🛠️ Tech Stack

### Backend
| Technology | Purpose |
|------------|---------|
| .NET 9 + ASP.NET Core | Web API and Worker Service |
| Entity Framework Core 9 | ORM with PostgreSQL |
| Hangfire | Job scheduling (cron 0 6 * * *) |
| ASP.NET Core Identity | User management |
| JWT + OAuth 2.0 (Google) | Authentication |
| SignalR | Real-time notifications |
| Redis 7 | Caching and session store |
| Serilog + Seq | Structured logging |
| OpenTelemetry + Prometheus | Observability and metrics |

### Frontend
| Technology | Purpose |
|------------|---------|
| Angular 21 | SPA framework |
| PrimeNG | UI component library |
| NgRx Signals | State management |
| ngx-apexcharts | Charts and visualizations |
| @microsoft/signalr | Real-time client |

### External APIs
| API | Purpose | Cost |
|-----|---------|------|
| Google Gmail API | Email data | Free |
| Google Calendar API | Calendar events | Free |
| Trello REST API | Task management | Free |
| Open-Meteo | Weather forecast | Free, no key needed |
| Anthropic Claude API | AI summarization | Pay per use |
| SendGrid | Email delivery | 100/day free |

### Infrastructure
| Service | Purpose |
|---------|---------|
| PostgreSQL 16 | Primary database |
| Redis 7 | Cache layer |
| Seq | Log aggregation |
| Prometheus | Metrics collection |
| Grafana | Metrics visualization |
| Docker Compose | Local infrastructure |
| GitHub Actions | CI/CD pipelines |
| Nginx | Reverse proxy (production) |

---

## 📁 Project Structure

```
orizon-app/
├── src/
│   ├── backend/                  # .NET 9 solution
│   │   ├── Orizon.Domain/
│   │   ├── Orizon.Application/
│   │   ├── Orizon.Infrastructure/
│   │   ├── Orizon.API/
│   │   ├── Orizon.Worker/
│   │   ├── Orizon.Tests.Unit/
│   │   └── Orizon.Tests.Integration/
│   └── frontend/                 # Angular 21 app
│       └── orizon-frontend/
├── infra/
│   ├── docker/                   # Dockerfiles
│   ├── nginx/                    # Reverse proxy config
│   ├── prometheus/               # Metrics config
│   ├── grafana/                  # Dashboards and datasources
│   └── scripts/                  # Backup and utility scripts
├── docs/                         # Project documentation
├── .github/
│   └── workflows/                # CI/CD pipelines
├── docker-compose.yml            # Infrastructure services
├── docker-compose.override.yml   # Local dev overrides
├── .env.example                  # Environment variables template
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

Make sure you have the following installed:

- [Git](https://git-scm.com/)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js LTS](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (with ASP.NET workload)

### 1. Clone the repository

```bash
git clone https://github.com/SEU_USUARIO/orizon-app.git
cd orizon-app
```

### 2. Configure environment variables

```bash
cp .env.example .env
# Edit .env with your actual API keys and credentials
```

### 3. Start infrastructure services

```bash
docker compose up -d
```

This starts: PostgreSQL, Redis, Seq, Prometheus and Grafana.

### 4. Run database migrations

```bash
cd src/backend
dotnet ef database update --project Orizon.Infrastructure --startup-project Orizon.API
```

### 5. Run the backend

```bash
# API
cd src/backend/Orizon.API
dotnet run

# Worker (separate terminal)
cd src/backend/Orizon.Worker
dotnet run
```

### 6. Run the frontend

```bash
cd src/frontend/orizon-frontend
npm install
ng serve
```

The app will be available at `http://localhost:4200`.

---

## 🔧 Local Services

| Service | URL | Credentials |
|---------|-----|-------------|
| Orizon App | http://localhost:4200 | — |
| API | http://localhost:5000 | — |
| Hangfire Dashboard | http://localhost:5001/hangfire | Admin login |
| Seq (Logs) | http://localhost:80 | — |
| Prometheus | http://localhost:9090 | — |
| Grafana | http://localhost:3000 | admin / see .env |
| PostgreSQL | localhost:5432 | see .env |
| Redis | localhost:6379 | see .env |

---

## 🗺️ Roadmap

| Version | Phase | Status |
|---------|-------|--------|
| v0.1.0 | Repository, Git and base structure | ✅ Done |
| v0.2.0 | Docker Compose infrastructure | 🔄 In progress |
| v0.3.0 | CI — GitHub Actions | ⏳ Pending |
| v0.4.0 | .NET 9 Clean Architecture | ⏳ Pending |
| v0.5.0 | Authentication (JWT + OAuth) | ⏳ Pending |
| v0.6.0 | External integrations | ⏳ Pending |
| v0.7.0 | Hangfire briefing job | ⏳ Pending |
| v0.8.0 | Angular 21 frontend | ⏳ Pending |
| v0.9.0 | Observability | ⏳ Pending |
| v0.10.0 | Tests | ⏳ Pending |
| v1.0.0 | Production deploy 🚀 | ⏳ Pending |

---

## 📄 License

This project is proprietary software. See [LICENSE](LICENSE) for full terms.

Source code is available for viewing and educational purposes only.
Any use, copying, modification, or distribution is strictly prohibited
without prior written permission from the copyright holder.

© 2026 Aurel. All Rights Reserved.

---

<div align="center">
  <sub>Built with ☕ and lots of early mornings.</sub>
</div>
