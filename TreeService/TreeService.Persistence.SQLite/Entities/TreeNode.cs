using System.ComponentModel.DataAnnotations;

namespace TreeService.Persistence.SQLite.Entities;

public class TreeNode
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int? ParentId { get; set; }
    public TreeNode? Parent { get; set; }
    
    public IList<TreeNode> Children { get; set; } = [];
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public string Path { get; set; } = string.Empty;
}