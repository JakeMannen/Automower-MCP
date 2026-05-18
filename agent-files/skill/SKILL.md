---
name: automower-mcp
description: Control Husqvarna Automower robotic mowers via the Automower MCP API. Use for status checks, starting/parking mowers, managing schedules, and troubleshooting errors.
---

# Automower MCP Server - Agent Instructions

## Purpose

Enable AI assistants to fully monitor and control Husqvarna Automower® robotic mowers through the Automower Connect API. The agent should use this skill when users ask about mower status, want to start/stop mowing, check errors, or manage work areas.

**Key constraints:**
- Always consult user before performing any action that changes mower state
- Always verify mower state before destructive operations
- Never clear errors without confirming they are understood and resolved by user
- Never override schedule without checking current status first
- Report battery levels prominently in all status reports
- Decode error codes using `getStatusDescriptions` when unfamiliar

## Default Tools & Workflow

Use these tools in the specified order for common tasks:

### 1. Status Queries (Information Gathering)

**Primary:** `listMowers`
- Use first to discover available mowers and their UUIDs
- Returns all mowers with basic status
- Required before any other operation

**Secondary:** `getMower`
- Use when `mowerId` is known from user input
- Returns full details including battery, activity, mode, statistics
- Required before any control operation to verify state

**Supporting:** `getMowerMessages`, `getStatusDescriptions`, `getWorkAreas`
- Use `getMowerMessages` to diagnose errors or recent events
- Use `getStatusDescriptions` for any unfamiliar status values
- Use `getWorkAreas` to see available zones and their calendars

### 2. Control Operations (Changes)

**Preferred order:**
1. Check current state with `getMower`
2. Use `startMowing` for timed sessions (1-65000 minutes)
3. Use `startMowingInWorkArea` for zone-specific mowing
4. Use `pauseMower` for immediate stop (temporary)
5. Use `resumeSchedule` to return to calendar mode
6. Use `parkMower` with duration for temporary parking
7. Use `parkUntilNextSchedule` to wait for next calendar task
8. Use `parkUntilFurtherNotice` for full manual override

**Validation before action:**
- Verify `activity` is not `MOWING` for start operations
- Check `batteryPercentage` is acceptable (>20% recommended)
- Confirm no `ERROR` or `FATAL` status codes in messages

## Procedural Patterns

### Status Monitoring

**User:** "Show me the status of my mowers"
**Action:**
1. Call `listMowers` to get all mower IDs
2. For each mower, call `getMower`
3. Extract and report:
   - `activity`: What it's currently doing (MOWING, CHARGING, etc.)
   - `batteryPercentage`: Current battery level
   - `mode`: Current operational mode (MAIN_AREA, HOME, etc.)
   - `errorCode` if non-zero (include description)
4. Format output as a readable table or list

**Output template:**
```
┌─────────────────────────────────────────────────────┐
│ 🤖 AUTOMOWER STATUS REPORT                          │
├─────────────────────────────────────────────────────┤
│ Mower: {mower_id}                                   │
│ ├ Battery: {battery}%                               │
│ ├ Activity: {activity}                              │
│ ├ Mode: {mode}                                      │
│ └ Status: {operational or error status}             │
└─────────────────────────────────────────────────────┘
```

### Starting a Mowing Session

**User:** "Start mower #1 for 30 minutes"
**Action:**
1. First call `listMowers` or use known UUID
2. Call `getMower` to verify current state
3. Check `errorCode` is zero
4. Call `startMowing` with `mowerId` and `durationMinutes=30`
5. Optionally monitor with `getMower` to confirm operation

**User:** "Mow work area #1 for 60 minutes"
**Action:**
1. Call `listMowers` to get mower UUID
2. Call `getWorkAreas` with `mowerId` to get available zones
3. Call `startMowingInWorkArea` with `workAreaId=1` and `durationMinutes=60`

### Stay-Out Zones

**User:** "Show me the stay-out zones on mower #1"
**Action:** Call `getStayOutZones` with `mowerId`

**User:** "Disable the pet zone"
**Action:**
1. Call `getStayOutZones` to get the zone UUID
2. Call `updateStayOutZone` with `enable=false`

### Work Area Details

**User:** "What's the schedule for work area #2?"
**Action:** Call `getWorkArea` with `mowerId` and `workAreaId=2` to get full schedule details

### Parking Operations

**User:** "Pause the mower" (temporary stop)
**Action:** Call `pauseMower`

**User:** "Park the mower for 2 hours"
**Action:** Call `parkMower` with `parkingDurationMinutes=120`

**User:** "Park until next scheduled task"
**Action:** Call `parkUntilNextSchedule`

**User:** "Park the mower indefinitely"
**Action:** Call `parkUntilFurtherNotice` (sets mode to HOME)

### Error Confirmation

**User:** "Mower error was fixed, clear it"
**Action:** Call `confirmError` with `mowerId` and `errorCode`

### Cutting Height Adjustment

Area-specific cutting height is set via `updateWorkArea` with `cuttingHeight` parameter in percentage. The actual cutting height in mm and percentage of max/min height can be derived from the following table:

**Conversion Table for Step, Cutting Height & Percentage for Automower Nera 4xx series**
| Step | Cutting Height | Percentage |
|-----|----------------|------------|
| 8	| 55 mm | 100% |
| 7	| 50 mm | 85,7% |
| 6	| 45 mm | 71,4% |
| 5	| 40 mm | 57,1% |
| 4	| 35 mm | 42,9% |
| 3	| 30 mm | 28,6% |
| 2	| 25 mm | 14,3% |
| 1	| 20 mm | 0% |

**Conversion Table for Step, Cutting Height & Percentage (Generic Automower models)**
| Step | Cutting Height | Percentage |
|-----|----------------|------------|
| 9	| 60 mm | 100% |
| 8	| 55 mm | 82% |
| 7	| 50 mm | 75% |
| 6	| 45 mm | 62% |
| 5	| 40 mm | 50% |
| 4	| 35 mm | 42% |
| 3	| 30 mm | 25% |
| 2	| 25 mm | 12% |
| 1	| 20 mm | 0% |


**User:** "Set cutting height to 4"
**Action:** Call `updateSettings` with `cuttingHeight=4`

**User:** "Set cutting height to 40 mm in Main Area"
**Action:** Call `updateWorkArea` with `workAreaId` for Main Area and `cuttingHeight=50` (since 40 mm corresponds to 50% for generic model)

### Settings Updates

**User:** "Turn on headlights"
**Action:** Call `updateSettings` with `headlights=true`

**User:** "Set the clock to 18:30"
**Action:** Call `updateSettings` with `localClockTime="18:30"`

### Work Area Configuration

**User:** "Rename work area #1 to 'Garden'"
**Action:** Call `updateWorkArea` with `workAreaId=1` and `name='Garden'`

**User:** "Disable work area #2"
**Action:** Call `updateWorkArea` with `workAreaId=2` and `enable=false`

**User:** "Set work area #1 to cut at height 3"
**Action:** Call `updateWorkArea` with `workAreaId=1` and `cuttingHeight=3`

### Blade Maintenance

**User:** "I just replaced the blade, reset the counter"
**Action:** Call `resetCuttingBladeUsageTime` with `mowerId`

### Troubleshooting Errors

**User:** "Mower #1 shows an error"
**Action:**
1. Call `getMower` with `mowerId`
2. Extract `errorCode` and check `status`
3. Call `getMowerMessages` with `mowerId`
4. Use `getStatusDescriptions` to decode error meanings
5. Provide context-aware recommendations:
   - Battery errors: Check charging station connection
   - Cut height errors: Verify grass blade height
   - Outside working area: Check boundary wires
   - Lift errors: Check if mower is lifted

## Error Handling & Gotchas

### Critical Gotchas

1. **Mower UUIDs are stable** - Store and reuse UUIDs; don't re-query unnecessarily
2. **Schedule conflicts** - `startMowing` only works when mower is NOT in a scheduled mowing period
3. **Battery thresholds** - Don't schedule mowing if battery <20%
4. **Error confirmations** - Some errors require `confirmError` before action can resume
5. **Work area limitations** - Not all mowers have work areas enabled; check with `getWorkAreas`
6. **Charging station detection** - `chargingStation` property indicates proximity; false positives possible

### Validation Checklist (before control operations)

- [ ] Current state retrieved via `getMower`
- [ ] No active errors (`errorCode` = 0)
- [ ] Battery level acceptable for operation
- [ ] Mower not already in active mowing session
- [ ] Correct UUID being operated on
- [ ] User confirmed the action

### Plan-Validate-Execute Pattern

**For batch operations:**
1. **Plan:** Create JSON with intended actions for each mower
2. **Validate:** Check all mowers are online and ready
3. **Execute:** Apply actions one by one, monitoring results
4. **Report:** Summarize success/failure for each operation

## Output Formats

### Status Report

Use table format for multiple mowers:

```
┌──────────────┬─────────┬────────────┬────────┬────────────┬──────────────┐
│ Mower ID     │ Battery │ Activity   │ Mode   │ Status     │ Last Error   │
├──────────────┼─────────┼────────────┼────────┼────────────┼──────────────┤
│ automower-1  │ 85%     │ MOWING     │ HOME   │ OK         │ —            │
└──────────────┴─────────┴────────────┴────────┴────────────┴──────────────┘
```

### Error Diagnosis

When errors are present:

```
⚠️ ERROR DETECTED
   Mower: automower-1
   Error Code: {code}
   Description: {description}
   
   Recommended Actions:
   • {action1}
   • {action2}
   • {action3}
   
   To clear this error, confirm it by calling:
   → confirmError(mowerId=..., errorCode=...)
```

### Work Area Info

When listing work areas:

```
Work Areas for Mower {id}:
┌─────────────┬─────────────────────────────────┬─────────────────────┐
│ Work Area   │ Schedule                         │ Duration            │
├─────────────┼─────────────────────────────────┼─────────────────────┤
│ 1           │ {schedule}                      │ {duration} minutes  │
└─────────────┴─────────────────────────────────┴─────────────────────┘
```

## Tool Descriptions

| Tool | When to Use |
|------|--------------|
| `listMowers` | Always first to discover mowers |
| `getMower` | Before any control operation |
| `getMowerMessages` | When errors or events need diagnosis |
| `getWorkAreas` | To check available zones |
| `getStatusDescriptions` | For any unfamiliar status values |
| `startMowing` | For timed sessions (not in schedule) |
| `startMowingInWorkArea` | For zone-specific mowing |
| `pauseMower` | Temporary stop (quick return) |
| `resumeSchedule` | Return to calendar mode |
| `parkMower` | Temporary parking with duration |
| `parkUntilNextSchedule` | Wait for next scheduled task |
| `parkUntilFurtherNotice` | Manual override (full park) |
| `updateCalendar` | When weekly schedule changes are needed |
| `updateWorkAreaCalendar` | When single work area schedule needs updating |
| `confirmError` | When error has been resolved and needs dismissal |
| `updateSettings` | To change cutting height, headlights, or clock time |
| `updateStayOutZone` | To enable/disable a specific stay-out zone |
| `updateWorkArea` | To update work area name, height, or enable/disable |
| `resetCuttingBladeUsageTime` | To reset blade usage counter after blade replacement |
| `getStayOutZones` | To view configured stay-out zones before toggling |
| `getWorkArea` | When mowerId is known and detailed schedule needed |

## Advanced Patterns

### Daily Status Check (automation)

```
Every morning at 7 AM:
1. listMowers
2. For each mower: getMower
3. If battery < 50%: notify user
4. If error code present: diagnose and report
5. If in ERROR state: recommend confirmError
```

### Emergency Stop

```
User: "Stop all mowers immediately"
Action:
1. listMowers
2. For each mower: pauseMower
3. Verify with getMower that activity is PAUSED
```

### Battery Management

```
Monitor battery levels:
• >80%: Operating normally
• 50-80%: Continue normal operations
• 30-50%: Consider temporary parking to charge
• <20%: Recommend manual parking to charging station
• 0%: Critical - recommend immediate charging
```

## Examples

### Simple Start

```
User: "Can mower #1 go out and mow?"
→ listMowers (get UUIDs)
→ getMower(mowerId=...) (check state)
→ startMowing(mowerId=..., durationMinutes=120)
```

### Work Area Selection

```
User: "Mow the garden area for an hour"
→ listMowers
→ getWorkAreas(mowerId=...)
→ startMowingInWorkArea(mowerId=..., workAreaId=1, durationMinutes=60)
```

### Error Recovery

```
User: "Mower won't start, check errors"
→ getMower(mowerId=...) (identify errorCode)
→ getMowerMessages(mowerId=...) (get context)
→ confirmError(mowerId=...) (if confirmable)
→ startMowing(mowerId=...) (retry)
```

### Schedule Management

```
User: "What's tomorrow's mowing schedule?"
→ getMower(mowerId=...) (get calendar)
→ extract tomorrow's tasks
→ present schedule to user
```

## Notes

- Always use UUIDs from `listMowers` (never guess IDs)
- Battery levels should be prominently displayed
- Error codes need decoding via `getStatusDescriptions`
- Work areas may not exist on all mowers
- Park operations are destructive; validate before executing
- Messages provide context for diagnosis

## References
- [Automower Connect API Documentation](https://developer.husqvarnagroup.cloud/docs/automower-connect-api)
- Tools list: `./references/tools.md`

---