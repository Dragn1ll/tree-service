namespace TreeService.Contracts.Dto;

public class TreeNodeDto
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int? ParentId { get; set; }
    
    public List<TreeNodeDto> Children { get; set; } = [];
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}