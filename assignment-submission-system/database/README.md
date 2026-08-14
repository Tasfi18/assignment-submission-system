# Database

PostgreSQL 14 or newer. Two scripts live here:

| File | Purpose |
| --- | --- |
| `schema.sql` | Creates every table, index and foreign key. Equivalent to the EF Core migration `20260801000000_InitialCreate`. |
| `seed.sql` | Inserts the demo accounts, classes, subjects, assignments and submissions. |

## You probably do not need these

The API creates the schema and seeds the demo data by itself on first start
(`Database:ApplyMigrationsOnStartup` and `Database:SeedDemoData` in `appsettings.json`, both `true`
by default). Create an empty database, point the connection string at it, run the API, and
everything is in place.

The scripts are here for the case where you would rather set the database up by hand, or want to
reset the demo data without restarting the API.

## Manual setup

```bash
createdb assignment_db

psql -d assignment_db -f database/schema.sql
psql -d assignment_db -f database/seed.sql
```

Or, if you connect with an explicit user and host:

```bash
psql "postgresql://postgres:postgres@localhost:5432/assignment_db" -f database/schema.sql
psql "postgresql://postgres:postgres@localhost:5432/assignment_db" -f database/seed.sql
```

Both scripts are idempotent (`CREATE TABLE IF NOT EXISTS`, `ON CONFLICT DO NOTHING`), so running
them twice is harmless.

`schema.sql` finishes by recording the migration in `__EFMigrationsHistory`, so the API will not
try to create the tables a second time when it starts.

## Applying migrations with the EF tooling instead

```bash
dotnet tool install --global dotnet-ef          # once
cd backend

dotnet ef database update \
  --project src/AssignmentSystem.Infrastructure \
  --startup-project src/AssignmentSystem.Api
```

To generate a fresh SQL script from the migrations:

```bash
dotnet ef migrations script \
  --project src/AssignmentSystem.Infrastructure \
  --startup-project src/AssignmentSystem.Api \
  --output ../database/schema-generated.sql
```

## Resetting the demo

```bash
dropdb assignment_db && createdb assignment_db
```

Then start the API again, or re-run the two scripts.

## Demo accounts

All passwords are stored as PBKDF2-HMAC-SHA256 hashes (100,000 iterations, per-password salt).

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@school.edu` | `Admin@123` |
| Teacher | `teacher@school.edu` | `Teacher@123` |
| Teacher | `rakib.hasan@school.edu` | `Teacher@123` |
| Student | `student@school.edu` | `Student@123` |
| Student | `sadia.islam@school.edu` | `Student@123` |
| Student | `mahdi.hasan@school.edu` | `Student@123` |
| Student | `farhana.akter@school.edu` | `Student@123` |
| Student | `imran.kabir@school.edu` | `Student@123` |
| Student | `nusaiba.rahman@school.edu` | `Student@123` |

## Quick checks

```sql
SELECT "Role", count(*) FROM "Users" GROUP BY "Role";
SELECT "Status", count(*) FROM "Assignments" GROUP BY "Status";
SELECT "Status", count(*) FROM "Submissions" GROUP BY "Status";
```

Table and column names are quoted PascalCase, because that is what EF Core generates by default.
Unquoted identifiers are folded to lower case by PostgreSQL, so `select * from Users` fails while
`SELECT * FROM "Users"` works.
