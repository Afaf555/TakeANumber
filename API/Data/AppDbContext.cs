using API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace API.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Counter> Counters { get; set; }
    public DbSet<CounterService> CounterServices { get; set; }
    public DbSet<Ticket> Tickets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CounterService>()
            .HasKey(x => new { x.CounterId, x.ServiceId });

        modelBuilder.Entity<CounterService>()
            .HasOne(x => x.Counter)
            .WithMany(c => c.CounterServices)
            .HasForeignKey(x => x.CounterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CounterService>()
            .HasOne(x => x.Service)
            .WithMany(s => s.CounterServices)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Ticket>()
            .HasOne(x => x.Client)
            .WithMany(u => u.Tickets)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(x => x.Counter)
            .WithMany(c => c.Tickets)
            .HasForeignKey(x => x.CounterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(x => x.Service)
            .WithMany(s => s.Tickets)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(x => x.Counter)
            .WithMany(c => c.Employees)
            .HasForeignKey(x => x.CounterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique();

        var dateTimeConverter = new ValueConverter<DateTime, DateTime>
        (
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
        );

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>
        (
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v
        );

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }
}