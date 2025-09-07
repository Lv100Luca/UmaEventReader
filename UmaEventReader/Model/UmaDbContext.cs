namespace UmaEventReader.Model;

using Microsoft.EntityFrameworkCore;

public class UmaContext : DbContext
{
    public DbSet<UmaEvent> Events => Set<UmaEvent>();
    public DbSet<UmaEventChoice> Choices => Set<UmaEventChoice>();
    public DbSet<Outcome> Outcomes => Set<Outcome>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=uma_db;Username=umauser;Password=umapassword");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Outcome>();

        base.OnModelCreating(modelBuilder);
    }
}
