---
name: entity-framework
description: "Use when: Entity Framework Core tasks in this project (model annotations, DbContext changes, migrations, SQLite setup)."
argument-hint: "Describe the EF change or migration task."
---

# Entity Framework Skill

## When to Use
- Add or update EF Core annotations on model classes
- Adjust relationships or foreign keys in `AppDbContext`
- Create or update EF migrations
- Verify or update SQLite connection setup

## Procedure
1. Inspect the affected model classes and the `AppDbContext` mapping.
2. Apply EF-ready annotations (`[Key]`, `[ForeignKey]`, `[Required]`) and navigation properties where needed.
3. Update fluent mapping in `AppDbContext` to match the model.
4. Run migrations:
   - `dotnet ef migrations add <Name> --project Vjezba.Model --startup-project Vjezba.Model`
   - `dotnet ef database update --project Vjezba.Model --startup-project Vjezba.Model`
5. Build the project and report any errors or warnings.
