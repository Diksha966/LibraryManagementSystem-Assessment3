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
}