# PRD-07: Testing, Parity, Rollout, and Governance

## 1. Objective

Define how the ECS migration is validated, rolled out safely, and governed to guarantee feature parity and controlled risk.

## 2. Scope

In scope:

- Parity definition and measurable gates
- Automated and manual validation strategy
- Rollout strategy and runtime toggles
- Program governance and decision checkpoints

Out of scope:

- Detailed implementation of each subsystem (covered by prior PRDs)

## 3. Parity Definition

Parity means:

- Same gameplay outcomes for equivalent input/packet sequences.
- Same user-visible results for critical UI and rendering flows.
- Same or better stability and performance profile.

Allowed differences:

- Internal architecture and object model
- Logged diagnostics and instrumentation details

## 4. Validation Strategy

Four validation layers:

1. Unit tests
- Component/system-level deterministic tests

2. Integration tests
- Packet decode -> command -> ECS state transition tests

3. Replay parity tests
- Re-run captured packet sessions and compare snapshots

4. Manual scenario certification
- High-risk gameplay and UI flows checklist

## 5. Replay Harness Requirements

Build deterministic replay tooling:

- Input: packet stream capture (with timing metadata)
- Execution: run legacy and ECS modes against same stream
- Output: snapshot diff at checkpoints

Snapshot content:

- Entity counts by type
- Key component/state fields
- Relationship integrity (container/equipment/party links)
- Selected UI-facing state projections

Diff policy:

- Critical mismatch: blocks release gate
- Noncritical mismatch: triaged with explicit waiver

## 6. Performance and Stability Gates

Performance gates:

- FPS and 95th percentile frame time no worse than baseline threshold
- Update-phase budget tracked by subsystem

Stability gates:

- No increase in crash frequency
- No sustained memory leak in 2+ hour soak runs

## 7. Rollout Strategy

Runtime flags:

- `UseEcsRuntime` (global switch)
- Subsystem flags (network, movement, inventory, render extract, UI bridge)

Rollout sequence:

1. Internal dev builds with subsystem flags
2. ECS default for CI replay suites
3. Extended soak and multiplayer sessions
4. ECS default in regular builds with fallback flag retained
5. Remove fallback after release confidence window

## 8. Governance

Cadence:

- Weekly migration review with parity dashboard

Required artifacts per subsystem cutover:

- Parity report
- Perf report
- Risk log update
- Rollback plan

Decision gates:

- Gate A: foundation ready (PRD-01/02)
- Gate B: network and core gameplay parity
- Gate C: render/UI/plugin parity
- Gate D: decommission legacy authority

## 9. Risk Register

R1: gameplay regression hidden in rare packet/order paths  
Mitigation: expanded replay corpus + random fuzz packet sequencing around known boundaries

R2: plugin ecosystem breakage  
Mitigation: compatibility matrix, plugin smoke suite, staged communication

R3: schedule overrun from subsystem coupling  
Mitigation: strict vertical slices and hard cutover criteria before starting new slices

R4: debug complexity during dual-runtime period  
Mitigation: unified tracing IDs and side-by-side telemetry views

## 10. Deliverables

- Automated parity test suite in CI
- Replay capture and comparison tooling
- Manual certification checklist with sign-off records
- Rollout and rollback runbooks

## 11. Exit Criteria

Program is complete when:

- ECS is default and legacy authority paths are removed.
- All parity, performance, and stability gates are met.
- Plugin compatibility commitments for current API surface are satisfied.

