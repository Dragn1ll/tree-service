namespace TreeService.Contracts.TreeNode.Requests;

public record UpdateTreeNodeRequest(
    string? Name, 
    string? Description, 
    int? ParentId
    );