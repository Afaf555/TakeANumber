using API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace API.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Counter> Counters { get; set; }
    public DbSet<CounterService> CounterServices { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<EmployeeSession> EmployeeSessions { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<NonWorkingDay> NonWorkingDays { get; set; }
    public DbSet<Leave> Leaves {get; set;}
   // public DbSet<SessionBreak> SessionBreaks { get; set; }
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

        modelBuilder.Entity<EmployeeSession>()
        .HasOne(x => x.User)
        .WithMany()
        .HasForeignKey(x => x.UserId)
        .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ActivityLog>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(50);
            e.Property(x => x.EntityType).HasMaxLength(50);
            e.Property(x => x.Details).HasMaxLength(500);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.Action);
            e.HasIndex(x=> new { x.ActorId,x.CreatedAt});
        });
        modelBuilder.Entity<Service>(s =>
        {
            s.Property(x => x.Code).HasMaxLength(5);
            s.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<EmployeeSession>(e =>
        {
            e.Property(x => x.CounterName).HasMaxLength(100);
            e.Property(x => x.Services).HasMaxLength(500);
            e.HasIndex(x=>new {x.UserId,x.LoginAt});
            e.HasIndex(x=>new {x.CounterId,x.LoginAt});
        });
        modelBuilder.Entity<Ticket>(e=>{

            e.HasIndex(x=>new{x.CounterId,x.StartedAt});
            e.HasIndex(x=>new {x.ServiceId,x.CreatedAt});

        });
        modelBuilder.Entity<NonWorkingDay>().Property(x => x.Date).HasColumnType("date");
        modelBuilder.Entity<NonWorkingDay>().HasIndex(x => x.Date).IsUnique();

        modelBuilder.Entity<Leave>(e =>
        {
           e.Property(x=>x.StartDate).HasColumnType("date");
           e.Property(x=>x.EndDate).HasColumnType("date");
           e.Property(x=>x.Type).HasConversion<string>(); 
           e.HasIndex(x=>new {x.UserId,x.StartDate});
           e.HasOne(x=>x.User)
           .WithMany()
           .HasForeignKey(x=>x.UserId)
           .OnDelete(DeleteBehavior.Cascade);
        });
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
            if (entityType.ClrType == typeof(NonWorkingDay)) continue;

            foreach (var property in entityType.GetProperties())
            {
                // StartDate/EndDate na Leave se datumi (kolona "date"), ne momenti vo vreme:
                // UTC konverzijata gi pomestuva za eden den, pa gi preskoknuvame.
                if (entityType.ClrType == typeof(Leave)
                    && (property.Name is nameof(Leave.StartDate) or nameof(Leave.EndDate)))
                    continue;

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