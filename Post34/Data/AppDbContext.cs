using Microsoft.EntityFrameworkCore;
using Post34.Models;

namespace Post34.Data;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Post34.Models;
using Post34.DTOs;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectPermission> ProjectPermissions => Set<ProjectPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ✅ Configure used_services_list to store as JSON
       modelBuilder.Entity<Project>()
                    .Property(p => p.used_services_list)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v),
                        v => JsonSerializer.Deserialize<List<ServiceItem>>(v) ?? new());
    }
}