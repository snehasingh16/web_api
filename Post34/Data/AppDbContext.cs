using Microsoft.EntityFrameworkCore;
using Post34.Models;

namespace Post34.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ProjectPermission> ProjectPermissions => Set<ProjectPermission>();
}