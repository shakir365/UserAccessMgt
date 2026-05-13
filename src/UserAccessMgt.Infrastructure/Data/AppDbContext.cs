using Microsoft.EntityFrameworkCore;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Institute> Institutes => Set<Institute>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<UserTransfer> UserTransfers => Set<UserTransfer>();
    public DbSet<Grade> Grades => Set<Grade>();

    public DbSet<Designation> Designations => Set<Designation>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);

            entity.HasOne(e => e.Institute)
                .WithMany(i => i.Users)
                .HasForeignKey(e => e.InstituteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Institute>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoginHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.FailureReason).HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany(u => u.LoginHistories)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.LoginAt);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Attendances)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Institute)
                .WithMany(i => i.Attendances)
                .HasForeignKey(e => e.InstituteId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.SubmittedByUser)
                .WithMany(u => u.SubmittedAttendances)
                .HasForeignKey(e => e.SubmittedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LeaveType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Comments).HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany(u => u.LeaveRequests)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ApprovedBy)
                .WithMany(u => u.ApprovedLeaveRequests)
                .HasForeignKey(e => e.ApprovedById)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<UserTransfer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserTransfers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.FromInstitute)
                .WithMany(i => i.FromInstituteTransfers)
                .HasForeignKey(e => e.FromInstituteId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ToInstitute)
                .WithMany(i => i.ToInstituteTransfers)
                .HasForeignKey(e => e.ToInstituteId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.TransferredBy)
                .WithMany(u => u.TransferredByRecords)
                .HasForeignKey(e => e.TransferredById)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GradeCode).IsUnique();

            entity.Property(e => e.GradeCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.GradeName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.CreateDate)
                .IsRequired();
        });

        modelBuilder.Entity<Designation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DesignationCode).IsUnique();

            entity.Property(e => e.DesignationCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.DesignationName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.CreateDate)
                .IsRequired();
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "SuperAdmin", Description = "System administrator" },
            new Role { Id = 2, Name = "InstituteAdmin", Description = "Institute administrator" },
            new Role { Id = 3, Name = "User", Description = "Regular user" }
        );
    }
}
