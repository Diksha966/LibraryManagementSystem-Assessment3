# Person 3 integration notes

How the Person 3 modules were merged with Person 1 (branches, transfers) and Person 2 (API, notifications, exports).

| # | Integration point | Resolution |
|---|---|---|
| IP1 | `LoansController.Create` and `Return` | Kept Person 2's notification logging and Person 1's in-transit and open-transfer checks. Added the reservation queue check on borrow, `FulfilAsync` after borrowing and `NotifyNextAsync` after a return. |
| IP2 | `KioskController.Checkout` (second borrow path) | Now refuses items In Transit and items with an open transfer, and writes the same "Item Borrowed" notification as the desk. |
| IP3 | `ReservationService` notice | Writes a "Reserved Item Available" entry through `INotificationService.LogAsync` (event key `reservation-ready-{id}`) and to the console. |
| IP4 | Item becomes Available | `NotifyNextAsync` is called after a return, after an Admin status edit to Available, after a transfer is received, and after an in-transit transfer is cancelled. |
| IP5 | Reports | Person 3's inventory report includes the item's branch and an In transit count. |

## Existing files changed

`Program.cs`, `Data/LibraryDbContext.cs`, `Controllers/LoansController.cs`, `Controllers/ItemsController.cs`, `Services/Branches/TransferService.cs`, `Views/Shared/_Layout.cshtml`, `Views/Manager/Index.cshtml`, `Views/Public/Details.cshtml`.

## Bug fixed

`OverdueNotificationBackgroundService` sets `LoanStatus.Overdue` as soon as the app starts, but `LoansController.Return` only handled `LoanStatus.Active`, so an overdue loan could not be returned. `Return` now accepts Active and Overdue loans.

## Migration

Person 3 adds one table (`Reservations`, model `Models/Reservation.cs`). The integration lead generates it with the branch and transfer tables in one migration.
