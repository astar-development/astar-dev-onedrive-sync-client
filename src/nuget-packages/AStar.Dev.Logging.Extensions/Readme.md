# AStar.Dev.Logging.Extensions

## Introduction

AStar.Dev.Logging.Extensions is a lightweight helper library that standardizes structured logging across AStar Dev applications and services. It provides small, focused extensions that make it easier to add context, write consistent log entries, and reduce boilerplate around common logging patterns.

## Purpose and Scope

This package targets internal and external developers who need a consistent way to enrich logs, record diagnostic events, and keep logging calls uniform across the codebase. It is intentionally narrow in scope and focuses on logging ergonomics rather than owning logging infrastructure.

## Target Audience

- Internal developers building AStar Dev applications and services
- External contributors integrating with AStar Dev components

## Key Features

- Structured logging helpers that encourage consistent message shapes
- Context enrichment utilities for correlating logs by account, operation, or component
- Lightweight extensions that integrate with existing logging frameworks
- Clear separation of logging intent from business logic

## Getting Started

Add the package reference, inject an `ILogger` where you need logging, and call the `AStarLog.Web` helpers for common HTTP status events.

```csharp
using AStar.Dev.Logging.Extensions.Messages;
using Microsoft.Extensions.Logging;

public sealed class RequestLogService
{
 private readonly ILogger _logger;

 public RequestLogService(ILogger<RequestLogService> logger)
 {
  _logger = logger;
 }

 public void LogBadRequest(string path)
 {
  AStarLog.Web.BadRequest(_logger, path);
 }

 public void LogUnauthorized(string path)
 {
  AStarLog.Web.Unauthorized(_logger, path);
 }

 public void LogServerError(string path)
 {
  AStarLog.Web.InternalServerError(_logger, path);
 }
}
```

You can use the rest of the web helpers in the same way:

```csharp
AStarLog.Web.NotFound(logger, requestPath);
AStarLog.Web.Conflict(logger, requestPath);
AStarLog.Web.TooManyRequests(logger, requestPath);
```

## Examples and Code Snippets

```csharp
// Example pattern showing structured, consistent logging usage.
// Exact APIs vary by consumer; the intent is a concise, enriched log entry.
logger.LogInformation("{Component} started for {AccountId}", "SyncEngine", accountId);
```

## Conclusion

Use AStar.Dev.Logging.Extensions when you need clean, consistent, and contextual logging without introducing heavy dependencies or boilerplate.
