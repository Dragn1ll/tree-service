namespace TreeService.Contracts.TreeNode.Requests;

public record CreateTreeNodeRequest(
    string Name, 
    string? Description, 
    Guid? ParentId
    );