namespace LibraryManagementSystem.Models;

public class Item
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ItemStatus Status { get; set; } = ItemStatus.Available;
}
