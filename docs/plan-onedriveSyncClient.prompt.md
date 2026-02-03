# OneDrive Sync Client - Project Plan

## Overview

This is the master project plan for the AStar OneDrive Sync Client. For detailed documentation, see the links below.

### Project Summary

Build a cross-platform OneDrive sync client using .NET 10, AvaloniaUI, and ReactiveUI with a layered, feature-sliced architecture. Implement secure multi-account support with proactive OAuth token refresh, bidirectional folder syncing via delta tokens, and background sync scheduling.

### Key Features

- **Multi-Account Support**: Manage multiple OneDrive accounts with GDPR-compliant data storage
- **Bidirectional Sync**: Two-way folder synchronization with delta tokens
- **Conflict Resolution**: User-driven conflict resolution with multiple strategies
- **Concurrent Operations**: Configurable upload/download limits per account
- **Background Scheduling**: Automatic periodic sync with configurable intervals
- **Diagnostic Logging**: Per-account debug logging with log viewer UI
- **Cross-Platform**: Windows, macOS, and Linux support with platform-specific secure storage

## Documentation Structure

This plan has been split into focused documents for better maintainability:

- **[Architecture](architecture.md)** - System architecture, design decisions, and feature implementations
- **[Database Schema](database-schema.md)** - PostgreSQL schema definition and GDPR compliance
- **[Implementation Roadmap](roadmap.md)** - Detailed task breakdown across 9 phases
- **[Testing Strategy](testing-strategy.md)** - BDD scenarios and test requirements _(coming soon)_
- **[Configuration Reference](configuration.md)** - Settings and environment variables _(coming soon)_

## Quick Links

- [Project Structure](PROJECT_STRUCTURE.md) - Folder organization and conventions
- [Entra ID Setup Guide](guides/entra-id-app-registration.md) - OAuth app registration  
- [README](../README.md) - Getting started and dependencies

## Current Status

- ✅ **Phase 0**: Configuration setup complete
- 🔄 **Phase 1**: Foundation architecture in progress
- ⏳ **Phases 2-8**: Pending

## Technology Stack

- **.NET 10**: Target framework
- **AvaloniaUI 11.3**: Cross-platform UI framework
- **ReactiveUI 22.3**: MVVM with reactive extensions
- **EF Core 10**: ORM for PostgreSQL
- **PostgreSQL 12+**: Database with `onedrive` schema
- **MSAL**: Microsoft Authentication Library
- **Kiota V5**: Graph API client generation
- **Serilog**: Structured logging with PostgreSQL sink
- **OpenTelemetry**: Observability and metrics

## Development Workflow

Each task in the [Implementation Roadmap](roadmap.md) should:

1. Be implemented in a separate feature branch
2. Include appropriate unit/integration tests
3. Pass all quality gates before merge
4. Result in a focused, reviewable pull request

See `.github/copilot-instructions.md` for branch naming and commit conventions.

---

**Document Version**: 1.0  
**Last Updated**: February 3, 2026  
**Maintained By**: AStar Development Team
