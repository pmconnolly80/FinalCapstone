# Beer App

## Local development

### With Docker

```bash
docker compose up --build
```

- Frontend: http://localhost:3000
- API: http://localhost:5153/swagger
- Database: localhost:5432

### Without Docker

- Start PostgreSQL locally or update the connection string.
- Run the API with `dotnet run` in the backend folder.
- Run the frontend with `npm install && npm run dev` in the frontend folder.
