using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class Toy : Item
{
    [Required]
    [StringLength(50)]
    public string ToyType { get; set; } = string.Empty;

    [Range(0, 18)]
    public int RecommendedAge { get; set; }
}