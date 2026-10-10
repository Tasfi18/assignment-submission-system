using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Application.Common.Interfaces;

/// <summary>
/// The persistence surface the application services are written against. Keeping this abstraction in
/// the Application layer means the services can be unit tested against the EF Core in-memory provider
/// without any knowledge of PostgreSQL.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<ClassRoom> ClassRooms { get; }

    DbSet<Subject> Subjects { get; }

    DbSet<ClassSubjectTeacher> ClassSubjectTeachers { get; }

    DbSet<Assignment> Assignments { get; }

    DbSet<Submission> Submissions { get; }

    DbSet<AppSetting> AppSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
