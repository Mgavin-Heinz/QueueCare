# QueueCare

**A web-based appointment and virtual queue management system for public clinics.**
SE2 Capstone Project · Occupational Certificate: Software Engineer · Supervisor: Mr. Lusukama Selemani

## Problem
Patients at South African public clinics often arrive very early and wait for hours without knowing their place in the queue. Staff manage queues on paper and cannot report on waiting times.

## Solution
QueueCare lets patients book appointments and track their position in a live queue. Reception and nursing staff run the queue from a dashboard, and admins get reports on patients served, average wait and no-show rate.

## Team
| Member | Role |
| --- | --- |
| Heinz Pretorius | Project lead / Scrum lead + backend |
| Christopher Johnson | Database and backend developer |
| Victoria Julius | Testing / QA + documentation |
| Tirick Stadhouer | Frontend / UI developer |

## Technology stack
- C# · ASP.NET Core MVC (Razor views)
- SQL Server · Entity Framework Core
- ASP.NET Core Identity (roles: Patient, Receptionist, Nurse, Admin)
- Bootstrap · xUnit · SignalR (nice-to-have)
- Visual Studio 2022 · SQL Server Management Studio · GitHub

## Features (must-have)
- Patient registration and login
- Book and cancel appointments by service type
- Walk-in check-in at reception
- Live queue page with position and estimated wait
- Reception dashboard: call next, served, no-show, skip
- Nurse view of who is next
- Admin: services, staff accounts, daily capacity
- Reports: served per day, average wait, no-show rate

## Setup
1. In Visual Studio 2022: **Clone a repository** → `https://github.com/Mgavin-Heinz/QueueCare`
2. Switch to the `dev` branch (Git → Manage Branches), then create your own feature branch from it.
3. Open `QueueCare/QueueCare.sln`.
4. Tools → NuGet Package Manager → Package Manager Console, run `Update-Database`.
5. Press F5. Register a test account to check login works.

The app uses SQL Server LocalDB, which comes with Visual Studio's "ASP.NET and web development" workload. No extra database setup is needed.

## Demo accounts
To be added once seed data is in place (test data only, no real patient information).

## How we work
See [CONTRIBUTING.md](CONTRIBUTING.md) for branches, commit messages and pull requests.
