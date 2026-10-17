using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

// Person 1 (multi-branch inventory): a physical library branch.
// Every item belongs to exactly one branch (Item.BranchId).
public class Branch
{
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    [RegularExpression("^[A-Za-z0-9]+$",
        ErrorMessage = "Use letters and numbers only, for example PAL.")]
    [Display(Name = "Branch code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Branch name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Address { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<Item> Items { get; set; } = new();
}
