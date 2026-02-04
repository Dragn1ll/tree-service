using Microsoft.EntityFrameworkCore;
using TreeService.Domain.Abstractions;
using TreeService.Persistence.SQLite;
using TreeService.Persistence.SQLite.Entities;

namespace TreeService.Domain.Services;

public class TreeValidator : ITreeValidator
{
    private readonly AppDbContext _context;
    
    public TreeValidator(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task ValidateNoCycleAsync(TreeNode node)
    {
        if (!node.ParentId.HasValue)
        {
            return;
        }
        
        var visited = new HashSet<Guid> { node.Id };
        var currentId = node.ParentId.Value;
        
        while (currentId != Guid.Empty)
        {
            if (!visited.Add(currentId))
            {
                throw new InvalidOperationException("Обнаружена циклическая ссылка");
            }

            var id = currentId;
            var parent = await _context.TreeNodes
                .Where(n => n.Id == id)
                .Select(n => new { n.Id, n.ParentId })
                .FirstOrDefaultAsync();

            if (parent == null)
            {
                break;
            }
            
            currentId = parent.ParentId ?? Guid.Empty;
        }
    }
}