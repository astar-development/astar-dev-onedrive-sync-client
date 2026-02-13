# AStar.Dev.Source.Generators

## Introduction

AStar.Dev.Source.Generators is a focused set of Roslyn source generators that reduce boilerplate in AStar Dev projects. The generators are designed to keep registration and wiring code consistent, predictable, and easy to maintain.

## Purpose and Scope

This package provides high-level, compile-time generation for common application wiring tasks. It does not replace DI frameworks or configuration providers; it only generates the glue code that teams otherwise hand-write repeatedly.

## Target Audience

- Internal developers building AStar Dev applications
- External developers integrating AStar Dev packages
- Contributors extending or maintaining the generators

## Key Features

- DI service registration generator that produces `IServiceCollection` extensions based on service attributes
- Options registration generator that binds configuration sections to options classes and registers them in DI

## Examples and Code Snippets

```csharp
// Example intent: annotate services once, then let the generator emit registration code.
[Service(ServiceLifetime.Scoped, As = typeof(IMyOtherService))] // This service will be registered as IMyOtherService with scoped lifetime.
public sealed class MyService : IMyService, IMyOtherService { }
```

```csharp
// Example intent: bind and register options from configuration with generated glue.
// Exact attribute names may vary by project conventions.
[Options("MyOptions")]
public sealed class MyOptions
{
 public string Endpoint { get; init; } = string.Empty;
}
```

## Conclusion

Use AStar.Dev.Source.Generators to keep DI and options registration consistent, reduce boilerplate, and make configuration wiring easier to review.
