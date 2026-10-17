using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public record HoldResult(bool Success, string Message, int Position);

public record ReservationInfo(Reservation Reservation, int Position);

/// <summary>
/// Reservation / waitlist rules. Queue order is ReservedAt, then Id.
/// "Active" means Waiting or Notified.
/// </summary>
public class ReservationService
{
    private readonly LibraryDbContext _context;
    private readonly ILogger<ReservationService> _logger;
    private readonly INotificationService _notifications;

    public ReservationService(
        LibraryDbContext context,
        ILogger<ReservationService> logger,
        INotificationService notifications)
    {
        _context = context;
        _logger = logger;
        _notifications = notifications;
    }

    // Active reservations for an item, first in line first.
    public async Task<List<Reservation>> GetQueueAsync(int itemId)
    {
        return await _context.Reservations
            .Include(r => r.Borrower)
            .Where(r => r.ItemId == itemId &&
                (r.Status == ReservationStatus.Waiting ||
                 r.Status == ReservationStatus.Notified))
            .OrderBy(r => r.ReservedAt)
            .ThenBy(r => r.Id)
            .ToListAsync();
    }

    public async Task<HoldResult> PlaceHoldAsync(int itemId, int borrowerId)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null)
            return new HoldResult(false, "That item could not be found.", 0);

        if (item.Status == ItemStatus.Available)
            return new HoldResult(false,
                "This item is available now, so you can borrow it instead of reserving it.", 0);

        if (item.Status == ItemStatus.Destroyed)
            return new HoldResult(false,
                "This item has been destroyed and cannot be reserved.", 0);

        var borrower = await _context.Borrowers.FindAsync(borrowerId);

        if (borrower == null)
            return new HoldResult(false, "Member not found.", 0);

        bool alreadyQueued = await _context.Reservations.AnyAsync(r =>
            r.ItemId == itemId &&
            r.BorrowerId == borrowerId &&
            (r.Status == ReservationStatus.Waiting ||
             r.Status == ReservationStatus.Notified));

        if (alreadyQueued)
            return new HoldResult(false,
                "You already have a reservation for this item.", 0);

        bool hasItemOnLoan = await _context.Loans.AnyAsync(l =>
            l.ItemId == itemId &&
            l.BorrowerId == borrowerId &&
            l.Status == LoanStatus.Active);

        if (hasItemOnLoan)
            return new HoldResult(false,
                "You currently have this item on loan.", 0);

        int alreadyInQueue = await _context.Reservations.CountAsync(r =>
            r.ItemId == itemId &&
            (r.Status == ReservationStatus.Waiting ||
             r.Status == ReservationStatus.Notified));

        _context.Reservations.Add(new Reservation
        {
            ItemId = itemId,
            BorrowerId = borrowerId,
            ReservedAt = DateTime.Now,
            Status = ReservationStatus.Waiting
        });

        await _context.SaveChangesAsync();

        return new HoldResult(true,
            "Your reservation has been placed.", alreadyInQueue + 1);
    }

    /// <summary>
    /// Call whenever an item becomes Available (return, status edit).
    /// Notifies the first Waiting patron, unless someone has already been notified.
    /// </summary>
    public async Task NotifyNextAsync(int itemId)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null || item.Status != ItemStatus.Available)
            return;

        var queue = await GetQueueAsync(itemId);

        if (queue.Count == 0 ||
            queue.Any(r => r.Status == ReservationStatus.Notified))
        {
            return;
        }

        var next = queue[0];
        next.Status = ReservationStatus.Notified;
        next.NotifiedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        await SendAvailableNoticeAsync(next, item);
    }

    // Writes the "item is ready" notice to the shared notification log
    // (Person 2's NotificationService) and to the console.
    private async Task SendAvailableNoticeAsync(Reservation reservation, Item item)
    {
        _logger.LogInformation(
            "[SIMULATED EMAIL] To: {Email} | Subject: Your reserved item is available | " +
            "Body: Hello {Name}, \"{Title}\" ({Code}) is now available to borrow.",
            reservation.Borrower?.Email,
            reservation.Borrower?.Name,
            item.Title,
            item.LibraryCode);

        await _notifications.LogAsync(
            category: "Reservations",
            severity: "Info",
            title: "Reserved Item Available",
            message: $"{reservation.Borrower?.Name} ({reservation.Borrower?.Email}): " +
                     $"\"{item.Title}\" ({item.LibraryCode}) is now available to borrow.",
            eventKey: $"reservation-ready-{reservation.Id}",
            entityType: "Reservation",
            entityId: reservation.Id,
            branchId: item.BranchId);
    }

    // True if nobody is queued, or this borrower is first in line.
    public async Task<bool> CanBorrowAsync(int itemId, int borrowerId)
    {
        var queue = await GetQueueAsync(itemId);

        return queue.Count == 0 || queue[0].BorrowerId == borrowerId;
    }

    // Call after a loan is created: closes that patron's reservation.
    public async Task FulfilAsync(int itemId, int borrowerId)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r =>
                r.ItemId == itemId &&
                r.BorrowerId == borrowerId &&
                (r.Status == ReservationStatus.Waiting ||
                 r.Status == ReservationStatus.Notified));

        if (reservation != null)
        {
            reservation.Status = ReservationStatus.Fulfilled;
            await _context.SaveChangesAsync();
        }
    }

    // Cancels a reservation. Pass requiredBorrowerId when a patron cancels
    // their own hold. If the cancelled hold had been notified, the next
    // person in line is notified.
    public async Task<bool> CancelAsync(
        int reservationId,
        int? requiredBorrowerId = null)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r =>
                r.Id == reservationId &&
                (r.Status == ReservationStatus.Waiting ||
                 r.Status == ReservationStatus.Notified));

        if (reservation == null)
            return false;

        if (requiredBorrowerId.HasValue &&
            reservation.BorrowerId != requiredBorrowerId.Value)
        {
            return false;
        }

        bool wasNotified = reservation.Status == ReservationStatus.Notified;

        reservation.Status = ReservationStatus.Cancelled;
        await _context.SaveChangesAsync();

        if (wasNotified)
            await NotifyNextAsync(reservation.ItemId);

        return true;
    }

    public async Task<List<ReservationInfo>> GetBorrowerReservationsAsync(
        int borrowerId)
    {
        var mine = await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Item)
            .Where(r => r.BorrowerId == borrowerId &&
                (r.Status == ReservationStatus.Waiting ||
                 r.Status == ReservationStatus.Notified))
            .OrderBy(r => r.ReservedAt)
            .ToListAsync();

        var result = new List<ReservationInfo>();

        foreach (var r in mine)
        {
            int ahead = await _context.Reservations.CountAsync(o =>
                o.ItemId == r.ItemId &&
                (o.Status == ReservationStatus.Waiting ||
                 o.Status == ReservationStatus.Notified) &&
                (o.ReservedAt < r.ReservedAt ||
                 (o.ReservedAt == r.ReservedAt && o.Id < r.Id)));

            result.Add(new ReservationInfo(r, ahead + 1));
        }

        return result;
    }
}
