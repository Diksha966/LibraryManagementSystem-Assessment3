using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public abstract class Item
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
    public ItemStatus Status { get; set; } = ItemStatus.Available;

    // Person 1 (multi-branch inventory): the branch that currently holds
    // this item. Changes only when an inter-branch transfer is received.
    // The database default is 1 (Central Library), so code that creates
    // items without setting a branch still produces valid rows.
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    public Branch? Branch { get; set; }
}