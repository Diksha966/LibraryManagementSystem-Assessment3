# Person 2 - Documentation

## Security model

- All API endpoints require authentication.
- Read-only API routes use role-based authorization.
- No create/update/delete endpoints are exposed in the API surface.

Authorization by route:

- `/api/items`, `/api/loans`: `Admin`, `Reception`, `Manager`
- `/api/dashboard/*`, `/api/notifications`, `/api/exports/*`: `Admin`, `Manager`

## Endpoints

### Items API

- `GET /api/items`
  - Query: `search`, `status`, `branchId`
  - Returns item list with status and type metadata.
- `GET /api/items/{id}`
  - Query: `branchId`
  - Returns one item.

### Loans API

- `GET /api/loans`
  - Query: `status`, `overdueOnly`, `branchId`
  - Returns loan list with borrower and item metadata.
- `GET /api/loans/{id}`
  - Query: `branchId`
  - Returns one loan.

### Dashboard API

- `GET /api/dashboard/summary`
  - Query: `branchId`
  - Returns borrowing, item, fine, and notification aggregate data.

### Notification API

- `GET /api/notifications`
  - Query: `severity`, `category`, `take`, `branchId`
  - Returns notification logs ordered by latest first.

### Export API

- `GET /api/exports/loans.csv`
  - Returns CSV export of loans.
- `GET /api/exports/notifications.csv`
  - Returns CSV export of notification logs.
- `GET /api/exports/dashboard.pdf`
  - Returns PDF export containing dashboard summary metrics.
