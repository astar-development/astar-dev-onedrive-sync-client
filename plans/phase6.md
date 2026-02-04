# Phase 6: Telemetry & Diagnostics

**Purpose**: Implement OpenTelemetry for observability and per-account diagnostic logging.

**Task 6.1**: Configure OpenTelemetry traces

- [ ] Add OpenTelemetry configuration in `Program.cs`
- [ ] Configure trace collection for sync operations
- [ ] Add activity sources for instrumentation
- [ ] Test trace generation

**Task 6.2**: Configure OpenTelemetry metrics

- [ ] Configure metric collection (sync duration, items synced, conflicts)
- [ ] Add meters for custom metrics
- [ ] Test metric generation

**Task 6.3**: Implement DatabaseTraceExporter

- [ ] Create custom `DatabaseTraceExporter` class
- [ ] Export traces to PostgreSQL database (onedrive schema)
- [ ] Add unit tests mocking database writes

**Task 6.4**: Implement DatabaseLogProvider

- [ ] Create custom `ILoggerProvider` implementation
- [ ] Write logs to `ApplicationLogs` table
- [ ] Add structured properties (JSONB column)
- [ ] Add unit tests for log provider

**Task 6.5**: Implement fallback file-based exporter

- [ ] Create file-based trace exporter for cases where DB unavailable
- [ ] Write traces to `{AppDataFolder}/logs/traces.json`
- [ ] Add unit tests for file exporter

**Task 6.6**: Implement per-account diagnostic logging

- [ ] Create `DiagnosticSettings` entity and repository
- [ ] Implement per-account log level configuration
- [ ] Integrate with logging provider to respect account-specific levels
- [ ] Add unit tests for per-account filtering

**Task 6.7**: Integrate debug logging toggle in Edit Account UI

- [ ] Add debug logging toggle to `EditAccountViewModel`
- [ ] Wire toggle to DiagnosticSettings repository
- [ ] Update logging configuration dynamically
- [ ] Test toggle behavior

**Task 6.8**: Implement structured logging helpers

- [ ] Create `StructuredLoggingExtensions` class
- [ ] Add helper methods for semantic logging (with properties)
- [ ] Add unit tests for logging helpers

**Task 6.9**: Implement end-to-end telemetry test

- [ ] Test: Generate traces/metrics/logs → verify stored in database
- [ ] Test: Per-account debug logging filters correctly
- [ ] Write BDD scenario for diagnostic logging
