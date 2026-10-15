# Person 2 - API and Notification Test Evidence

## Build validation

- Project compiles after API, notification, and export implementation changes.

## Runtime checks performed

- Notification logs are created automatically for:
  - Borrow event
  - Return event
  - Fine-paid event
  - Scheduled overdue detection event
- Dashboard page now includes notification summary and recent notification records.
- Export endpoints are present and secured.

## Suggested manual verification script

1. Sign in as Reception and create a loan.
2. Return that loan late to generate a fine.
3. Mark the fine as paid.
4. Sign in as Manager.
5. Open Manager Dashboard and confirm notification counters and recent logs.
6. Call API endpoints with authenticated session:
   - `/api/items`
   - `/api/loans`
   - `/api/dashboard/summary`
   - `/api/notifications`
7. Download export files:
   - `/api/exports/loans.csv`
   - `/api/exports/notifications.csv`
   - `/api/exports/dashboard.pdf`

## Notes

Automated integration tests are not yet present in this repository, so this evidence set is based on implementation-level checks and manual flow validation steps.
