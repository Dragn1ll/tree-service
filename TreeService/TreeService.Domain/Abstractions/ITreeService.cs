using TreeService.Contracts.Dto;
using TreeService.Contracts.TreeNode.Requests;

namespace TreeService.Domain.Abstractions;

public interface ITreeService
{
    Task<TreeNodeDto?> GetNodeAsync(Guid id);
    Task<List<TreeNodeDto>> GetRootNodesAsync();
    Task<TreeNodeDto> CreateNodeAsync(CreateTreeNodeRequest request);
    Task<TreeNodeDto?> UpdateNodeAsync(Guid id, UpdateTreeNodeRequest request);
    Task<bool> DeleteNodeAsync(Guid id);
    Task<TreeNodeDto> GetTreeAsync(Guid? rootId = null);
    Task<List<TreeNodeDto>> GetSubtreeAsync(Guid nodeId);
}