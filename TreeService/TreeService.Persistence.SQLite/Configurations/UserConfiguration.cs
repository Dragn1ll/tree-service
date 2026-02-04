using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TreeService.Persistence.SQLite.Entities;

namespace TreeService.Persistence.SQLite.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> modelBuilder)
    {
        modelBuilder.HasIndex(u => u.Username)
            .IsUnique();
    }
}