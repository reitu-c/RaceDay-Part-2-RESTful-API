using Microsoft.EntityFrameworkCore;
using RaceDay.Part2.Models;

namespace RaceDay.Part2.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Enrolment> Enrolments => Set<Enrolment>();

    public DbSet<Result> Results => Set<Result>();

    public DbSet<RaceRoute> Routes => Set<RaceRoute>();

    public DbSet<Weather> WeatherRecords => Set<Weather>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map C# classes to the existing SQL Server tables.
        modelBuilder.Entity<User>().ToTable("USER");
        modelBuilder.Entity<Event>().ToTable("EVENT");
        modelBuilder.Entity<Category>().ToTable("CATEGORY");
        modelBuilder.Entity<Enrolment>().ToTable("ENROLMENT");
        modelBuilder.Entity<Result>().ToTable("RESULT");
        modelBuilder.Entity<RaceRoute>().ToTable("ROUTE");
        modelBuilder.Entity<Weather>().ToTable("WEATHER");

        // Configure primary keys.
        modelBuilder.Entity<User>().HasKey(x => x.UserID);
        modelBuilder.Entity<Event>().HasKey(x => x.EventID);
        modelBuilder.Entity<Category>().HasKey(x => x.CategoryID);
        modelBuilder.Entity<Enrolment>().HasKey(x => x.EnrolmentID);
        modelBuilder.Entity<Result>().HasKey(x => x.ResultID);
        modelBuilder.Entity<RaceRoute>().HasKey(x => x.RouteID);
        modelBuilder.Entity<Weather>().HasKey(x => x.WeatherID);

        // Configure identity-generated primary keys.
        modelBuilder.Entity<User>().Property(x => x.UserID).ValueGeneratedOnAdd();
        modelBuilder.Entity<Event>().Property(x => x.EventID).ValueGeneratedOnAdd();
        modelBuilder.Entity<Category>().Property(x => x.CategoryID).ValueGeneratedOnAdd();
        modelBuilder.Entity<Enrolment>().Property(x => x.EnrolmentID).ValueGeneratedOnAdd();
        modelBuilder.Entity<Result>().Property(x => x.ResultID).ValueGeneratedOnAdd();
        modelBuilder.Entity<RaceRoute>().Property(x => x.RouteID).ValueGeneratedOnAdd();
        modelBuilder.Entity<Weather>().Property(x => x.WeatherID).ValueGeneratedOnAdd();

        // Configure column lengths and decimal precision.
        modelBuilder.Entity<User>().Property(x => x.FullName).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<User>().Property(x => x.Email).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<User>().Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        modelBuilder.Entity<User>().Property(x => x.Role).HasMaxLength(20).IsRequired();
        modelBuilder.Entity<User>().Property(x => x.Phone).HasMaxLength(20);
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();

        modelBuilder.Entity<Event>().Property(x => x.EventName).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<Event>().Property(x => x.Location).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<Event>().Property(x => x.Description).HasMaxLength(500);
        modelBuilder.Entity<Event>().Property(x => x.Status).HasMaxLength(20).IsRequired();
        modelBuilder.Entity<Event>().Property(x => x.EventDate).HasColumnType("date");

        modelBuilder.Entity<Category>().Property(x => x.CategoryName).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<Category>().Property(x => x.DistanceKM).HasPrecision(6, 2);
        modelBuilder.Entity<Category>().Property(x => x.EntryFee).HasPrecision(10, 2);

        modelBuilder.Entity<Enrolment>().Property(x => x.EnrolmentDate).HasColumnType("datetime2");
        modelBuilder.Entity<Enrolment>().Property(x => x.EmergencyContact).HasMaxLength(100);
        modelBuilder.Entity<Enrolment>().Property(x => x.Status).HasMaxLength(20).IsRequired();

        modelBuilder.Entity<Result>().Property(x => x.FinishTime).HasColumnType("time");
        modelBuilder.Entity<Result>().Property(x => x.ResultStatus).HasMaxLength(30).IsRequired();

        modelBuilder.Entity<RaceRoute>().Property(x => x.RouteName).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<RaceRoute>().Property(x => x.DistanceKM).HasPrecision(6, 2);
        modelBuilder.Entity<RaceRoute>().Property(x => x.StartPoint).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<RaceRoute>().Property(x => x.FinishPoint).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<RaceRoute>().Property(x => x.RouteURL).HasMaxLength(500);

        modelBuilder.Entity<Weather>().Property(x => x.RecordedAt).HasColumnType("datetime2");
        modelBuilder.Entity<Weather>().Property(x => x.TemperatureC).HasPrecision(5, 2);
        modelBuilder.Entity<Weather>().Property(x => x.WindSpeedKmh).HasPrecision(6, 2);
        modelBuilder.Entity<Weather>().Property(x => x.RainProbability).HasPrecision(5, 2);

        // Configure the existing foreign-key relationships.
        modelBuilder.Entity<Event>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.OrganiserID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => x.EventID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ParticipantID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Result>()
            .HasOne<Enrolment>()
            .WithMany()
            .HasForeignKey(x => x.EnrolmentID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RaceRoute>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => x.EventID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Weather>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => x.EventID)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure existing unique constraints.
        modelBuilder.Entity<Category>()
            .HasIndex(x => new { x.EventID, x.CategoryName })
            .IsUnique();

        modelBuilder.Entity<Enrolment>()
            .HasIndex(x => new { x.ParticipantID, x.CategoryID })
            .IsUnique();

        modelBuilder.Entity<Result>()
            .HasIndex(x => x.EnrolmentID)
            .IsUnique();

        modelBuilder.Entity<RaceRoute>()
            .HasIndex(x => x.EventID)
            .IsUnique();
    }
}

