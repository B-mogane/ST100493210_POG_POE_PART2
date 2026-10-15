# RaceDay Part 2 — ASP.NET Core REST API starter

This starter is structured around the supplied RaceDay SQL schema and endpoint plan. It is a foundation for completing, running, testing, and committing the assessed project. Review every file, run it locally, and  meaningful GitHub commits.

## Requirements
- .NET 8 SDK 
- SQL Server / SQL Server Express / LocalDB
- Visual Studio 2022 with ASP.NET and web development workload

## Important schema note
The supplied SQL script is the reference schema. The EF Core models below use its six tables: User, Event, Category, Enrolment, Result, Payment. Your assignment also says each event must have an event type. Distance is currently stored per Category as DistanceKm.
