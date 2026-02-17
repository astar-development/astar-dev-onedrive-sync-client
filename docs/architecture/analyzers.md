# Source Analyzers

This repository ships Roslyn analyzers in `AStar.Dev.Source.Analyzers` to
enforce source-generator compatibility rules at compile time.

## AutoRegisterOptions Analyzer

`AutoRegisterOptionsPartialAnalyzer` validates types annotated with
`[AutoRegisterOptions]`.

Rules:

- ASTAROPT002: Requires annotated types to be `partial`.
- ASTAROPT003: Requires annotated record structs to be `readonly`.

Diagnostics are reported at the type identifier to align with standard C#
diagnostic UX.

<!-- © Capgemini 2026 -->