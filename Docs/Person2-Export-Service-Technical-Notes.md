# Person 2 - Export Service Technical Notes

## Implementation summary

The export service is implemented in `Services/Exports/ExportService.cs` behind `IExportService`.

Supported outputs:

- Loans CSV
- Notifications CSV
- Dashboard summary PDF

## CSV format details

- UTF-8 output.
- Header row included.
- Values are escaped when containing commas, quotes, or line breaks.
- Date format is `yyyy-MM-dd` for loan dates and `yyyy-MM-dd HH:mm:ss` for notification timestamps.

## PDF format details

- Lightweight internal PDF builder in `Services/Exports/SimplePdfBuilder.cs`.
- Single-page textual summary export.
- ASCII-safe output for compatibility with current project constraints.

## Security and access

Exports are exposed through read-only authenticated endpoints:

- `GET /api/exports/loans.csv`
- `GET /api/exports/notifications.csv`
- `GET /api/exports/dashboard.pdf`

Authorization is restricted to `Admin` and `Manager` roles.

## Branch readiness

Branch-aware query input (`branchId`) is included across API endpoints so export filtering can be extended without changing endpoint contracts once multi-branch schema is finalized.
