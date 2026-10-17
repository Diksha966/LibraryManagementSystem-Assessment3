namespace LibraryManagementSystem.Models;

public class Reservation
{
    public int Id { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    public int BorrowerId { get; set; }
    public Borrower? Borrower { get; set; }

    public DateTime ReservedAt { get; set; } = DateTime.Now;

    public DateTime? NotifiedAt { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Waiting;
}
