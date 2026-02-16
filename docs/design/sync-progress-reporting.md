# Sync Progress Reporting

## Purpose

Progress reporting during sync must carry both the external account identifier and the hashed account identifier. This lets UI and logging layers display user-facing account IDs while keeping traceability for internal logs and database writes.

## Delegate Signature

The progress callback signature used by `IFileTransferService` includes both identifiers:

```
Action<string, HashedAccountId, SyncStatus, int, int, long, long, int, int, int, int, string?, long?>
```

The first `string` is the OneDrive account ID; the second parameter is the hashed account ID.

## Rationale

- The sync UI uses the clear account ID for display.
- Logging and repository calls rely on the hashed ID for consistency.
- Passing both avoids accidental substitution of hashed IDs in UI paths.

## Call Site Guidance

- Always pass `accountId` first, then `hashedAccountId`.
- Do not repurpose the account ID parameter for hashed values.
