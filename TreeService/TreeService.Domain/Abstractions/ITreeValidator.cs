using TreeService.Persistence.SQLite.Entities;

namespace TreeService.Domain.Abstractions;

public interface ITreeValidator
{
    Task ValidateNoCycleAsync(TreeNode node);
}