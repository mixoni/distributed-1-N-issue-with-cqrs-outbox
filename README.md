# N+1 â†’ CQRS + Outbox POC (ASP.NET Core + Angular)

## Infra
docker compose -f backend/docker/docker-compose.yml up -d

## Services (each in its own terminal)
dotnet run --project backend/src/Customers.Api
dotnet run --project backend/src/Orders.Api
dotnet run --project backend/src/OutboxRelay.Worker
dotnet run --project backend/src/Orders.Read.Projector
dotnet run --project backend/src/Bff.Api

## Create sample orders
curl -X POST http://localhost:5002/api/orders -H "Content-Type: application/json" -d '{ "customerId": 1, "total": 99.5 }'
curl -X POST http://localhost:5002/api/orders -H "Content-Type: application/json" -d '{ "customerId": 2, "total": 120 }'

## Endpoints (BFF)
http://localhost:5000/api/orders/v1/naive
http://localhost:5000/api/orders/v1/batched
http://localhost:5000/api/orders/v2/read-model

## Angular
cd frontend/angular-ui
npm install
npm start
# open http://localhost:4200
