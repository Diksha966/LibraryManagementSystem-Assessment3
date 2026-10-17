namespace LibraryManagementSystem.Models;

public enum ReservationStatus
{
    Waiting,    // In the queue, item not yet available to them
    Notified,   // Item is available and this patron has been told
    Fulfilled,  // Patron borrowed the item
    Cancelled   // Cancelled by the patron or by staff
}
