# Astra Syndicate: Fleet Wars - Backend Service (.NET 9)
> **MK CREATIVE Agency — Internal Technical Operations**  
> **Role**: Backend Engineer  
> **Assignee**: Mahmoud Mabrouk  
> **Sprint Focus**: Offline & Client Architecture (Sprint 1 Kickoff)

---

## 🚀 Overview

This repository contains the backend architecture and REST API for **Astra Syndicate: Fleet Wars**, engineered in **.NET 9 (C# / ASP.NET Core Web API)** to support offline-first mobile/desktop gameplay with asynchronous conflict-free cloud state synchronization.

### Sprint 1 Deliverables Checklist

| Deliverable ID | Description | Output Format | Status | Location |
| :--- | :--- | :--- | :---: | :--- |
| **BE-01** | Supabase DB Schema & Free-Tier API Environment | SQL / .NET 9 |  Completed | [`database/migrations/`](file:///c:/Users/dell/Desktop/Sprint%20One/database/migrations/) & [`src/AstraFleetWars.Api/`](file:///c:/Users/dell/Desktop/Sprint%20One/src/AstraFleetWars.Api/) |
| **BE-02** | Offline-to-Online Sync Payload Specification | OpenAPI 3.0 / Spec Doc |  Completed | [`docs/BE-02_OFFLINE_SYNC_SPECIFICATION.md`](file:///c:/Users/dell/Desktop/Sprint%20One/docs/BE-02_OFFLINE_SYNC_SPECIFICATION.md) & [`/api-docs`](http://localhost:5000/api-docs) |
| **BE-03** | Account Auth & Profile Sync Service | Deployed / Runnable API |  Completed | [`src/AstraFleetWars.Api/`](file:///c:/Users/dell/Desktop/Sprint%20One/src/AstraFleetWars.Api/) |

---

## 🛠️ Architecture & Tech Stack

* **Framework**: .NET 9 (C# 13, ASP.NET Core Web API)
* **API Documentation**: OpenAPI 3.0 + Swagger UI (`Swashbuckle.AspNetCore`) at `/api-docs`
* **Security & Auth**: JWT (JSON Web Tokens) with Guest & Commander login
* **Data Access**: Repository Pattern (`IPlayerRepository`) with thread-safe `InMemoryPlayerRepository` for local development and PostgreSQL / Supabase SQL migrations
* **Testing**: xUnit test suite (`tests/AstraFleetWars.Tests/`)
* **Deployment**: Multi-stage Docker container (`Dockerfile`, `render.yaml`) for Render & Railway free-tier

---

## 🏁 Quick Start Guide

### 1. Build Solution
```bash
dotnet build
```

### 2. Run Automated xUnit Tests
```bash
dotnet test
```
Tests all 4 conflict resolution rules, anti-cheat bounds, and idempotency guarantees.

### 3. Run Web API Server
```bash
dotnet run --project src/AstraFleetWars.Api
```
The server will start at `http://localhost:5000`:
* **Interactive Swagger UI**: [http://localhost:5000/api-docs](http://localhost:5000/api-docs)
* **Health Check**: [http://localhost:5000/health](http://localhost:5000/health)

*(Note: Automatically runs in Local Mock Mode with demo commander `commander_reyes` if Supabase keys are not set!)*

---

## ⚡ Core Sync Protocol & Conflict Resolution (Task 1.2)

1. **Chapter Progression (High-Water Mark)**:
   * `stars = Math.Max(server.Stars, client.Stars)`
   * `high_score = Math.Max(server.HighScore, client.HighScore)`
   * Player never loses earned stars or score even when playing offline on older builds.
2. **Achievements (Additive Set Union)**:
   * $\text{State} = \text{ServerAchievements} \cup \text{ClientAchievements}$
3. **Fleet Loadout (Optimistic Revision Locking)**:
   * If `client.RevisionVersion >= server.RevisionVersion`: Loadout accepted and version bumped.
   * If `client.RevisionVersion < server.RevisionVersion`: Stale loadout rejected, preserving latest fleet.
4. **Anti-Cheat Currency Bounding**:
   * Offline credit gain capped at 50,000 per batch.
5. **Idempotency**:
   * Repeated submissions with the same UUID `sync_id` return the cached authoritative state without double-rewarding.

---

## 📡 API Endpoints

| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :---: |
| `GET` | `/health` | API Health Status | No |
| `GET` | `/api-docs` | Interactive Swagger UI Documentation | No |
| `POST` | `/api/v1/auth/guest` | Anonymous guest session | No |
| `POST` | `/api/v1/auth/login` | Login with username (e.g. `commander_reyes`) | No |
| `POST` | `/api/v1/auth/register` | Register new commander profile | No |
| `GET` | `/api/v1/player/profile` | Authoritative player profile & metadata | **Bearer JWT** |
| `POST` | `/api/v1/sync` | **Offline-to-Online Sync with Conflict Resolution** | **Bearer JWT** |
| `GET` | `/api/v1/sync/status` | Current sync revision & timestamp | **Bearer JWT** |
| `POST` | `/api/v1/webhooks/log` | **Zero-Cost Passive Telemetry (202 Accepted)** | No |
| `GET` | `/api/v1/webhooks/logs` | View recent telemetry logs | No |
