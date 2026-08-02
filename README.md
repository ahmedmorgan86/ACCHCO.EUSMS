# ACCHCO EUSMS — Equipment User Support Management System

An integrated system for managing IT support tickets and equipment at the Alexandria Container Handling Company (ACCHCO). Built as a WPF desktop application with a shared SQLite database across the network.

## Key Features

- **Ticket Management**: Create, track, categorize, link to equipment, attach files, full history log.
- **Equipment Management**: Port equipment (RTG, Kalmar, Hyster, ReachStacker), network devices, access points, printers, gate OCR cameras.
- **Device Replacements**: Document replacement operations with a complete history (add / edit / delete).
- **User Management**: Users, roles, permissions, Active Directory synchronization.
- **Full ITSM**: Incidents, Changes, Problems, Known Errors, Service Requests, CMDB configuration items.
- **Dashboards**: General statistics, ITIL dashboards, and performance analytics.
- **Reports**: PDF, Excel, and CSV.
- **Real-time multi-machine sync**: All instances run against one shared SQLite database over an SMB share, with automatic refresh for everyone within ~1 second.

## Technology Stack

| Technology | Purpose |
|---|---|
| WPF (.NET 8) | Desktop UI |
| MVVM + CommunityToolkit.Mvvm | Design pattern, state & messaging |
| Entity Framework Core 8 | Data access (SQLite) |
| Microsoft.Data.Sqlite | Database connections |
| Serilog | Logging |
| SQLite | Database (single shared network file) |

## Project Structure

```
ACCHCO.EUSMS.sln
├── src/
│   ├── ACCHCO.EUSMS.App          — WPF application: views, ViewModels, helpers
│   ├── ACCHCO.EUSMS.Data         — Data layer: entities, repositories, DbContext
│   ├── ACCHCO.EUSMS.Services     — Business logic layer (ITSM, tickets, equipment, ...)
│   └── ACCHCO.EUSMS.Reports      — Report generators (PDF / Excel / CSV)
├── scripts/
│   └── InitializeDatabase.sql    — Database initialization script
├── publish.ps1                   — Publish script (single self-contained exe)
└── README.md
```

## Build & Run

```bash
# Build the solution
dotnet build ACCHCO.EUSMS.sln

# Publish a single self-contained executable (Win-x64)
.\publish.ps1
```

The publish output is placed in the `publish\` folder and includes:
- `ACCHCO.EUSMS.exe` — the application (self-contained, no .NET install required)
- `appsettings.json` — database settings
- `اقرأني.txt` — user guide (Arabic)

## Network Synchronization

- The database is a single shared SQLite file on an SMB network share.
- All machines open the same file and automatically detect changes every 0.5 seconds.
- `PRAGMA journal_mode=DELETE` is used (the safe mode for network shares), and sequential number generation is atomic to avoid duplicates.
