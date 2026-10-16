using RaceDay_api.Models;
using Microsoft.EntityFrameworkCore;


namespace RaceDay_api.Data;
//start of namespace
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<RaceRoute> Routes => Set<RaceRoute>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("User");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("User_ID");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Contact).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Gender).HasMaxLength(20).IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("Password_Hash").HasMaxLength(255).IsRequired();
        });

        b.Entity<Organiser>(e =>
        {
            e.ToTable("Organiser");
            e.HasKey(x => x.OrganiserId);
            e.Property(x => x.OrganiserId).HasColumnName("Organiser_ID");
            e.Property(x => x.UserId).HasColumnName("User_ID");
            e.Property(x => x.OrganiserName).HasColumnName("Organiser_name").HasMaxLength(200).IsRequired();
            e.Property(x => x.ClubName).HasColumnName("Club_name").HasMaxLength(200).IsRequired();
            // 1:1 with User
            e.HasOne(x => x.User).WithOne(u => u.Organiser)
                .HasForeignKey<Organiser>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Participant>(e =>
        {
            e.ToTable("Participant");
            e.HasKey(x => x.ParticipantId);
            e.Property(x => x.ParticipantId).HasColumnName("Participant_ID");
            e.Property(x => x.UserId).HasColumnName("User_ID");
            e.Property(x => x.MedicalInfo).HasColumnName("Medical_info").HasMaxLength(255);
            // 1:1 with User
            e.HasOne(x => x.User).WithOne(u => u.Participant)
                .HasForeignKey<Participant>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Event>(e =>
        {
            e.ToTable("Event");
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventId).HasColumnName("Event_ID");
            e.Property(x => x.OrganiserId).HasColumnName("Organiser_ID");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.EventType).HasColumnName("Event_type").HasMaxLength(10).IsRequired();
            e.Property(x => x.Location).HasMaxLength(200).IsRequired();
            e.Property(x => x.Date).HasColumnType("date");
            e.Property(x => x.Time).HasColumnType("time");
            // Organiser 1:M Event
            e.HasOne(x => x.Organiser).WithMany(o => o.Events)
                .HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RaceRoute>(e =>
        {
            e.ToTable("Route");
            e.HasKey(x => x.RouteId);
            e.Property(x => x.RouteId).HasColumnName("Route_ID");
            e.Property(x => x.EventId).HasColumnName("Event_ID");
            e.Property(x => x.StartingPoint).HasColumnName("Starting_Point").HasMaxLength(200).IsRequired();
            e.Property(x => x.EndingPoint).HasColumnName("Ending_Point").HasMaxLength(200).IsRequired();
            e.Property(x => x.Distance).HasColumnType("decimal(6,2)");
            // Event 1:M Route
            e.HasOne(x => x.Event).WithMany(ev => ev.Routes)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Category");
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.CategoryId).HasColumnName("Category_ID");
            e.Property(x => x.EventId).HasColumnName("Event_ID");
            e.Property(x => x.CategoryName).HasColumnName("Category_name").HasMaxLength(100).IsRequired();
            e.Property(x => x.EntryFee).HasColumnName("Entry_fee").HasColumnType("decimal(8,2)").HasDefaultValue(0m);
            e.Property(x => x.MaxParticipants).HasColumnName("Max_participants");
            // Event 1:M Category
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Registration>(e =>
        {
            e.ToTable("Registration");
            e.HasKey(x => x.RegistrationId);
            e.Property(x => x.RegistrationId).HasColumnName("Registration_ID");
            e.Property(x => x.CategoryId).HasColumnName("Category_ID");
            e.Property(x => x.ParticipantId).HasColumnName("Participant_ID");
            e.Property(x => x.RaceNum).HasColumnName("Race_num").HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.RaceNum).IsUnique();
            e.Property(x => x.RegistrationDate).HasColumnName("Registration_date").HasColumnType("date");

            // Category 1:M Registration
            e.HasOne(x => x.Category).WithMany(c => c.Registrations)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);

            // Participant 1:M Registration.
            // NO ACTION (not CASCADE): SQL Server rejects two cascade paths from User to Registration
            // (User->Participant->Registration and User->Organiser->Event->Category->Registration).
            // The API removes a participant's registrations explicitly when the account is deleted.
            e.HasOne(x => x.Participant).WithMany(p => p.Registration)
                .HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.ResultId);
            e.Property(x => x.ResultId).HasColumnName("Results_ID");
            e.Property(x => x.RegistrationId).HasColumnName("Registration_ID");
            e.Property(x => x.FinishTime).HasColumnName("Finish_time").HasColumnType("time");
            // Registration 1:1 Results
            e.HasOne(x => x.Registration).WithOne(r => r.Result)
                .HasForeignKey<Result>(x => x.RegistrationId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}



