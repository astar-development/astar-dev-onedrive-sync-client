# AutoRegisterOptions Readonly Record Struct Rule

## Context

The AutoRegisterOptions analyzer enforces that types annotated with
`[AutoRegisterOptions]` are compatible with the source generator. Record structs
are often used for options models, but non-readonly record structs introduce
defensive copies and can violate expected immutability. We need a diagnostic to
ensure record structs used with AutoRegisterOptions are declared `readonly`.

## Goals

- Emit a clear analyzer error when a record struct annotated with
  `[AutoRegisterOptions]` is not `readonly`.
- Keep existing partial enforcement unchanged.
- Provide a diagnostic ID for tracking and tooling integration.

## Non-Goals

- Enforce `readonly` for classes or non-record structs.
- Change generator behavior or runtime behavior.

## Approach

- Extend `AutoRegisterOptionsPartialAnalyzer` to recognize record struct
  declarations (`record struct`) with the attribute.
- Add a new diagnostic (ASTAROPT003) that triggers when the record struct is
  not marked `readonly`.
- Report the diagnostic at the type identifier for consistency.

## Diagnostics

| ID | Title | Severity | Condition |
| --- | --- | --- | --- |
| ASTAROPT003 | Options record struct must be readonly | Error | Record struct with `[AutoRegisterOptions]` is not `readonly` |

## Test Plan

- Analyzer test that verifies ASTAROPT003 is reported for a non-readonly record
  struct with `[AutoRegisterOptions]`.
- Analyzer test that verifies no ASTAROPT003 is reported for a readonly record
  struct with `[AutoRegisterOptions]`.

## Rollout

- Update analyzer release notes with ASTAROPT003.
- Add analyzer test project to the solution.

<!-- © Capgemini 2026 -->