# AStar.Dev.Utilities

## Introduction

AStar.Dev.Utilities is a compact collection of general-purpose helpers and extension methods used across AStar Dev projects. The goal is to standardize common utility operations and keep repeated logic out of application code.

## Purpose and Scope

This package provides reusable utilities that simplify everyday tasks such as string checks, JSON serialization helpers, lightweight LINQ helpers, and small quality-of-life extensions. It is intentionally broad but conservative, favouring small helpers over large abstractions.

## Target Audience

- Internal developers building AStar Dev applications and services
- External developers integrating AStar Dev packages
- Contributors extending or maintaining the utilities

## Key Features


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

## Extension Methods by Class

### EncryptionExtensions
- `Encrypt(this string plainText, string? key = null, string? iv = null)`
- `Decrypt(this string plainText, string? key = null, string? iv = null)`

### ObjectExtensions
- `ToJson<T>(this T @object)`

### StringExtensions
- `IsNull(this string? value)`
- `IsNotNull(this string? value)`
- `IsNullOrWhiteSpace(this string? value)`
- `IsNotNullOrWhiteSpace(this string? value)`
- `IsImage(this string json)`
- `IsNumberOnly(this string json)`
- `TruncateIfRequired(this string target, int truncateLength)`
- `RemoveTrailing(this string json, string removeTrailing)`
- `SanitizeFilePath(this string json)`
- `string NormalizeLinux(this string path)`
- `string NormalizeWindows(this string path)`

### RegexExtensions
- `ContainsAtLeastOneLowercaseLetter(this string value)`
- `ContainsAtLeastOneUppercaseLetter(this string value)`
- `ContainsAtLeastOneDigit(this string value)`
- `ContainsAtLeastOneSpecialCharacter(this string value)`

### EnumExtensions
- `T ParseEnum<T>(this string value)`

### LinqExtensions
- `ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)`

## Conclusion

Use AStar.Dev.Utilities to keep common helper logic consistent and easy to reuse across projects.
