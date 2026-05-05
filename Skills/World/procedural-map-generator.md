# ProceduralMapGenerator

**Script:** `Assets/Scripts/ProceduralMapGenerator.cs`
**Status:** Phase 1 — Active
**Category:** World

## Purpose

Generates a simple test environment at runtime. Creates floor planes, walls, and obstacles using primitive GameObjects (no imported assets). Used for Phase 1-2 testing before Phase 3's formal map.

## Dependencies

- None (self-contained, runs at scene start)

## Public API

| Member | Type | Description |
|--------|------|-------------|
| `mapWidth` | float | Map width in meters (default 50) |
| `mapLength` | float | Map length in meters (default 50) |
| `wallHeight` | float | Wall height (default 4) |
| `obstacleCount` | int | Number of random obstacles (default 20) |
| `seed` | int | Random seed for reproducibility |
| `Generate()` | void | Generate the map |

## Generated Elements

### Required
- **Ground plane** — large Plane or scaled Cube at y=0
- **Boundary walls** — 4 walls forming the arena perimeter
- **Scattered cover** — random cubes/cylinders as hiding spots (pillars, crates)

### Optional (stretch)
- **Second floor** — elevated platform with ramp access
- **Corridors** — narrow wall segments creating lanes
- **Safe zone** — marked spawn area (Seeker can't enter during Hiding phase)

## Spawn Points

```
Generated map includes:
- 1 Seeker spawn (marked, at edge of map)
- 6 Hider spawns (distributed, not overlapping)
- All spawns use empty GameObjects with SpawnPoint tag
```

## Notes

- All objects are primitive Unity shapes (Cube, Cylinder, Plane) — zero asset imports.
- Colliders are auto-added for physics/raycast support.
- Map is static after generation (no runtime modification).
- Use `seed` to reproduce the same layout across clients for testing.
- In production (Phase 3+), replace with a formal Unity scene/map.
