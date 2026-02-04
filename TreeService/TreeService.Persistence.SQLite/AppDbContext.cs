using Microsoft.EntityFrameworkCore;
using TreeService.Persistence.SQLite.Configurations;
using TreeService.Persistence.SQLite.Entities;

namespace TreeService.Persistence.SQLite;

public class AppDbContext :DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<TreeNode> TreeNodes { get; set; }
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
    }
}