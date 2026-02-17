# Debug Logging System - Usage Guide

## Overview

A flexible debug logging system that writes to the SQLite database when enabled per-account. Uses AsyncLocal context to flow account information through async operations without requiring explicit parameter passing.

## Architecture Components

### 1. Database Layer

- **DebugLogEntity**: Entity for storing logs (Id, AccountId, TimestampUtc, LogLevel, Source, Message, Exception)
- **Migration**: `20260107202506_AddDebugLogsTable.cs`

### 2. Service Layer

- **IDebugLogger**: Interface for debug logging operations
- **DebugLogger**: Implementation that checks EnableDebugLogging and writes to DB
- **DebugLogContext**: Static AsyncLocal context holder
- **DebugLog**: Static facade for convenient access

### 3. Integration

- Registered as Scoped in ServiceConfiguration
- Initialized in App.axaml.cs startup
- Context set/cleared in SyncEngine.StartSyncAsync

## How It Works

1. **Context Setting**: At the start of each sync operation, `DebugLogContext.SetAccountId(accountId)` is called
2. **Context Flow**: AsyncLocal flows the context through all async operations in that execution tree
3. **Logging Check**: Before writing, DebugLogger queries the account's `EnableDebugLogging` setting
4. **Database Write**: If enabled, logs are written to the DebugLogs table
5. **Context Cleanup**: Context is cleared in the finally block

## Usage Patterns

### Pattern 1: Static Access (Recommended for most cases)

```csharp
// Entry logging
await DebugLog.EntryAsync("ClassName.MethodName", cancellationToken);

// Info logging
await DebugLog.LogInfoAsync("ClassName.MethodName", "Details about what happened", cancellationToken);

// Error logging
try 
{
    // ... code ...
}
catch (Exception ex)
{
    await DebugLog.LogErrorAsync("ClassName.MethodName", "What failed", ex, cancellationToken);
    throw;
}

// Exit logging
await DebugLog.ExitAsync("ClassName.MethodName", cancellationToken);
```

### Pattern 2: Dependency Injection (When testing needed)

```csharp
public class MyService
{
    private readonly IDebugLogger _debugLogger;
    
    public MyService(IDebugLogger debugLogger)
    {
        _debugLogger = debugLogger;
    }
    
    public async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        await _debugLogger.LogEntryAsync("MyService.DoWorkAsync", cancellationToken);
        // ... work ...
        await _debugLogger.LogExitAsync("MyService.DoWorkAsync", cancellationToken);
    }
}
```

## Setting Account Context

Only needed at operation boundaries (e.g., start of sync):

```csharp
try
{
    // Set context once at the boundary
    DebugLogContext.SetAccountId(accountId);
    
    // All methods called from here will have access to the context
    await DoSyncWorkAsync();
}
finally
{
    // Always clear in finally
    DebugLogContext.Clear();
}
```

## Example: Adding Logging to a New Service

```csharp
public async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken)
{
    await DebugLog.EntryAsync("FileProcessor.ProcessFileAsync", cancellationToken);
    
    try
    {
        await DebugLog.LogInfoAsync("FileProcessor.ProcessFileAsync", 
            $"Processing file: {filePath}", cancellationToken);
        
        // ... file processing logic ...
        
        await DebugLog.LogInfoAsync("FileProcessor.ProcessFileAsync", 
            "File processed successfully", cancellationToken);
    }
    catch (IOException ex)
    {
        await DebugLog.LogErrorAsync("FileProcessor.ProcessFileAsync", 
            $"Failed to process file: {filePath}", ex, cancellationToken);
        throw;
    }
    finally
    {
        await DebugLog.ExitAsync("FileProcessor.ProcessFileAsync", cancellationToken);
    }
}
```

## Performance Considerations

- **Minimal overhead when disabled**: If account has EnableDebugLogging=false, only a DB query to check the setting
- **Async operations**: All logging is async and doesn't block
- **Context is cheap**: AsyncLocal has minimal overhead
- **No constructor pollution**: Static access means no new DI parameters needed

## Querying Debug Logs

Access logs via SQL:

```sql
SELECT * FROM DebugLogs 
WHERE AccountId = 'account-id' 
ORDER BY TimestampUtc DESC 
LIMIT 100;
```

Or create a repository if needed:

```csharp
public interface IDebugLogRepository
{
    Task<IReadOnlyList<DebugLogEntry>> GetByAccountIdAsync(
        string accountId, int pageSize, int skip, CancellationToken cancellationToken = default);
}
```

## Best Practices

1. **Use descriptive sources**: Always include ClassName.MethodName for easy tracking
2. **Log at boundaries**: Entry/Exit for major operations (not every method)
3. **Log meaningful info**: Include context like file paths, counts, status
4. **Always log errors**: Catch blocks should always log before rethrowing
5. **Don't log sensitive data**: Avoid logging passwords, tokens, full file contents
6. **Use cancellation tokens**: Pass them through for proper cancellation support

## Testing

### Testing with DI

```csharp
[Fact]
public async Task ProcessFileLogsEntry()
{
    IDebugLogger mockLogger = Substitute.For<IDebugLogger>();
    var service = new MyService(mockLogger);
    
    await service.ProcessFileAsync("test.txt", CancellationToken.None);
    
    await mockLogger.Received(1).LogEntryAsync("MyService.ProcessFileAsync", Arg.Any<CancellationToken>());
}
```

### Testing with Static (Integration)

```csharp
[Fact]
public async Task ProcessFileCreatesDebugLogWhenEnabled()
{
    // Arrange: Create account with EnableDebugLogging=true
    var account = new AccountInfo(..., EnableDebugLogging: true);
    await accountRepo.AddAsync(account);
    
    DebugLogContext.SetAccountId(account.AccountId);
    
    // Act
    await service.ProcessFileAsync("test.txt", CancellationToken.None);
    
    // Assert: Query DebugLogs table
    var logs = await context.DebugLogs
        .Where(l => l.AccountId == account.AccountId)
        .ToListAsync();
    logs.Should().NotBeEmpty();
}
```

## Troubleshooting

**Logs not appearing?**

1. Check account's EnableDebugLogging is true
2. Verify DebugLogContext.SetAccountId() was called
3. Ensure migration has been applied
4. Check for exceptions in log writing (won't fail silently)

**Context is null?**

- Context only flows through async continuations in the same execution tree
- If you start a new Task/Thread, you must set context again
- Fire-and-forget tasks won't have context (use await)
