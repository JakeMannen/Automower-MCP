# automower-mcp

A [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server for the [Husqvarna Automower® Connect API](https://developer.husqvarnagroup.cloud/apis/automower-connect-api). Exposes every REST endpoint as an MCP tool so any MCP-compatible AI assistant (Claude Desktop, VS Code Copilot, etc.) can read mower status and control your mower.

Built with .NET 10 and the [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk). Supports two transports:
- **stdio** (default) — host spawns the process directly, no port binding required
- **HTTP** — Streamable HTTP on port 8080, set `MCP_TRANSPORT=http`

---

## Prerequisites

| Requirement | Notes |
|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | For building locally |
| Docker | For container deployment |
| Husqvarna developer account | [developer.husqvarnagroup.cloud](https://developer.husqvarnagroup.cloud) |

### Credentials

Create an application that uses both `Authentication API & Automower Connect API` at [developer.husqvarnagroup.cloud](https://developer.husqvarnagroup.cloud)

You need two values at runtime — both are found in your application in the developer portal:

| Developer portal   | Is Environment Variable        | Description |
|--------------------|--------------------------------|---|
| `Application key`   | `HUSQVARNA_API_KEY`            | Used as `X-Api-Key` header and as `client_id` for token requests |
| `Application secret` | `HUSQVARNA_APPLICATION_SECRET` | Used as `client_secret` to obtain OAuth2 tokens |

The server automatically acquires an access token on first use via the `client_credentials` grant and caches it, refreshing transparently before the 1-hour expiry. You never need to manage tokens manually.

---

## Running with Docker (recommended)

```bash
docker build -t automower-mcp .
```

**stdio transport (default)**

```bash
docker run -i -d \
  -e HUSQVARNA_API_KEY=your_application_key \
  -e HUSQVARNA_APPLICATION_SECRET=your_application_secret \
  automower-mcp
```

The `-i` flag keeps stdin open for the stdio transport.

**HTTP transport**

```bash
docker run -d \
  -e HUSQVARNA_API_KEY=your_application_key \
  -e HUSQVARNA_APPLICATION_SECRET=your_application_secret \
  -e MCP_TRANSPORT=http \
  -p 8080:8080 \
  automower-mcp
```

The server listens on `http://localhost:8080`.

---

## Running locally

```bash
cd src/AutomowerMcp
export HUSQVARNA_API_KEY=your_application_key
export HUSQVARNA_APPLICATION_SECRET=your_application_secret
```

**stdio transport (default)**
```bash
dotnet run
```

**HTTP transport**
```bash
MCP_TRANSPORT=http dotnet run
```

The server listens on `http://localhost:5000` (or whatever `ASPNETCORE_URLS` is set to).

---

## Connecting to an MCP host

### Claude Desktop

Add to `claude_desktop_config.json`. Choose one approach:

**Using Docker**

```json
{
  "mcpServers": {
    "automower": {
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "-e", "HUSQVARNA_API_KEY=YOUR_HUSQVARNA_API_KEY",
        "-e", "HUSQVARNA_APPLICATION_SECRET=YOUR_HUSQVARNA_APPLICATION_SECRET",
        "automower-mcp"
      ]
    }
  }
}
```

**Using `dotnet run` (requires .NET 10 SDK and cloned repo)**

```json
{
  "mcpServers": {
    "automower": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/automower-mcp/src/AutomowerMcp", "--no-build"],
      "env": {
        "HUSQVARNA_API_KEY": "YOUR_HUSQVARNA_API_KEY",
        "HUSQVARNA_APPLICATION_SECRET": "YOUR_HUSQVARNA_APPLICATION_SECRET"
      }
    }
  }
}
```

**Using a published binary (requires .NET 10 runtime)**

First publish once:
```bash
dotnet publish src/AutomowerMcp -c Release -o ~/automower-mcp-bin
```

Then add to config:
```json
{
  "mcpServers": {
    "automower": {
      "command": "dotnet",
      "args": ["/Users/you/automower-mcp-bin/AutomowerMcp.dll"],
      "env": {
        "HUSQVARNA_API_KEY": "YOUR_HUSQVARNA_API_KEY",
        "HUSQVARNA_APPLICATION_SECRET": "YOUR_HUSQVARNA_APPLICATION_SECRET"
      }
    }
  }
}
```

### VS Code (Copilot Agent)

Add to `.vscode/mcp.json` in your workspace.

**stdio — Docker**

```json
{
  "servers": {
    "automower": {
      "type": "stdio",
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "-e", "HUSQVARNA_API_KEY=YOUR_HUSQVARNA_API_KEY",
        "-e", "HUSQVARNA_APPLICATION_SECRET=YOUR_HUSQVARNA_APPLICATION_SECRET",
        "automower-mcp"
      ]
    }
  }
}
```

**stdio — `dotnet run` (requires .NET 10 SDK and cloned repo)**

```json
{
  "servers": {
    "automower": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/automower-mcp/src/AutomowerMcp", "--no-build"],
      "env": {
        "HUSQVARNA_API_KEY": "YOUR_HUSQVARNA_API_KEY",
        "HUSQVARNA_APPLICATION_SECRET": "YOUR_HUSQVARNA_APPLICATION_SECRET"
      }
    }
  }
}
```

**stdio — published binary can be found in GitHub Releases (requires .NET 10 runtime installed)**

```json
{
  "servers": {
    "automower": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["/path/to/automower-mcp-bin/AutomowerMcp.dll"],
      "env": {
        "HUSQVARNA_API_KEY": "YOUR_HUSQVARNA_API_KEY",
        "HUSQVARNA_APPLICATION_SECRET": "YOUR_HUSQVARNA_APPLICATION_SECRET"
      }
    }
  }
}
```

**HTTP transport** (requires the server to be running separately on port 8080)

```json
{
  "servers": {
    "automower": {
      "type": "http",
      "url": "http://localhost:8080"
    }
  }
}
```

---

## Available tools

### Query tools (read)

All GET responses are automatically enriched with human-readable description fields alongside opaque values: `errorCodeDescription`, `modeDescription`, `activityDescription`, and `stateDescription`.

| Tool | API endpoint | Description |
|---|---|---|
| `ListMowers` | `GET /mowers` | List all mowers. Includes enriched status descriptions inline. |
| `GetMower` | `GET /mowers/{id}` | Full details for a single mower, including enriched status descriptions. |
| `GetMowerMessages` | `GET /mowers/{id}/messages` | Last 50 messages/events. Each entry includes `errorCodeDescription`. |
| `GetStayOutZones` | `GET /mowers/{id}/stayOutZones` | All stay-out zones with enabled status |
| `GetWorkAreas` | `GET /mowers/{id}/workAreas` | Summary list of all work areas |
| `GetWorkArea` | `GET /mowers/{id}/workAreas/{workAreaId}` | Detailed data for one work area including its calendar |

### Action tools (write)

| Tool | API endpoint | Description |
|---|---|---|
| `StartMowing` | `POST /mowers/{id}/actions` | Mow for a fixed number of minutes |
| `StartMowingInWorkArea` | `POST /mowers/{id}/actions` | Mow a specific work area, optionally with a duration |
| `PauseMower` | `POST /mowers/{id}/actions` | Pause the mower in place |
| `ResumeSchedule` | `POST /mowers/{id}/actions` | Remove any override; resume the calendar schedule |
| `ParkMower` | `POST /mowers/{id}/actions` | Park for a fixed duration, with optional external reason code |
| `ParkUntilNextSchedule` | `POST /mowers/{id}/actions` | Park until the next scheduled mowing window |
| `ParkUntilFurtherNotice` | `POST /mowers/{id}/actions` | Park indefinitely (mode becomes HOME) |
| `UpdateCalendar` | `POST /mowers/{id}/calendar` | Replace the entire weekly mowing schedule |
| `ConfirmError` | `POST /mowers/{id}/errors/confirm` | Dismiss a confirmable non-fatal error |
| `UpdateSettings` | `POST /mowers/{id}/settings` | Update cutting height, headlight mode, and/or clock |
| `ResetCuttingBladeUsageTime` | `POST /mowers/{id}/statistics/resetCuttingBladeUsageTime` | Reset blade usage counter after a blade change |
| `UpdateStayOutZone` | `PATCH /mowers/{id}/stayOutZones/{stayOutId}` | Enable or disable a stay-out zone |
| `UpdateWorkArea` | `PATCH /mowers/{id}/workAreas/{workAreaId}` | Update work area settings (cutting height, name, pattern) |
| `UpdateWorkAreaCalendar` | `POST /mowers/{id}/workAreas/{workAreaId}/calendar` | Replace the schedule for a specific work area |

### Status tool (local, no API call)

| Tool | Description |
|---|---|
| `GetStatusDescriptions` | Returns all possible values for `mode`, `activity`, and `state` with descriptions. These are already inlined into every GET response — use this only if you encounter an unfamiliar value. |

---
## Agent files

The ```agent-files``` folder contains files usable together with your agent of choice. Skills etc.