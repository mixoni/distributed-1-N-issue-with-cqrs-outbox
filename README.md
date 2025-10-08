# 1+N Problem Resolved by CQRS + Outbox  
*(ASP.NET Core + Angular + PostgreSQL + RabbitMQ)*

---

## 🧩 Project Overview

This proof of concept demonstrates how to solve the **1+N problem** using **CQRS** (Command Query Responsibility Segregation) combined with the **Outbox Pattern** and a **BFF (Backend for Frontend)** architecture.

### 🔍 Key Concepts
- **CQRS** separates read and write responsibilities across distinct services.
- **Outbox Pattern** ensures reliable event publishing without data inconsistency.
- **BFF (Backend for Frontend)** optimizes data access for the Angular UI.
- **RabbitMQ** acts as the message broker between write and read models.
- **PostgreSQL** is used as the transactional and read-model database.

### 🧠 Flow Summary
1. **Orders.Api** saves an order and writes an event to the **Outbox** table.  
2. **OutboxRelay.Worker** publishes events to **RabbitMQ**.  
3. **Orders.Read.Projector** consumes those messages and updates the **Read Model**.  
4. The **BFF.Api** queries the read model efficiently — eliminating the 1+N problem.  
5. The **Angular UI** visualizes results in real-time.

---

## 🏗️ Infrastructure Setup

Start all infrastructure services using Docker:

```bash
docker compose -f backend/docker/docker-compose.yml up -d
```

---

## 🚀 Running Services

Run each backend service in a separate terminal:

```bash
dotnet run --project backend/src/Customers.Api
dotnet run --project backend/src/Orders.Api
dotnet run --project backend/src/OutboxRelay.Worker
dotnet run --project backend/src/Orders.Read.Projector
dotnet run --project backend/src/Bff.Api
```

---

## 🧪 Create Sample Orders

Use `curl` to create sample data:

```bash
curl -X POST http://localhost:5002/api/orders      -H "Content-Type: application/json"      -d '{ "customerId": 1, "total": 99.5 }'

curl -X POST http://localhost:5002/api/orders      -H "Content-Type: application/json"      -d '{ "customerId": 2, "total": 120 }'
```

---

## 🌐 BFF Endpoints

Compare different approaches:

- [Naive Query (1+N issue)](http://localhost:5000/api/orders/v1/naive)  
- [Batched Query (Improved)](http://localhost:5000/api/orders/v1/batched)  
- [Read Model Query (CQRS + Outbox)](http://localhost:5000/api/orders/v2/read-model)

---

## 🖥️ Angular Frontend

Start the Angular UI:

```bash
cd frontend/angular-ui
npm install
npm start
```

Then open the app in your browser:  
👉 [http://localhost:4200](http://localhost:4200)

---

## 📚 Tech Stack

| Layer | Technology |
|-------|-------------|
| Backend | ASP.NET Core 8, CQRS, MediatR, EF Core |
| Messaging | RabbitMQ |
| Database | PostgreSQL |
| Frontend | Angular |
| Infrastructure | Docker Compose |
| Patterns | Outbox, BFF, Event-Driven Architecture |

---

## ✅ Expected Result

When running all components:
- Orders are created and propagated through the Outbox → RabbitMQ → Read Model pipeline.  
- The BFF endpoints will show improved query performance and eliminate 1+N queries.  
- The Angular UI will display order data in near real-time using the CQRS read model.

---

*Author: Miljan Janković*  
*© 2025 — Educational POC for CQRS + Outbox Architecture*
