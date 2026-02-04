using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TreeService.Persistence.SQLite.Entities;

namespace TreeService.Persistence.SQLite.Configurations;

public class TreeNodeConfiguration : IEntityTypeConfiguration<TreeNode>
{
    public void Configure(EntityTypeBuilder<TreeNode> modelBuilder)
    {
        modelBuilder.HasOne(t => t.Parent)
            .WithMany(t => t.Children)
            .HasForeignKey(t => t.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.HasIndex(t => t.ParentId);
        
        modelBuilder.HasIndex(t => t.Path);
        
        modelBuilder.Property(t => t.Path)
            .HasMaxLength(1000);
    }
}