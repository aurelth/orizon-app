using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Orizon.Domain.Entities;
using Orizon.Infrastructure.Identity;

namespace Orizon.Infrastructure.Data;

public class OrizonDbContext : IdentityDbContext<AppIdentityUser>
{
    public OrizonDbContext(DbContextOptions<OrizonDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<BriefingEntry> BriefingEntries => Set<BriefingEntry>();
    public DbSet<TrelloBoardConfig> TrelloBoardConfigs => Set<TrelloBoardConfig>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {      
        base.OnModelCreating(builder);
     
        builder.ApplyConfigurationsFromAssembly(
            typeof(OrizonDbContext).Assembly);
        
        builder.Entity<AppIdentityUser>().ToTable("users");
    }
}