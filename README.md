## Running with Docker

From the `CarRental` directory, start the API and PostgreSQL with:

```bash
docker compose up --build
```

The API applies pending EF Core migrations automatically only in the local `Development` Docker environment, including the `btree_gist` exclusion constraint migration. This convenience is not intended as a production migration strategy.

Swagger is available at [http://localhost:8080/swagger](http://localhost:8080/swagger).

Stop the environment with `docker compose down`. PostgreSQL data is stored in the `postgres_data` named volume and remains after that command. To remove the local database as well, run:

```bash
docker compose down -v
```

PostgreSQL is also exposed on `localhost:5432` for optional inspection with a local database client. Its Docker-only development credentials are `postgres` / `postgres`.