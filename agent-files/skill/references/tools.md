# Automower MCP Tools Reference

This document provides a summary of all MCP tools available for controlling and monitoring Husqvarna Automower robotic mowers.

## Query Tools (Read-only)

These tools retrieve the current status, history, and configuration of mowers. All status responses include human-readable descriptions for codes (e.g., `activityDescription`).

| Tool | Purpose | Key Parameters |
|---|---|---|
| `ListMowers` | List all mowers linked to the user account. | None |
| `GetMower` | Get full details (battery, GPS, settings, status) for one mower. | `mowerId` (UUID) |
| `GetMowerMessages` | Get the last 50 events/errors with severity and timestamps. | `mowerId` (UUID) |
| `GetStayOutZones` | List all configured stay-out zones and their enabled status. | `mowerId` (UUID) |
| `GetWorkAreas` | List all work areas (zones) defined for the mower. | `mowerId` (UUID) |
| `GetWorkArea` | Get details and calendar schedule for a specific work area. | `mowerId`, `workAreaId` |
| `GetStatusDescriptions`| Get all possible values for mode, activity, and state. | None |

## Action Tools (State-changing)

These tools send commands to the mower or update its configuration.

### Mowing & Parking

| Tool | Purpose | Key Parameters |
|---|---|---|
| `StartMowing` | Mow for a fixed duration (switches to `SECONDARY_AREA`). | `mowerId`, `durationMinutes` |
| `StartMowingInWorkArea` | Mow a specific work area (indefinitely or for duration). | `mowerId`, `workAreaId`, `durationMinutes` |
| `PauseMower` | Stop the mower in its current position. | `mowerId` |
| `ResumeSchedule` | Clear overrides and follow the weekly calendar. | `mowerId` |
| `ParkMower` | Park for a fixed duration, then resume schedule. | `mowerId`, `durationMinutes`, `externalReason` |
| `ParkUntilNextSchedule`| Park until the next calendar task begins. | `mowerId` |
| `ParkUntilFurtherNotice`| Park indefinitely (switches mode to `HOME`). | `mowerId` |

### Configuration & Maintenance

| Tool | Purpose | Key Parameters |
|---|---|---|
| `UpdateCalendar` | Replace the entire weekly schedule (requires full JSON). | `mowerId`, `tasksJson` |
| `UpdateWorkAreaCalendar`| Replace the schedule for one specific work area. | `mowerId`, `workAreaId`, `tasksJson` |
| `UpdateSettings` | Change cutting height, headlights, or sync clock. | `mowerId`, `cuttingHeight`, `headlightMode`, `dateTime`, `timeZone` |
| `UpdateWorkArea` | Change work area name, height, or mowing pattern. | `mowerId`, `workAreaId`, `cuttingHeight`, `enable`, `name`, `orientation`, `orientationShift` |
| `UpdateStayOutZone` | Enable or disable a stay-out zone. | `mowerId`, `stayOutZoneId`, `enable` |
| `ConfirmError` | Dismiss a confirmable non-fatal error. | `mowerId` |
| `ResetCuttingBladeUsageTime` | Reset blade timer after a replacement. | `mowerId` |
