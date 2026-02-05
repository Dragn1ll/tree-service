using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TreeService.Contracts.Dto;
using TreeService.Contracts.TreeNode.Requests;
using TreeService.Domain.Abstractions;

namespace TreeService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "UserOrAdmin")]
public class TreeController : ControllerBase
{
    private readonly ITreeService _treeService;
    private readonly ILogger<TreeController> _logger;
    
    public TreeController(ITreeService treeService, ILogger<TreeController> logger)
    {
        _treeService = treeService;
        _logger = logger;
    }
    
    [HttpGet("nodes")]
    public async Task<ActionResult<List<TreeNodeDto>>> GetRootNodes()
    {
        _logger.LogInformation("Getting root nodes");
        
        try
        {
            var nodes = await _treeService.GetRootNodesAsync();
            _logger.LogInformation("Retrieved {Count} root nodes", nodes.Count);
            return Ok(nodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root nodes");
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpGet("nodes/{id}")]
    public async Task<ActionResult<TreeNodeDto>> GetNode(Guid id)
    {
        _logger.LogInformation("Getting node {Id}", id);
        
        try
        {
            var node = await _treeService.GetNodeAsync(id);
            
            if (node == null)
            {
                _logger.LogWarning("Node {Id} not found", id);
                return NotFound();
            }
            
            _logger.LogInformation("Node {Id} retrieved", id);
            return Ok(node);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting node {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpPost("nodes")]
    public async Task<ActionResult<TreeNodeDto>> CreateNode(CreateTreeNodeRequest request)
    {
        var username = User.Identity?.Name ?? "anonymous";
        _logger.LogInformation("Creating node: {Name}, ParentId: {ParentId}, User: {User}", 
            request.Name, request.ParentId, username);
        
        try
        {
            var node = await _treeService.CreateNodeAsync(request);
            
            _logger.LogInformation("Node created: {Id} {Name}, User: {User}", 
                node.Id, node.Name, username);
            
            return CreatedAtAction(nameof(GetNode), new { id = node.Id }, node);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("циклическую ссылку"))
        {
            _logger.LogWarning("Cycle detected when creating node: {Name}, User: {User}, Error: {Message}", 
                request.Name, username, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating node: {Name}, User: {User}", 
                request.Name, username);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpPut("nodes/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<TreeNodeDto>> UpdateNode(Guid id, UpdateTreeNodeRequest request)
    {
        var username = User.Identity?.Name ?? "anonymous";
        _logger.LogInformation("Updating node {Id} by admin {User}", id, username);
        
        try
        {
            var node = await _treeService.UpdateNodeAsync(id, request);
            
            if (node == null)
            {
                _logger.LogWarning("Node {Id} not found for update by {User}", id, username);
                return NotFound();
            }
            
            _logger.LogInformation("Node {Id} updated by admin {User}", id, username);
            return Ok(node);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("циклическую ссылку"))
        {
            _logger.LogWarning("Cycle detected when updating node {Id} by {User}: {Message}", 
                id, username, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating node {Id} by admin {User}", id, username);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpDelete("nodes/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> DeleteNode(Guid id)
    {
        var username = User.Identity?.Name ?? "anonymous";
        _logger.LogInformation("Deleting node {Id} by admin {User}", id, username);
        
        try
        {
            var result = await _treeService.DeleteNodeAsync(id);
            
            if (!result)
            {
                _logger.LogWarning("Node {Id} not found for deletion by {User}", id, username);
                return NotFound();
            }
            
            _logger.LogInformation("Node {Id} deleted by admin {User}", id, username);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting node {Id} by admin {User}", id, username);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpGet("tree")]
    public async Task<ActionResult<TreeNodeDto>> GetTree([FromQuery] Guid? rootId)
    {
        _logger.LogInformation("Getting tree, rootId: {RootId}", rootId);
        
        try
        {
            var tree = await _treeService.GetTreeAsync(rootId);
            _logger.LogInformation("Tree retrieved, rootId: {RootId}", rootId);
            return Ok(tree);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid rootId {RootId}: {Message}", rootId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tree, rootId: {RootId}", rootId);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpGet("nodes/{id}/subtree")]
    public async Task<ActionResult<List<TreeNodeDto>>> GetSubtree(Guid id)
    {
        _logger.LogInformation("Getting subtree for node {Id}", id);
        
        try
        {
            var subtree = await _treeService.GetSubtreeAsync(id);
            _logger.LogInformation("Subtree retrieved for node {Id}, count: {Count}", 
                id, subtree.Count);
            return Ok(subtree);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subtree for node {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpGet("export")]
    public async Task<ActionResult> ExportTree([FromQuery] Guid? rootId)
    {
        var username = User.Identity?.Name ?? "anonymous";
        _logger.LogInformation("Exporting tree by {User}, rootId: {RootId}", username, rootId);
        
        try
        {
            var tree = await _treeService.GetTreeAsync(rootId);
            
            var json = System.Text.Json.JsonSerializer.Serialize(
                tree, 
                new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                }
            );
            
            _logger.LogInformation("Tree exported by {User}, rootId: {RootId}", username, rootId);
            return Content(json, "application/json");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Export failed: invalid rootId {RootId} by {User}: {Message}", 
                rootId, username, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tree by {User}, rootId: {RootId}", 
                username, rootId);
            return StatusCode(500, "Internal server error");
        }
    }
}