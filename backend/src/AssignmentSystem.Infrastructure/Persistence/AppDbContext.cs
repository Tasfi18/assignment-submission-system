using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<ClassRoom> ClassRooms => Set<ClassRoom>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<ClassSubjectTeacher> ClassSubjectTeachers => Set<ClassSubjectTeacher>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<Submission> Submissions => Set<Submission>();

    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
