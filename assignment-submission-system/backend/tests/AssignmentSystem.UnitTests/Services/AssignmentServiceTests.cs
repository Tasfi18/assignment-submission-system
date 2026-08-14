using AssignmentSystem.Application.Common.Exceptions;
using AssignmentSystem.Application.Contracts.Assignments;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.UnitTests.TestKit;

namespace AssignmentSystem.UnitTests.Services;

/// <summary>
/// Covers the two things that matter most for assignments: who is allowed to see them, and the
/// draft -> published -> closed lifecycle.
/// </summary>
public class AssignmentServiceTests
{
    // ------------------------------------------------------------- visibility

    [Fact]
    public async Task Student_sees_only_published_or_closed_assignments_for_their_own_class()
    {
        using var context = new TestContext();

        var published = context.GivenAssignment(AssignmentStatus.Published);
        var closed = context.GivenAssignment(AssignmentStatus.Closed);
        context.GivenAssignment(AssignmentStatus.Draft);
        context.GivenAssignment(AssignmentStatus.Published, classRoomId: context.ClassNineB.Id, teacherId: context.TeacherTwo.Id);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Assignments(context);

        var result = await service.GetAsync(new AssignmentQuery());

        result.TotalCount.Should().Be(2);
        result.Items.Select(a => a.Id).Should().BeEquivalentTo(new[] { published.Id, closed.Id });
    }

    [Fact]
    public async Task Student_cannot_open_a_draft_and_is_not_told_it_exists()
    {
        using var context = new TestContext();

        var draft = context.GivenAssignment(AssignmentStatus.Draft);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Assignments(context);

        // A 404 rather than a 403: an unpublished assignment must not leak its existence.
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(draft.Id));
    }

    [Fact]
    public async Task Student_cannot_open_an_assignment_belonging_to_another_class()
    {
        using var context = new TestContext();

        var otherClassAssignment = context.GivenAssignment(
            AssignmentStatus.Published, classRoomId: context.ClassNineB.Id, teacherId: context.TeacherTwo.Id);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Assignments(context);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(otherClassAssignment.Id));
    }

    [Fact]
    public async Task Teacher_sees_only_the_assignments_they_created()
    {
        using var context = new TestContext();

        var mine = context.GivenAssignment(AssignmentStatus.Published);
        context.GivenAssignment(AssignmentStatus.Published, classRoomId: context.ClassNineB.Id, teacherId: context.TeacherTwo.Id);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        var result = await service.GetAsync(new AssignmentQuery());

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(mine.Id);
    }

    [Fact]
    public async Task Teacher_cannot_open_an_assignment_owned_by_another_teacher()
    {
        using var context = new TestContext();

        var theirs = context.GivenAssignment(
            AssignmentStatus.Published, classRoomId: context.ClassNineB.Id, teacherId: context.TeacherTwo.Id);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(theirs.Id));
    }

    [Fact]
    public async Task Admin_sees_every_assignment_including_drafts()
    {
        using var context = new TestContext();

        context.GivenAssignment(AssignmentStatus.Published);
        context.GivenAssignment(AssignmentStatus.Draft);
        context.GivenAssignment(AssignmentStatus.Published, classRoomId: context.ClassNineB.Id, teacherId: context.TeacherTwo.Id);

        context.CurrentUser.SignInAs(context.Admin);
        var service = ServiceFactory.Assignments(context);

        var result = await service.GetAsync(new AssignmentQuery());

        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Student_assignment_list_includes_their_own_submission()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        context.GivenSubmission(assignment, context.StudentOne, SubmissionStatus.Graded, marks: 80m);
        context.GivenSubmission(assignment, context.StudentTwo);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Assignments(context);

        var result = await service.GetAsync(new AssignmentQuery());

        var item = result.Items.Single();
        item.MySubmission.Should().NotBeNull();
        item.MySubmission!.Marks.Should().Be(80m);
        item.MySubmission.StatusName.Should().Be("Graded");
    }

    // ---------------------------------------------------------------- create

    [Fact]
    public async Task Teacher_cannot_create_an_assignment_for_a_class_subject_they_are_not_assigned_to()
    {
        using var context = new TestContext();

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        // Teacher One teaches Maths and Physics in 10-A, but not Chemistry.
        var request = NewCreateRequest(context, subjectId: context.Chemistry.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task New_assignment_is_a_draft_unless_immediate_publication_was_requested()
    {
        using var context = new TestContext();

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        var created = await service.CreateAsync(NewCreateRequest(context));

        created.Status.Should().Be(AssignmentStatus.Draft);
        created.PublishedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Assignment_can_be_published_at_creation_time()
    {
        using var context = new TestContext();

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        var created = await service.CreateAsync(NewCreateRequest(context) with { PublishImmediately = true });

        created.Status.Should().Be(AssignmentStatus.Published);
        created.PublishedAtUtc.Should().Be(context.Clock.UtcNow);
    }

    [Fact]
    public async Task Assignment_cannot_be_published_at_creation_time_with_a_deadline_in_the_past()
    {
        using var context = new TestContext();

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        var request = NewCreateRequest(context) with
        {
            PublishImmediately = true,
            DeadlineUtc = context.Clock.UtcNow.AddDays(-1)
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
    }

    // --------------------------------------------------------------- publish

    [Fact]
    public async Task Publishing_makes_a_draft_visible_and_stamps_the_publication_time()
    {
        using var context = new TestContext();

        var draft = context.GivenAssignment(AssignmentStatus.Draft);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        var published = await service.PublishAsync(draft.Id);

        published.Status.Should().Be(AssignmentStatus.Published);
        published.PublishedAtUtc.Should().Be(context.Clock.UtcNow);
    }

    [Fact]
    public async Task Only_a_draft_can_be_published()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.PublishAsync(assignment.Id));
    }

    [Fact]
    public async Task An_expired_draft_cannot_be_published()
    {
        using var context = new TestContext();

        var draft = context.GivenAssignment(AssignmentStatus.Draft, deadlineUtc: context.Clock.UtcNow.AddDays(-1));

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.PublishAsync(draft.Id));
    }

    [Fact]
    public async Task An_assignment_cannot_be_published_to_a_class_without_students()
    {
        using var context = new TestContext();

        var emptyClass = new ClassRoom
        {
            Id = Guid.NewGuid(), Name = "Class 11 - C", Code = "C11C", AcademicYear = 2026, CreatedAtUtc = context.Clock.UtcNow
        };

        context.Db.ClassRooms.Add(emptyClass);
        context.Db.ClassSubjectTeachers.Add(new ClassSubjectTeacher
        {
            Id = Guid.NewGuid(),
            ClassRoomId = emptyClass.Id,
            SubjectId = context.Mathematics.Id,
            TeacherId = context.TeacherOne.Id,
            CreatedAtUtc = context.Clock.UtcNow
        });
        await context.Db.SaveChangesAsync();

        var draft = context.GivenAssignment(AssignmentStatus.Draft, classRoomId: emptyClass.Id);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.PublishAsync(draft.Id));
    }

    [Fact]
    public async Task Only_a_published_assignment_can_be_closed()
    {
        using var context = new TestContext();

        var draft = context.GivenAssignment(AssignmentStatus.Draft);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CloseAsync(draft.Id));
    }

    // ---------------------------------------------------------------- update

    [Fact]
    public async Task A_teacher_cannot_modify_an_assignment_owned_by_someone_else()
    {
        using var context = new TestContext();

        var theirs = context.GivenAssignment(
            AssignmentStatus.Published, classRoomId: context.ClassNineB.Id, subjectId: context.Mathematics.Id, teacherId: context.TeacherTwo.Id);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(theirs.Id, NewUpdateRequest(context)));
    }

    [Fact]
    public async Task The_target_class_cannot_be_changed_once_students_have_submitted()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        context.GivenSubmission(assignment, context.StudentOne);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        var request = NewUpdateRequest(context) with { SubjectId = context.Physics.Id };

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(assignment.Id, request));
    }

    [Fact]
    public async Task Maximum_marks_cannot_drop_below_marks_that_were_already_awarded()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published, maxMarks: 100m);
        context.GivenSubmission(assignment, context.StudentOne, SubmissionStatus.Graded, marks: 82m);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        var request = NewUpdateRequest(context) with { MaxMarks = 50m };

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(assignment.Id, request));
    }

    [Fact]
    public async Task A_closed_assignment_can_no_longer_be_edited()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Closed);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(assignment.Id, NewUpdateRequest(context)));
    }

    [Fact]
    public async Task An_editable_assignment_is_updated()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        var request = NewUpdateRequest(context) with { Title = "Revised title", MaxMarks = 75m };

        var updated = await service.UpdateAsync(assignment.Id, request);

        updated.Title.Should().Be("Revised title");
        updated.MaxMarks.Should().Be(75m);
    }

    // ---------------------------------------------------------------- delete

    [Fact]
    public async Task An_assignment_with_submissions_cannot_be_deleted()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        context.GivenSubmission(assignment, context.StudentOne);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeleteAsync(assignment.Id));
    }

    [Fact]
    public async Task An_assignment_without_submissions_is_deleted()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Draft);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Assignments(context);

        await service.DeleteAsync(assignment.Id);

        context.Db.Assignments.Any(a => a.Id == assignment.Id).Should().BeFalse();
    }

    // --------------------------------------------------------------- helpers

    private static CreateAssignmentRequest NewCreateRequest(TestContext context, Guid? subjectId = null) => new()
    {
        Title = "Algebra worksheet",
        Description = "Complete every exercise in chapter two.",
        ClassRoomId = context.ClassTenA.Id,
        SubjectId = subjectId ?? context.Mathematics.Id,
        MaxMarks = 100m,
        DeadlineUtc = context.Clock.UtcNow.AddDays(5),
        AllowLateSubmission = false,
        AllowResubmission = true
    };

    private static UpdateAssignmentRequest NewUpdateRequest(TestContext context) => new()
    {
        Title = "Algebra worksheet",
        Description = "Complete every exercise in chapter two.",
        ClassRoomId = context.ClassTenA.Id,
        SubjectId = context.Mathematics.Id,
        MaxMarks = 100m,
        DeadlineUtc = context.Clock.UtcNow.AddDays(5),
        AllowLateSubmission = false,
        AllowResubmission = true
    };
}
