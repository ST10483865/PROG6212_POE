## PART 2 
# PROG6212_POE
RaceDay is a stack API-driven event management system built for the South African road running, walking and cycling community. Many local road events are still managed through paper-based registration, spreadsheets and disconnected communication channels. RaceDay solves this problem by giving Event Organisers a platform to create and manage events, categories and participant results. RaceDay also gives Participants a way to browse events enter races track their personal performance history and prepare for race day using live weather and route information.

The system is built around a database with eight core entities. User, Organiser, Participant, Event, Route, Category, Registration and Results. These entities are connected through an API that covers authentication, user profiles, event and category management event enrolments and results capture.

. Part 1: planning (ERD, API endpoint plan, SQL schema) in /docs.
.Part 2: the RESTful API (Part2/RaceDay.Api) and its unit tests (Part2/RaceDay.Tests).

## User Roles

**Organiser** is a user who creates and manages events. Organiser can create, update and delete events and their categories. Organiser can view registrations for Organisers events. Organiser can capture race results once an event has taken place.

**Participant** is a user who takes part in events. Participant can. Search for upcoming events. Participant can register for a category within an event. Participant can view Participants registration and results history. Participant can access race-day information such, as route details and weather conditions.


## Tech Stack

ASP.NET Core Web API (C#, .NET 8), Entity Framework Core (Code-First) on SQL Server, cookie-based session authentication, hashed passwords (PasswordHasher), Swagger (Swashbuckle), xUnit, GitHub Actions.

Setup and Run Instructions

Prerequisites: Visual Studio 2022 (ASP.NET workload), .NET 8 SDK, SQL Server or SQL Server LocalDB (installed with Visual Studio).

Clone the repository and open Part2/RaceDay.sln in Visual Studio.
Check the connection string in Part2/RaceDay.Api/appsettings.json. The default uses LocalDB and a database called Raceday_System. Change the server name if you use a full SQL Server instance.
Create the database:
Visual Studio: Tools > NuGet Package Manager > Package Manager Console, set the default project to RaceDay.Api, then run Update-Database.
Command line: from Part2/RaceDay.Api run dotnet ef database update. The API also applies pending migrations automatically on startup.
Set RaceDay.Api as the startup project and press F5. Swagger UI opens at /swagger.
In Swagger: call POST /api/auth/register, then POST /api/auth/login. Swagger keeps the session cookie, so protected endpoints work straight away. Register one Organiser and one Participant to test both roles.

Run the unit tests: Test Explorer > Run All, or from the Part2 folder run dotnet test. The tests use an in-memory database, so no SQL Server is needed.

Authentication and Role-Based Access

POST /api/auth/login verifies the password against its stored hash and signs the user in with a session cookie containing their ID, role and profile ID. Controllers use [Authorize(Roles = "...")] for role checks, and ownership checks make sure an Organiser can only change their own events, categories and results, and a Participant can only see their own registrations and results.

## Unit Tests

Part2/RaceDay.Tests runs the real API in memory with the real login flow and covers success and failure scenarios for: registration and login, event management, role rejection in both directions, and event enrolments (including full categories and duplicate entries) plus result capture.

## Differences from the Part 1 Plan

Explained in full at the top of docs/api_endpoint_plan.md. In short: the Admin role was removed (the brief only has two roles), events gained name, description and eventType, a GET /api/events/{eventId}/registrations endpoint was added so Organisers can see enrolments, blank responses in the plan were filled in, and the database gained User.Password_Hash and the new Event columns. Registration -> Participant uses NO ACTION because SQL Server rejects two cascade paths. The ERD and docs/raceday_schema.sql are updated to match. In C#, the Route entity class is named RaceRoute (mapped to the Route table) to avoid clashing with an ASP.NET type.

## Repo structure
/docs
_/Section A(erd diagram)
-/Section B markdown file
_/Section C sql script

## References
_SQL Server Management Studio 2022
_draw.io
-WORD DOC

 ##Youtube Link
 https://youtu.be/S0dWDHe3k0Q

 ## SCREENSHOOT OF CI/CD WORKFLOW
 <img width="1920" height="1020" alt="image" src="https://github.com/user-attachments/assets/6226eedf-9884-4a3e-bcac-5ee7561bab6c" />

 ## THE PROJECT HAS  TWO BRANCHES
 ## _MAIN Contains Part 1
 ## -Master Contains Part2
 

 


