namespace LibraryManagementSystem.Models;

public class Book : Item
{
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
}
