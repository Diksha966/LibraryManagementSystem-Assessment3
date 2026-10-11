using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class Music : Item
{
    [Required]
    [StringLength(100)]
    public string Artist { get; set; } = string.Empty;

    [Range(1900, 2100)]
    public int Year { get; set; }

    [StringLength(30)]
    public string Format { get; set; } = string.Empty;
}