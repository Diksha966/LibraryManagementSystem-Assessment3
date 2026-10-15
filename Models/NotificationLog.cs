using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class NotificationLog
{
    public int Id { get; set; }

    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [StringLength(30)]
    public string Severity { get; set; } = "Info";

    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    [StringLength(100)]
    public string? EntityType { get; set; }

    public int? EntityId { get; set; }

    [StringLength(120)]
    public string? EventKey { get; set; }

    // Branch is nullable for forward compatibility with multi-branch architecture.
    public int? BranchId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
