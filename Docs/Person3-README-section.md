## Self-service, reservations, import and reports (Person 3)

### Run it

```
dotnet restore
dotnet ef database update
dotnet run
```

`dotnet ef database update` needs the combined migration that the integration lead generates on `main`
(branches, transfers and the `Reservations` table). Person 3 does not add migrations.

### Demo accounts and data

| Who | Sign in |
|---|---|
| Admin | admin@library.com / Admin123! |
| Reception | reception@library.com / Reception123! |
| Manager | manager@library.com / Manager123! |
| Patron (kiosk and reserve page) | Member number `1` with john.smith@example.com, or `2` with jane.doe@example.com |

Seed items: `BK001` The Hobbit (Borrowed by John, overdue, Central), `TY001` LEGO (Available, Central),
`MU001` Thriller (Available, Northside).

### Where to find each module

| Module | URL | Who |
|---|---|---|
| Self-service kiosk | `/Kiosk` | Public |
| Reserve an item | Public Library, item details, Reserve this item (`/Reserve/Index/{id}`) | Public |
| Reservation queues | `/Reservations` | Reception |
| CSV import | `/Import` (sample file `SampleData/items-sample.csv`) | Admin |
| Reports and exports | `/Reports`, also a button on the Manager Dashboard | Manager |

### Quick test script

1. **Kiosk checkout.** Open `/Kiosk`, choose Borrow an item, sign in as member 1, scan `TY001`. The loan appears under My account.
2. **Reservation.** Open `BK001` in Public Library, choose Reserve this item, join the queue as member 2.
3. **Waitlist notification.** Log in as Reception, open Loans, return the BK001 loan (a late fine of $60 is created). `/Reservations` shows Jane as Notified, and a "Reserved Item Available" entry appears in the notification log on the Manager Dashboard.
4. **Queue fairness.** At the kiosk, try to borrow `BK001` as member 1: refused, it is held for another member. Borrow it as member 2: the reservation becomes Fulfilled.
5. **CSV import.** Log in as Admin, open Import Items, upload `SampleData/items-sample.csv`. Four rows import (into the branch named in `BranchCode`), two are skipped with reasons shown.
6. **Exports.** Log in as Manager, open Reports and Exports, download each report as CSV and PDF. The inventory report shows each item's branch and an In transit count.

### Integration checks (with Person 1 and Person 2)

1. **Transfer lock.** As Admin, request a transfer of `TY001` to Northside. At the kiosk, borrowing `TY001` is refused (requested for transfer). Cancel the transfer to unlock it.
2. **Transit and the queue.** Request, dispatch and receive a transfer of an Available item that has a reservation queue. On receipt the first person in the queue becomes Notified and a log entry is written.
3. **Overdue loans can be returned.** After the app has run once, The Hobbit's loan shows as Overdue. Returning it still works and creates the fine.

### Known limitations

- Patron identification on the kiosk and reserve page is member number plus email, with no PIN.
- Reservations do not expire, and the queue is per item, not per branch.
- Notices go to the notification log and the console only (no real email or SMS).
- No mock external metadata provider for the importer.
- No automated tests yet.
