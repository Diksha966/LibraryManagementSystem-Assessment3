using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class ItemFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string LibraryCode { get; set; } = string.Empty;

    [Required]
    public string ItemType { get; set; } = "Book";

    [Required]
    public ItemStatus Status { get; set; } = ItemStatus.Available;

    // Person 1: branch chosen when the item is created. On Edit it is shown
    // read-only, because an item only changes branch through a transfer.
    [Range(1, int.MaxValue, ErrorMessage = "Please choose a branch.")]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    public string? BranchName { get; set; }

    // Book
    [StringLength(100)]
    public string Author { get; set; } = string.Empty;

    [StringLength(50)]
    public string Genre { get; set; } = string.Empty;

    [StringLength(20)]
    public string ISBN { get; set; } = string.Empty;

    // Toy
    [StringLength(50)]
    public string ToyType { get; set; } = string.Empty;

    [Range(0, 18)]
    public int RecommendedAge { get; set; }

    // Music
    [StringLength(100)]
    public string Artist { get; set; } = string.Empty;

    [Range(1900, 2100)]
    public int Year { get; set; }

    [StringLength(30)]
    public string Format { get; set; } = string.Empty;
}