using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

// Maps the entity classes onto the tables created by docs/RaceDay-Database.sql.
// Keys, constraints, defaults and delete rules all match that script.
public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<OrganiserDetail> OrganiserDetails => Set<OrganiserDetail>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Users
        b.Entity<User>(e =>
        {
            e.Property(x => x.FullName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Email).IsRequired().HasMaxLength(150);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(255);
            e.Property(x => x.Role).IsRequired().HasMaxLength(20);
            e.Property(x => x.DateJoined).HasDefaultValueSql("GETDATE()");
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "Role IN ('Organiser', 'Participant')"));
            e.HasOne(x => x.OrganiserDetail).WithOne(x => x.User)
             .HasForeignKey<OrganiserDetail>(x => x.OrganiserID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // OrganiserDetails - the PK is also the FK to Users, so no auto-increment
        b.Entity<OrganiserDetail>(e =>
        {
            e.HasKey(x => x.OrganiserID);
            e.Property(x => x.OrganiserID).ValueGeneratedNever();
            e.Property(x => x.ClubName).HasMaxLength(100);
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.Bio).HasMaxLength(500);
        });

        // Events
        b.Entity<Event>(e =>
        {
            e.Property(x => x.EventName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Location).IsRequired().HasMaxLength(200);
            e.Property(x => x.Distance).IsRequired().HasMaxLength(50);
            e.Property(x => x.EventType).IsRequired().HasMaxLength(20);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.ToTable("Events", t => t.HasCheckConstraint("CK_Events_EventType", "EventType IN ('Run', 'Walk', 'Cycle')"));
            // NO ACTION in the SQL script: deleting an organiser keeps their events
            e.HasOne(x => x.Organiser).WithMany(x => x.Events)
             .HasForeignKey(x => x.OrganiserID)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // Categories
        b.Entity<Category>(e =>
        {
            e.Property(x => x.CategoryName).IsRequired().HasMaxLength(100);
            e.Property(x => x.MaxParticipants).HasDefaultValue(0);
            e.Property(x => x.EntryFee).HasColumnType("decimal(10,2)").HasDefaultValue(0m);
            e.ToTable("Categories", t => t.HasCheckConstraint("CK_MaxParticipants", "MaxParticipants >= 0"));
            e.HasOne(x => x.Event).WithMany(x => x.Categories)
             .HasForeignKey(x => x.EventID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Enrolments - a participant can only enter a category once
        b.Entity<Enrolment>(e =>
        {
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID }).IsUnique();
            e.Property(x => x.EnrolmentDate).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Participant).WithMany(x => x.Enrolments)
             .HasForeignKey(x => x.ParticipantID)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Category).WithMany(x => x.Enrolments)
             .HasForeignKey(x => x.CategoryID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Results - one result per enrolment
        b.Entity<Result>(e =>
        {
            e.HasIndex(x => x.EnrolmentID).IsUnique();
            e.Property(x => x.FinishTime).HasColumnType("time(7)");
            e.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Finished");
            e.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "Status IN ('Finished', 'DNF', 'Disqualified')"));
            e.HasOne(x => x.Enrolment).WithOne(x => x.Result)
             .HasForeignKey<Result>(x => x.EnrolmentID)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
