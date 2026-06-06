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
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<UserTransfer> UserTransfers => Set<UserTransfer>();
    public DbSet<UserDirectSupervisor> UserDirectSupervisors => Set<UserDirectSupervisor>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<Weekend> Weekends => Set<Weekend>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Division> Division => Set<Division>();
    public DbSet<District> District => Set<District>();
    public DbSet<Thana> Thana => Set<Thana>();
    public DbSet<UserDataViewLevel> UserDataViewLevels => Set<UserDataViewLevel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable(t => t.HasCheckConstraint("CK_Users_MobileNumber_BD", "[MobileNumber] LIKE '01[3-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'"));
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            entity.HasIndex(e => e.LoginID).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.LoginID).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.MobileNumber).HasMaxLength(11).IsRequired();

            entity.HasOne(e => e.Institute)
                .WithMany(i => i.Users)
                .HasForeignKey(e => e.InstituteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Grade)
                .WithMany(g => g.Users)
                .HasForeignKey(e => e.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Designation)
                .WithMany(d => d.Users)
                .HasForeignKey(e => e.DesignationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Institute>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.InstituteNameEN).HasMaxLength(255).IsRequired();
            entity.Property(e => e.InstituteNameBN).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.LatitudeLongitude).HasMaxLength(100);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255);

            entity.HasOne(e => e.UserDataViewLevel)
                .WithMany(v => v.Roles)
                .HasForeignKey(e => e.UserDataViewLevelID)
                .OnDelete(DeleteBehavior.Restrict);
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
            entity.Property(e => e.CheckInLatitudeLongitude).HasMaxLength(100);
            entity.Property(e => e.CheckOutLatitudeLongitude).HasMaxLength(100);

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

            entity.HasOne(e => e.LeaveTypeRecord)
                .WithMany(t => t.LeaveRequests)
                .HasForeignKey(e => e.LeaveTypeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.SupervisorUser)
                .WithMany(u => u.SupervisedLeaveRequests)
                .HasForeignKey(e => e.SupervisorUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ApprovedBy)
                .WithMany(u => u.ApprovedLeaveRequests)
                .HasForeignKey(e => e.ApprovedById)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.HasIndex(e => e.Name).IsUnique();

            entity.HasData(
                new LeaveType { Id = 1, Name = "Casual Leave", IsActive = true, CreatedAt = new DateTime(2026, 6, 6) },
                new LeaveType { Id = 2, Name = "Sick Leave", IsActive = true, CreatedAt = new DateTime(2026, 6, 6) },
                new LeaveType { Id = 3, Name = "Earned Leave", IsActive = true, CreatedAt = new DateTime(2026, 6, 6) },
                new LeaveType { Id = 4, Name = "Maternity Leave", IsActive = true, CreatedAt = new DateTime(2026, 6, 6) },
                new LeaveType { Id = 5, Name = "Paternity Leave", IsActive = true, CreatedAt = new DateTime(2026, 6, 6) },
                new LeaveType { Id = 6, Name = "Without Pay", IsActive = true, CreatedAt = new DateTime(2026, 6, 6) },
                new LeaveType { Id = 7, Name = "Other", IsActive = true, CreatedAt = new DateTime(2026, 6, 6) },
                new LeaveType { Id = 8, Name = "Medical Leave", IsActive = true, CreatedAt = new DateTime(2026, 6, 6) },
                new LeaveType { Id = 9, Name = "Study Leave", IsActive = true, CreatedAt = new DateTime(2026, 6, 6) },
                new LeaveType { Id = 10, Name = "Leave Without Pay", IsActive = true, CreatedAt = new DateTime(2026, 6, 6) });
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

        modelBuilder.Entity<UserDirectSupervisor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserID).IsUnique();
            entity.Property(e => e.ActiveDateFrom).HasColumnType("date").IsRequired();
            entity.Property(e => e.ExpireDate).HasColumnType("date");
            entity.Property(e => e.CreateDate);

            entity.HasOne(e => e.User)
                .WithMany(u => u.DirectSupervisorRecords)
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.SupervisorUser)
                .WithMany(u => u.SupervisorForUsers)
                .HasForeignKey(e => e.Supervisor_UserID)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.CreateByUser)
                .WithMany(u => u.CreatedDirectSupervisorRecords)
                .HasForeignKey(e => e.CreateBy_UserID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GradeCode).IsUnique();

            entity.Property(e => e.GradeCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.GradeNameEN)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.GradeNameBN)
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

            entity.Property(e => e.DesignationNameEN)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.DesignationNameBN)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.CreateDate)
                .IsRequired();
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.DepartmentCode).IsUnique();

            entity.Property(e => e.DepartmentCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.DepartmentName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.CreateDate)
                .IsRequired();
        });

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HolidayName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.HolidayDate).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.HolidayDate).IsUnique();
        });

        modelBuilder.Entity<Weekend>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable(t => t.HasCheckConstraint("CK_Weekends_DayOfWeek", "[DayOfWeek] BETWEEN 0 AND 6"));
            entity.Property(e => e.DayOfWeek).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.DayOfWeek).IsUnique();
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShiftCode).IsUnique();
            entity.Property(e => e.ShiftCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ShiftName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.EndTime).IsRequired();
            entity.Property(e => e.LateAfterMinutes).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<UserDataViewLevel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DataViewLevel).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RelatedRoleInfo).HasMaxLength(100).IsRequired();
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "SuperAdmin", Description = "System administrator", UserDataViewLevelID = 1 },
            new Role { Id = 2, Name = "InstituteAdmin", Description = "Institute administrator", UserDataViewLevelID = 5 },
            new Role { Id = 3, Name = "User", Description = "Regular user", UserDataViewLevelID = 6 }
        );

        modelBuilder.Entity<UserDataViewLevel>().HasData(
            new UserDataViewLevel { Id = 1, DataViewLevel = "All Division", RelatedRoleInfo = "SuperAdmin" },
            new UserDataViewLevel { Id = 2, DataViewLevel = "Own Division Only", RelatedRoleInfo = "DivisionalAdmin" },
            new UserDataViewLevel { Id = 3, DataViewLevel = "Own District Only", RelatedRoleInfo = "DisrtictAdmin" },
            new UserDataViewLevel { Id = 4, DataViewLevel = "Own Thana Only", RelatedRoleInfo = "ThanaAdmin" },
            new UserDataViewLevel { Id = 5, DataViewLevel = "Own Institute Only", RelatedRoleInfo = "InstituteAdmin" },
            new UserDataViewLevel { Id = 6, DataViewLevel = "Own Data Only", RelatedRoleInfo = "User" },
            new UserDataViewLevel { Id = 7, DataViewLevel = "Own Departments All", RelatedRoleInfo = "DepartmentalAdmin" }
        );
    }
}
