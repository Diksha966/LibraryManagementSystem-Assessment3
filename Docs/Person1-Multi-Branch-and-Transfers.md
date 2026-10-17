# Person 1 – Multi-branch Inventory and Inter-branch Transfers

Owner: Diksha (Person 1 – architecture and integration)

## What this adds

| Area | What it does | Who can use it |
|---|---|---|
| Branches | Create, edit and deactivate branches (code, name, address). A branch cannot be deactivated while it holds items or has open transfers. | Admin (view: Manager) |
| Branch inventory | Every item belongs to one branch. Items list, public catalogue, Manager dashboard and API can be filtered by branch. | Admin, Manager, public |
| Transfers | Request → Dispatch → Receive, or Cancel. Full history kept for every item. | Admin, Reception |
| Locking | An item with an open transfer cannot be borrowed. Once dispatched its status is **In transit**. | Automatic |
| Logging | Every transfer step is written to the notification log (category `Transfers`) with the branch id. | Automatic |

## Business rules (enforced in `Services/Branches/TransferService.cs`)

1. Only an **Available** item can be requested.
2. One open (Requested or In Transit) transfer per item.
3. The destination must be an active branch and different from the item's current branch.
4. **Dispatch** only from Requested, and only if the item is still Available. The item status becomes **In transit**.
5. **Receive** only from In Transit. The item's `BranchId` changes to the destination and its status becomes Available. This is the only place an item changes branch.
6. **Cancel** from Requested (item unchanged) or In Transit (item returns to the source branch and becomes Available).
7. Transfers are never deleted; items with transfer history cannot be deleted.
8. Desk borrowing (`LoansController.Create`) refuses items that are in transit or have a requested transfer.
9. Admins cannot set **In transit** by hand, and cannot change an item's status while it has an open transfer.

## Files

New:

- `Models/Branch.cs`, `Models/ItemTransfer.cs`, `Models/TransferStatus.cs`
- `ViewModels/BranchViewModels.cs` (`BranchInventorySummary`, `TransferCreateViewModel`)
- `Services/Branches/ITransferService.cs`, `TransferService.cs`, `BranchService.cs`
- `Controllers/BranchesController.cs`, `Controllers/TransfersController.cs`
- `Views/Branches/*`, `Views/Transfers/*`, `Views/Shared/_ItemStatusBadge.cshtml`

Changed (small, commented `Person 1` blocks):

- `Models/Item.cs` – `BranchId` and `Branch`
- `Models/ItemStatus.cs` – `InTransit` added **at the end** so existing stored values do not change
- `Data/LibraryDbContext.cs` – `Branches`, `ItemTransfers`, relationships, unique branch code, seed branches, seed items assigned to branches
- `Program.cs` – registers `IBranchService` and `ITransferService`
- `Controllers/ItemsController.cs` and `Views/Items/*` – branch column and filter, branch on create, read-only branch on edit, transfer history, guards
- `Controllers/LoansController.cs` – refuses items in transit or requested for transfer; borrow log now includes the branch id
- `Controllers/PublicController.cs` and `Views/Public/*` – branch shown on each item, branch filter, In transit badge
- `Controllers/ManagerController.cs` and `Views/Manager/Index.cshtml` – In Transit count and Inventory by Branch table
- `Controllers/Api/ItemsApiController.cs`, `LoansApiController.cs`, `DashboardApiController.cs` – the `branchId` parameter agreed with Person 2 now filters results, and responses return the real branch
- `Views/Shared/_Layout.cshtml` – Branches and Transfers links

## Setup (run once after pulling)

```
dotnet tool install --global dotnet-ef      # only if dotnet ef is not installed
dotnet restore
dotnet build
dotnet ef migrations add AddBranchesAndTransfers
dotnet ef database update
dotnet run
```

The migration creates `Branches` and `ItemTransfers`, adds `Items.BranchId` (default 1) and seeds two branches:

| Id | Code | Name |
|---|---|---|
| 1 | CEN | Central Library |
| 2 | NTH | Northside Branch |

Seed items: BK001 and TY001 at Central Library, MU001 at Northside Branch. Existing items in an older database are placed in Central Library.

If an old `library.db` gives migration errors during testing, back it up, delete it and run `dotnet ef database update` again to create a fresh seeded database.

## Transfer workflow test script

Sign in as **admin@library.com / Admin123!** (or Reception for TC-T03 onwards). Record the result and take a screenshot for each step.

| ID | Steps | Expected result | Result |
|---|---|---|---|
| TC-T01 | Branches → Add Branch: code `PAL`, name `Palmerston Branch` | Branch saved and listed. Adding a second branch with code `pal` is refused (“Another branch already uses this code”). | |
| TC-T02 | Library Items → Branch filter = Northside Branch | Only MU001 (Thriller) is listed. | |
| TC-T03 | Transfers → Request Transfer: `TY001` to Palmerston Branch, reason “School visit” | Transfer #1 created with status Requested; TY001 still Available at Central Library with a “Transfer requested” badge. | |
| TC-T04 | Request a transfer of `BK001` (Borrowed) | Refused: “… is on loan and cannot be transferred.” | |
| TC-T05 | Request another transfer of `TY001` | Refused: “… already has an open transfer (#1).” | |
| TC-T06 | Request `MU001` to Northside Branch (its own branch) | Refused: “… is already at Northside Branch.” | |
| TC-T07 | As Reception, Loans → Create with `TY001` | Refused: “requested for transfer to another branch.” | |
| TC-T08 | Transfer #1 → Mark as Dispatched; then try Loans → Create with `TY001` | Transfer In transit; item status In transit; borrowing refused (“in transit between branches”). | |
| TC-T09 | Transfer #1 → Confirm Received | Transfer Received; TY001 now Available at Palmerston Branch; Branches page counts updated. | |
| TC-T10 | Request `MU001` to Central Library, dispatch, then Cancel with reason “Wrong item” | Transfer Cancelled; MU001 back to Available at Northside Branch; history shows each step with user and time. | |
| TC-T11 | Items → Edit TY001 while a transfer is open and change its status | Refused: open transfer must be received or cancelled first. | |
| TC-T12 | Branches → Edit Palmerston Branch, untick Active | Refused while it holds TY001. | |
| TC-T13 | Manager → Manager Dashboard | In Transit card and Inventory by Branch table show the same numbers as the Branches page. | |
| TC-T14 | Signed in as Manager, open `/api/items?branchId=2` and `/api/dashboard/summary?branchId=2` | Only Northside items and figures are returned, each with `branchId` and `branchName`. | |
| TC-T15 | Manager Dashboard → Recent notifications | Transfer Requested / Dispatched / Received / Cancelled entries appear. | |

## Integration notes for teammates

- **Person 3 (reservations):** two marked integration points in `TransferService`:
  - `RequestAsync`: refuse a transfer if the item is being held for a reservation.
  - `ReceiveAsync`: call `ReservationService.NotifyNextAsync(itemId)` because the item becomes Available at its new branch.

  `KioskController.Checkout` already refuses items that are not Available, so dispatched items are locked at the kiosk. It also needs the same `HasOpenTransferAsync` check as the desk, so items with a Requested transfer are refused.
- **Person 3 (CSV import):** imported items that do not set `BranchId` go to Central Library (database default 1). An optional `Branch` column can be added later.
- **Person 2 (API/exports):** `branchId` now filters Items, Loans and Dashboard endpoints; Notifications already filtered by `BranchId`. Transfer events are logged with `BranchId`, so the notifications filter works for them too.
- **Migrations:** generate `AddBranchesAndTransfers` on `main` after the Person 3 merge (`AddReservations`), one migration at a time.
