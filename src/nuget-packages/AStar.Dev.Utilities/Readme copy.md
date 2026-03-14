# AStar.Dev.Utilities

## Introduction

AStar.Dev.Utilities is a compact collection of general-purpose helpers and extension methods used across AStar Dev projects. The goal is to standardize common utility operations and keep repeated logic out of application code.

## Purpose and Scope

This package provides reusable utilities that simplify everyday tasks such as string checks, JSON serialization helpers, lightweight LINQ helpers, and small quality-of-life extensions. It is intentionally broad but conservative, favoring small helpers over large abstractions.

## Target Audience

- Internal developers building AStar Dev applications and services
- External developers integrating AStar Dev packages
- Contributors extending or maintaining the utilities

## Key Features

- String helpers for null/whitespace checks, truncation, and JSON deserialization
- Regex helpers for common password or input checks
- LINQ helpers such as `ForEach` for concise iteration
- JSON serialization helpers for objects
- More exist, please review the code for additional extensions

## Examples and Code Snippets

```csharp
using AStar.Dev.Utilities;

var isOk = "Hello".IsNotNullOrWhiteSpace();
var truncated = "A long message".TruncateIfRequired(5);
```

```csharp
using AStar.Dev.Utilities;

var hasDigit = "Pa55word".ContainsAtLeastOneDigit();
```

## Conclusion

Use AStar.Dev.Utilities to keep common helper logic consistent and easy to reuse across projects.
