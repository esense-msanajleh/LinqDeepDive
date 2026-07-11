# LINQ Deep Dive - Clean Architecture + Angular UI

This project uses .NET 10, SQL Server, and Angular with a clean architecture backend.

## Project Structure

- `LinqDeepDive.Domain` - entities
- `LinqDeepDive.Application` - DTOs and demo use case service
- `LinqDeepDive.Infrastructure` - EF Core DbContext, SQL Server setup, seed data
- `LinqDeepDive.Api` - controllers and composition root
- `linq-deep-dive-ui` - single page Angular presentation + live demo UI

## Topics Covered

- IEnumerable vs IQueryable
- Deferred execution vs immediate execution
- LINQ to SQL translation in EF Core
- Filtering, projection, and chaining
- Common performance mistakes

## SQL Server

Default API connection string:

`Server=(localdb)\MSSQLLocalDB;Database=LinqDeepDiveDb;Trusted_Connection=True;TrustServerCertificate=True;`

Edit `LinqDeepDive.Api/appsettings.json` to use another SQL Server.

## Run Backend API

`dotnet run --project .\LinqDeepDive.Api\LinqDeepDive.Api.csproj`

API endpoint:

`GET /api/linqdemo/run`

## Run Angular UI

`cd .\linq-deep-dive-ui`

`npm install`

`npm start`

Open:

`http://localhost:4200`

Set API URL in UI, then click `Run Live Demo`.
