using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TreeService.Contracts.Dto;
using TreeService.Contracts.TreeNode.Requests;
using TreeService.Domain.Abstractions;
using TreeService.Persistence.SQLite;
using TreeService.Persistence.SQLite.Entities;

namespace TreeService.Domain.Services;

public class TreeService : ITreeService
{
    private readonly AppDbContext _context;
    private readonly ITreeValidator _validator;
    private readonly ILogger<TreeService> _logger;
    
    public TreeService(AppDbContext context, ITreeValidator validator, ILogger<TreeService> logger)
    {
        _context = context;
        _validator = validator;
        _logger = logger;
    }
    
    public async Task<TreeNodeDto?> GetNodeAsync(Guid id)
    {
        _logger.LogInformation($"Get node with id: {id}");
        
        var node = await _context.TreeNodes.FindAsync(id);
        
        _logger.LogInformation($"Got node with id: {id}");
        
        return node != null 
            ? TreeNodeToDto(node) 
            : null;
    }
    
    public async Task<List<TreeNodeDto>> GetRootNodesAsync()
    {
        _logger.LogInformation("Get Root Nodes");
        
        var nodes = await _context.TreeNodes
            .Where(n => n.ParentId == null)
            .OrderBy(n => n.Name)
            .ToListAsync();
        
        _logger.LogInformation($"Got {nodes.Count} nodes");
        
        return nodes.Select(TreeNodeToDto).ToList();
    }
    
    public async Task<TreeNodeDto> CreateNodeAsync(CreateTreeNodeRequest request)
    {
        _logger.LogInformation($"Create node with name: {request.Name}");
        
        await using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var node = new TreeNode
            {
                Name = request.Name,
                Description = request.Description,
                ParentId = request.ParentId
            };
            
            if (request.ParentId.HasValue)
            {
                await _validator.ValidateNoCycleAsync(node);
            }
            
            await _context.TreeNodes.AddAsync(node);
            await _context.SaveChangesAsync();
            
            await UpdateNodePathAsync(node);
            
            await transaction.CommitAsync();
            
            _logger.LogInformation($"Created node with id: {node.Id}");
            
            return TreeNodeToDto(node);
        }
        catch
        {
            await transaction.RollbackAsync();
            
            _logger.LogError("Failed to create node");
            
            throw;
        }
    }
    
    public async Task<TreeNodeDto?> UpdateNodeAsync(Guid id, UpdateTreeNodeRequest request)
    {
        _logger.LogInformation($"Update node with id: {id}");
        
        await using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var node = await _context.TreeNodes.FindAsync(id);
            if (node == null) return null;
            
            var oldPath = node.Path;
            
            if (request.Name != null) node.Name = request.Name;
            if (request.Description != null) node.Description = request.Description;
            
            if (request.ParentId != node.ParentId)
            {
                if (request.ParentId.HasValue)
                {
                    var tempNode = new TreeNode { Id = node.Id, ParentId = request.ParentId };
                    await _validator.ValidateNoCycleAsync(tempNode);
                }
                
                node.ParentId = request.ParentId;
                
                await UpdateNodePathAsync(node);
                
                if (!string.IsNullOrEmpty(oldPath))
                {
                    var descendants = await _context.TreeNodes
                        .Where(n => n.Path.StartsWith(oldPath + "/"))
                        .ToListAsync();
                    
                    foreach (var descendant in descendants)
                    {
                        await UpdateNodePathAsync(descendant);
                    }
                }
            }
            
            node.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            await transaction.CommitAsync();
            
            _logger.LogInformation($"Updated node with id: {node.Id}");
            
            return TreeNodeToDto(node);
        }
        catch
        {
            await transaction.RollbackAsync();
            
            _logger.LogError("Failed to update node");
            
            throw;
        }
    }
    
    public async Task<bool> DeleteNodeAsync(Guid id)
    {
        _logger.LogInformation($"Delete node with id: {id}");
        
        await using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var node = await _context.TreeNodes.FindAsync(id);
            if (node == null) return false;
            
            var descendants = await _context.TreeNodes
                .Where(n => n.Path.StartsWith(node.Path + "/"))
                .Select(n => n.Id)
                .ToListAsync();
            
            _context.TreeNodes.RemoveRange(
                _context.TreeNodes.Where(n => n.Id == id || descendants.Contains(n.Id))
            );
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogInformation($"Deleted node with id: {node.Id}");
            
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            
            _logger.LogError("Failed to delete node");
            
            throw;
        }
    }
    
    public async Task<TreeNodeDto> GetTreeAsync(Guid? rootId = null)
    {
        _logger.LogInformation($"Get tree node with id: {rootId}");
        
        List<TreeNode> nodes;
        
        if (rootId.HasValue)
        {
            var root = await _context.TreeNodes.FindAsync(rootId.Value);
            if (root == null)
            {
                throw new ArgumentException("Root node not found");
            }
            
            nodes = await _context.TreeNodes
                .Where(n => n.Path.StartsWith(root.Path))
                .OrderBy(n => n.Path)
                .ToListAsync();
        }
        else
        {
            nodes = await _context.TreeNodes
                .OrderBy(n => n.Path)
                .ToListAsync();
        }
        
        _logger.LogInformation($"Got {nodes.Count} nodes");
        
        return BuildTree(nodes, rootId);
    }
    
    public async Task<List<TreeNodeDto>> GetSubtreeAsync(Guid nodeId)
    {
        _logger.LogInformation($"Get subtree node with id: {nodeId}");
        
        var node = await _context.TreeNodes.FindAsync(nodeId);
        if (node == null)
        {
            return [];
        }
        
        var nodes = await _context.TreeNodes
            .Where(n => n.Path.StartsWith(node.Path))
            .OrderBy(n => n.Path)
            .ToListAsync();
        
        var tree = BuildTree(nodes, nodeId);
        
        _logger.LogInformation($"Got {nodes.Count} nodes");
        
        return tree.Children;
    }
    
    private async Task UpdateNodePathAsync(TreeNode node)
    {
        var pathParts = new List<string>();
        
        if (node.ParentId.HasValue)
        {
            var parent = await _context.TreeNodes.FindAsync(node.ParentId.Value);
            if (parent != null)
            {
                pathParts.Add(parent.Path);
            }
        }
        
        pathParts.Add(node.Id.ToString());
        node.Path = string.Join("/", pathParts.Where(p => !string.IsNullOrEmpty(p)));
        
        await _context.SaveChangesAsync();
    }
    
    private TreeNodeDto BuildTree(List<TreeNode> nodes, Guid? rootId = null)
    {
        var nodeDict = nodes.ToDictionary(n => n.Id);
        var rootNodes = nodes.Where(n => 
            rootId.HasValue ? n.Id == rootId.Value : n.ParentId == null
        ).ToList();
        
        var rootDto = new TreeNodeDto { Id = Guid.Empty, Name = "Root", Children = new List<TreeNodeDto>() };
        
        foreach (var node in rootNodes)
        {
            rootDto.Children.Add(BuildTreeNode(node, nodeDict));
        }
        
        return rootDto;
    }
    
    private TreeNodeDto BuildTreeNode(TreeNode node, Dictionary<Guid, TreeNode> nodeDict)
    {
        var dto = TreeNodeToDto(node);
        
        foreach (var child in nodeDict.Values.Where(n => n.ParentId == node.Id))
        {
            dto.Children.Add(BuildTreeNode(child, nodeDict));
        }
        
        return dto;
    }

    private TreeNodeDto TreeNodeToDto(TreeNode node)
    {
        return new TreeNodeDto
        {
            Id = node.Id,
            Name = node.Name,
            Description = node.Description,
            ParentId = node.ParentId,
            CreatedAt = node.CreatedAt,
            UpdatedAt = node.UpdatedAt
        };
    }
}