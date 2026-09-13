namespace LibraryManagementSystem.Models;

public class Music : Item
{
    public string Artist { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}
