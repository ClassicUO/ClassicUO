# PRD-01: Flecs.NET Dependency and Build Integration

## 1. Objective

Integrate Flecs.NET as a source dependency (no NuGet) into ClassicUO, with deterministic local and CI builds across supported platforms.

## 2. Scope

In scope:

- Repository dependency strategy for `Flecs.NET`
- `csproj` references and build properties
- Native library handling for dev, build, test, publish
- CI pipeline updates for restore/build/test/publish

Out of scope:

- Gameplay refactor logic (covered in later PRDs)

## 3. Current State

- ClassicUO already uses source dependencies via submodules in [`external`](../../../external).
- Main runtime is net10 with AOT in [`src/ClassicUO.Client/ClassicUO.Client.csproj`](../../../src/ClassicUO.Client/ClassicUO.Client.csproj).
- Flecs.NET targets net10 and depends on source projects/bindings/native build chain.

## 4. Requirements

- No NuGet Flecs references in any ClassicUO project.
- `dotnet build` on solution works without manual copying of DLLs.
- `dotnet publish` includes required native assets for runtime RIDs.
- Debug builds can use Flecs debug checks where applicable.

## 5. Dependency Strategy

Primary strategy:

- Add `external/Flecs.NET` as a git submodule in `.gitmodules`.
- Reference [`external/Flecs.NET/src/Flecs.NET/Flecs.NET.csproj`](../../../external/Flecs.NET/src/Flecs.NET/Flecs.NET.csproj) from ECS host project(s).

Fallback local-dev strategy:

- Allow overriding Flecs root with an MSBuild property (`FlecsNetRoot`) pointing to `C:\dev\Flecs.NET`.
- Default remains repository-relative path to keep CI deterministic.

## 6. Project Layout Changes

Create new project:

- `src/ClassicUO.ECS/ClassicUO.ECS.csproj`

Responsibilities:

- Own all Flecs world setup/modules/systems/queries
- Reference `Flecs.NET.csproj`
- Be referenced by `ClassicUO.Client` (not vice versa)

Reason:

- Keeps ECS migration isolated and reversible.
- Avoids polluting existing projects during parallel migration.

## 7. Build Properties and Native Handling

Required properties:

- `AllowUnsafeBlocks=true` in ECS project (Flecs APIs use unsafe paths)
- Optional `FlecsStaticLink=true` when publish AOT path requires static linking

Native assets strategy:

- Development: rely on Flecs.NET project outputs in build graph
- Publish: verify RID-specific native assets copied to publish output
- Add explicit verification target (build step) that fails if expected `flecs` native artifact missing

## 8. CI/CD Plan

Pipeline additions:

1. Checkout with submodules (`--recursive`).
2. Restore/build solution including `ClassicUO.ECS`.
3. Run unit tests and new ECS parity tests.
4. Publish matrix for key RIDs:
- `win-x64`
- `linux-x64`
- `osx-arm64`

Validation step:

- Smoke run startup for each published artifact in CI where possible.

## 9. Migration Tasks

Task group A: Repo wiring

- Add submodule entry for `external/Flecs.NET`
- Add docs for developers to sync submodules

Task group B: Project wiring

- Add `ClassicUO.ECS` project
- Add project references
- Add build properties and conditional path override

Task group C: Native verification

- Add build/publish checks
- Add troubleshooting docs for missing native assets

## 10. Acceptance Criteria

- Clean clone + submodule init builds on supported dev OSes.
- `ClassicUO.Client` starts and creates Flecs world in a no-op bootstrap mode.
- CI build and publish jobs pass with Flecs dependency included.
- No NuGet Flecs references present in solution (`rg "Flecs.NET.*PackageReference"` returns none).

## 11. Risks and Mitigations

Risk: Flecs native artifact mismatch by RID
- Mitigation: explicit publish verification + smoke boot matrix

Risk: local absolute path leakage into committed project files
- Mitigation: only use property override, never hardcode absolute paths

Risk: build time increases
- Mitigation: isolate ECS project and tune incremental build caching

## 12. Exit Gate

PRD-01 is complete when dependency/build pipeline is stable and ECS host project can be consumed by `ClassicUO.Client` without enabling gameplay logic yet.

