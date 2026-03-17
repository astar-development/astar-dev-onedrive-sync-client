# AStar.Dev.Functional.Extensions

**Version**: 0.4.5
**Target Framework**: .NET 10.0
**License**: MIT

A functional programming library for C# inspired by F#, providing robust error handling and optional value patterns without exceptions.

---

## Overview

AStar.Dev.Functional.Extensions brings functional programming patterns to C# applications, enabling cleaner, more expressive error handling and optional value management. This library eliminates the need for exception-based control flow in business logic while maintaining type safety and composability.

### Key Features

- **`Result<TSuccess, TError>`**: Discriminated union representing success or failure
- **`Option<T>`**: Type-safe representation of values that may or may not exist
- **Async/Await Support**: First-class async operations with `MatchAsync`, `MapAsync`, `BindAsync`
- **LINQ Integration**: Query syntax support for functional composition
- **Pattern Matching**: Exhaustive pattern matching with `Match()` for all cases
- **Extension Methods**: Rich set of combinators (`Map`, `Bind`, `OrElse`, `Filter`, etc.)
- **ViewModels Integration**: Specialized extensions for MVVM patterns

---

## Core Types

### `Result<TSuccess, TError>`

Represents an operation that can succeed with a value of type `TSuccess` or fail with an error of type `TError`.

**Common Usage Pattern**:

```csharp
// Instead of throwing exceptions:
public Result<User, string> GetUser(int userId)
{
    if (userId <= 0)
        return "Invalid user ID"; // Implicit conversion to Error
    
    var user = _repository.Find(userId);
    return user ?? "User not found"; // Null becomes Error
}

// Consume with pattern matching:
var result = GetUser(42);
result.Match(
    onSuccess: user => Console.WriteLine($"Found: {user.Name}"),
    onFailure: error => Console.WriteLine($"Error: {error}")
);
```

**Key Methods**:

- `Match<TResult>()`: Pattern match to handle both success and failure cases
- `MatchAsync<TResult>()`: Async version with support for async handlers
- `Map()`: Transform the success value (functor pattern)
- `Bind()`: Chain operations that return `Result` (monad pattern)
- `MapError()`: Transform the error value
- Implicit conversions from `TSuccess` and `TError` for concise syntax

### `Option<T>`

Represents a value that may or may not be present, eliminating null reference issues.

**Common Usage Pattern**:

```csharp
// Instead of returning null:
public Option<User> FindUser(string email)
{
    var user = _repository.FindByEmail(email);
    return user; // Null automatically becomes None
}

// Consume safely:
var userOption = FindUser("test@example.com");
userOption.Match(
    onSome: user => Console.WriteLine($"Welcome {user.Name}"),
    onNone: () => Console.WriteLine("User not found")
);
```

**Key Methods**:

- `Match<TResult>()`: Pattern match for Some/None cases
- `Map()`: Transform the value if present
- `Bind()`: Chain operations that return `Option`
- `Filter()`: Keep value only if predicate is true
- `OrElse()`: Provide alternative if None
- `IsSome` / `IsNone`: Boolean properties for checking state

---

## Advanced Features

### Async Support

All core operations have async variants:

```csharp
public async Task<Result<User, string>> GetUserAsync(int userId)
{
    var result = await _repository.GetByIdAsync(userId);
    return result ?? "User not found";
}

var userResult = await GetUserAsync(42);
await userResult.MatchAsync(
    onSuccess: async user => await SaveToCache(user),
    onFailure: error => LogError(error)
);
```

### LINQ Query Syntax

Compose operations using familiar LINQ syntax:

```csharp
var result = 
    from user in GetUser(userId)
    from profile in GetProfile(user.ProfileId)
    from settings in GetSettings(profile.SettingsId)
    select new { user, profile, settings };
```

### Functional Composition

Chain operations fluently:

```csharp
var result = GetUser(userId)
    .Map(user => user.ToDto())
    .Bind(dto => ValidateDto(dto))
    .MapError(error => $"Failed to get user: {error}");
```

---

## Use Cases

### Error Handling Without Exceptions

Replace exception-based control flow with explicit error types:

```csharp
// Before:
try { var user = GetUser(id); }
catch (NotFoundException ex) { /* handle */ }
catch (ValidationException ex) { /* handle */ }

// After:
GetUser(id).Match(
    onSuccess: user => ProcessUser(user),
    onFailure: error => HandleError(error)
);
```

### Optional Values

Handle missing data without null checks:

```csharp
// Before:
var config = LoadConfig();
if (config?.ApiKey != null) { UseApiKey(config.ApiKey); }

// After:
LoadConfig()
    .Bind(c => c.ApiKey)
    .Match(
        onSome: apiKey => UseApiKey(apiKey),
        onNone: () => UseDefaultApiKey()
    );
```

### Repository Pattern

Return explicit success/failure from data access:

```csharp
public interface IRepository<T>
{
    Task<Result<T, string>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<Unit, string>> SaveAsync(T entity, CancellationToken ct = default);
    Task<Option<T>> FindByEmailAsync(string email, CancellationToken ct = default);
}
```

### Service Layer

Compose business logic without exception handling:

```csharp
public async Task<Result<UserDto, string>> CreateUserAsync(CreateUserRequest request)
{
    return await ValidateRequest(request)
        .BindAsync(async req => await _repository.CreateAsync(req.ToEntity()))
        .MapAsync(async user => await EnrichWithProfile(user))
        .Map(user => user.ToDto());
}
```

---

## Additional Utilities

### `Unit`

Represents the absence of a meaningful value (similar to `void` but usable as a type parameter).

### `Pattern`

Utility class for pattern matching scenarios.

### `Try`

Execute operations that may throw exceptions and wrap results in `Result<T, Exception>`.

```csharp
var result = Try.Execute(() => JsonSerializer.Deserialize<User>(json));
// Returns Result<User, Exception>
```

### Extension Methods

- **Collection Extensions**: `FirstOrNone()`, `SingleOrNone()`, `ToOption()`, `SequenceResults()`
- **ViewModel Extensions**: Integration helpers for MVVM patterns
- **Error Response**: Structured error handling for API responses

---

## Dependencies

- **Microsoft.Extensions.Logging** (10.0.2): Optional logging integration
- **AStar.Dev.Logging.Extensions**: Internal logging utilities

---

## Target Audience

This library is ideal for:

- **Enterprise Applications**: Robust error handling without exception spam
- **Domain-Driven Design**: Explicit failure cases in domain logic
- **MVVM Applications**: Clean ViewModel error handling (Avalonia, WPF)
- **Repository/Service Patterns**: Type-safe data access and business logic
- **Functional Programming Enthusiasts**: F#/Haskell-style patterns in C#

---

## Philosophy

**Explicit over Implicit**: Make success and failure cases visible in method signatures.

**Composability**: Chain operations using `Map`, `Bind`, and LINQ syntax.

**Type Safety**: Leverage the compiler to enforce handling of all cases.

**No Hidden Control Flow**: Avoid exceptions for expected failures; use `Result` for control flow.

---

## Contributing

This package is part of the AStar Development internal tooling ecosystem. For issues or feature requests, contact the development team.

---

**Copyright**: AStar Development 2025
**Author**: Jason Barden
**Repository**: <https://github.com/astar-development/astar-dev-functional-extensions>

---

This README provides:

1. **Clear Value Proposition**: Explains what the library does and why developers should use it
2. **Core Types Documentation**: Detailed examples for `Result<T>` and `Option<T>` with real-world usage
3. **Advanced Features**: Shows async support, LINQ integration, and functional composition
4. **Practical Use Cases**: Demonstrates how to apply the library in common scenarios (repositories, services, error handling)
5. **Additional Utilities**: Covers supporting types like `Unit`, `Pattern`, and `Try`
6. **Philosophy Section**: Explains the design principles behind the library
7. **Target Audience**: Helps developers understand if this library fits their needs

The content is structured to progressively introduce concepts from basic to advanced, with plenty of code examples showing the before/after differences that make the value immediately clear.---

This README provides:

1. **Clear Value Proposition**: Explains what the library does and why developers should use it
2. **Core Types Documentation**: Detailed examples for `Result<T>` and `Option<T>` with real-world usage
3. **Advanced Features**: Shows async support, LINQ integration, and functional composition
4. **Practical Use Cases**: Demonstrates how to apply the library in common scenarios (repositories, services, error handling)
5. **Additional Utilities**: Covers supporting types like `Unit`, `Pattern`, and `Try`
6. **Philosophy Section**: Explains the design principles behind the library
7. **Target Audience**: Helps developers understand if this library fits their needs

The content is structured to progressively introduce concepts from basic to advanced, with plenty of code examples showing the before/after differences that make the value immediately clear.
