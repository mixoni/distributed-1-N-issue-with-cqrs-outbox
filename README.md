# 🚀 N+1 Problem resolved by CQRS + Outbox Pattern (ASP.NET Core + Angular + RabbitMQ)

This proof-of-concept demonstrates how **N+1 queries** can severely impact performance in distributed systems — and how to eliminate them using:
- **Batched API calls**
- **CQRS (Command Query Responsibility Segregation)**
- **Outbox Pattern** for reliable event propagation via **RabbitMQ**

---

## 🧩 Architecture Overview

```
┌────────────┐      ┌────────────┐      ┌────────────┐
│ Customers  │◄────►│ RabbitMQ   │◄────►│ Orders     │
│   .Api     │      │ (Outbox)   │      │   .Api     │
└─────▲──────┘      └─────▲──────┘      └─────▲──────┘
      │                   │                 │
      │                   │                 │
      │             ┌─────┴──────┐          │
      │             │ Orders.Read│◄─────────┘
      │             │ .Projector │   (subscribes to Rabbit events)
      │             └─────▲──────┘
      │                   │
      ▼                   ▼
┌───────────────────────────────────────────┐
│                 BFF.Api                   │
│  /api/orders/v1/naive    → N+1 calls      │
│  /api/orders/v1/batched  → 1 batch call   │
│  /api/orders/v2/read     → CQRS read model│
└───────────────────────────────────────────┘
              │
              ▼
        Angular UI (4200)
```

---

## 🧰 Requirements

- .NET 8 SDK  
- Node.js 20+  
- Docker & Docker Compose  

---

## ⚙️ Setup

### 1. Start dependencies
```bash
docker compose -f backend/docker/docker-compose.yml up -d
```

It starts:
- PostgreSQL (Orders + Customers DBs)
- RabbitMQ (management UI → [http://localhost:15672](http://localhost:15672))

---

### 2. Start backend services (each in its own terminal)

```bash
dotnet run --project backend/src/Customers.Api
dotnet run --project backend/src/Orders.Api
dotnet run --project backend/src/OutboxRelay.Worker
dotnet run --project backend/src/Orders.Read.Projector
dotnet run --project backend/src/Bff.Api
```

---

### 3. Start Angular UI

```bash
cd frontend/angular-ui
npm install
npm start
```

Then open [http://localhost:4200](http://localhost:4200)

---

## 🌱 Seed Sample Data

You can seed orders with a simple script (WSL/Linux):

```bash
./seed-orders.sh 4000 30
```

or PowerShell:
```powershell
.\seed-orders.ps1 -Count 4000 -Concurrency 30
```

This creates ~4000 orders for 3 customers to make the N+1 problem visible.

---

## 👀 Observe the N+1 Problem

Open [http://localhost:4200](http://localhost:4200)

Switch between:
- 🟥 **Naive (N+1)** → 4001 HTTP calls  
- 🟨 **Batched** → 2 HTTP calls (Orders + Batch Customers)  
- 🟩 **CQRS Read** → 1 DB query (no network joins)

In the UI you’ll see:
- `Elapsed ms` — total request time  
- `Total HTTP`, `/customers/{id}`, `/customers/batch`, `/orders`, `/orders/read` — real backend call counts collected via BFF metrics headers.

---

## 🧠 How it Works

### 🧩 Outbox Pattern
When `Orders.Api` writes a new Order:
1. It also writes a record into the **Outbox** table within the same transaction.
2. `OutboxRelay.Worker` reads unprocessed events from the Outbox and publishes them to **RabbitMQ**.
3. `Orders.Read.Projector` subscribes to those events and updates the **OrdersRead** table.

### 🧩 CQRS Read Model
`OrdersRead` is a **denormalized view** optimized for queries — it already contains all data needed by the UI (e.g. CustomerName).  
If a customer’s name changes, `Customers.Api` emits a `CustomerRenamed` event, and the projector updates all affected rows.

---

## 🧪 API Endpoints

### BFF
| Mode | Endpoint | Description |
|------|-----------|-------------|
| Naive | `/api/orders/v1/naive` | N+1 HTTP calls to Customers |
| Batched | `/api/orders/v1/batched` | Single batch call |
| CQRS | `/api/orders/v2/read-model` | Reads denormalized model |

### Direct APIs
| Service | Example URL |
|----------|-------------|
| Customers.Api | `http://localhost:5001/api/customers` |
| Orders.Api | `http://localhost:5002/api/orders` |
| BFF.Api | `http://localhost:5000/swagger` |

---

## 🧮 Expected Results (≈4000 orders)

| Mode | Total HTTP | /customers/{id} | /customers/batch | /orders | /orders/read | Typical time |
|------|-------------|-----------------|------------------|----------|---------------|---------------|
| **Naive** | ~4002 | ~4001 | 0 | 1 | 0 | 3–5 s |
| **Batched** | 2 | 0 | 1 | 1 | 0 | ~100 ms |
| **CQRS** | 1 | 0 | 0 | 0 | 1 | ~20 ms |

---

## 🧾 Notes

- `--poll=2000` is set in `package.json` to make Angular hot-reload work on WSL/NTFS.
- All sample data and Docker volumes can be cleaned with:
  ```bash
  docker compose -f backend/docker/docker-compose.yml down -v
  ```

---

## 🏁 Summary

| Pattern | Strength | Weakness |
|----------|-----------|-----------|
| **Naive (N+1)** | simplest to implement | exponential network overhead |
| **Batched** | huge performance gain, minimal refactor | still synchronous aggregation |
| **CQRS + Outbox** | fastest, reliable, scalable | eventual consistency, more moving parts |

---

✅ **Result:** clear, measurable demonstration of how **CQRS + Outbox Pattern** eliminates the N+1 problem in a distributed .NET microservice environment.
