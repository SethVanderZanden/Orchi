---
name: ef-migrations
description: >-
  Require Entity Framework Core migrations to be generated with `dotnet ef`,
  never handwritten. Use when adding or changing EF migrations, AppDbContext,
  entity schema, Migrations/*.cs, model snapshot, or when build locks block
  `dotnet ef migrations add`.
---

# EF migrations (generate, don't handwrite)

## Rule

**Never handwrite** EF migration files (`Migrations/*.cs`, Designer, or `AppDbContextModelSnapshot.cs` edits) unless **all** of the following are true:

1. Generated scaffolding is impossible or wrong after genuine troubleshooting
2. You have explained why to the human
3. The human **explicitly approved** handwriting this migration

Until then: unblock `dotnet ef` and regenerate. Do not "save time" with a manual Up/Down stub.

## Required workflow

1. Update entities / `AppDbContext` / fluent config first.
2. Ensure the API project builds (`dotnet build src/API`).
3. Generate:

```powershell
dotnet ef migrations add <MigrationName> --project src/API --output-dir Migrations
```

4. Review the generated Up/Down + snapshot. Do not invent a parallel hand-written file with the same class name.
5. Apply when appropriate: `dotnet ef database update --project src/API` (API auto-migrates on startup in normal local runs).

## If `dotnet ef` fails

| Symptom | Fix |
|---------|-----|
| Build failed / `Orchi.Api.exe` locked | Stop the running API process, then rebuild and re-run `dotnet ef` |
| Name already used | Remove the conflicting incomplete migration (`dotnet ef migrations remove` if it was the last generated one, or delete the bad stub **and** ensure snapshot is consistent), then add again with a new name if needed |
| Tool missing | Use installed `dotnet-ef`; do not substitute a hand-written migration |

**Do not** create a stub `YYYYMMDDHHMMSS_Name.cs` with only `AddColumn`/`DropColumn` as a workaround for a file lock or failed build.

## Handwritten exception (human-approved only)

If the human approved handwriting:

- Mirror the style of the latest **generated** migration in the repo
- Update `AppDbContextModelSnapshot.cs` to match the model (incomplete stubs without snapshot/Designer are forbidden)
- Prefer fixing generation next time over normalizing more hand-written migrations

## Anti-patterns

- Writing `Migrations/*_Something.cs` because the agent was blocked mid-task
- Leaving a partial migration that blocks `dotnet ef migrations add` with "name is used by an existing migration"
- Editing the snapshot by hand while also planning to run `dotnet ef` later (pick one path; prefer generate)
