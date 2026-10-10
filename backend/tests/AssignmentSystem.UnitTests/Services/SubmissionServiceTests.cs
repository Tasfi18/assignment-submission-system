using AssignmentSystem.Application.Common.Exceptions;
using AssignmentSystem.Application.Contracts.Submissions;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.UnitTests.TestKit;

namespace AssignmentSystem.UnitTests.Services;

/// <summary>
/// Covers the submission workflow end to end: who may submit, what happens around the deadline,
/// when an answer may be replaced, and who is allowed to award marks.
/// </summary>
public class SubmissionServiceTests
{
    // -------------------------------------------------------------- submitting

    [Fact]
    public async Task Student_can_submit_to_a_published_assignment_for_their_own_class()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        var submission = await service.SubmitAsync(assignment.Id, NewSubmission());

        submission.Status.Should().Be(SubmissionStatus.Submitted);
        submission.IsLate.Should().BeFalse();
        submission.StudentName.Should().Be("Student One");
        submission.AssignmentTitle.Should().Be(assignment.Title);
    }

    [Fact]
    public async Task Student_cannot_submit_to_an_unpublished_assignment()
    {
        using var context = new TestContext();

        var draft = context.GivenAssignment(AssignmentStatus.Draft);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.SubmitAsync(draft.Id, NewSubmission()));
    }

    [Fact]
    public async Task Student_cannot_submit_to_a_closed_assignment()
    {
        using var context = new TestContext();

        var closed = context.GivenAssignment(AssignmentStatus.Closed);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.SubmitAsync(closed.Id, NewSubmission()));
    }

    [Fact]
    public async Task Student_cannot_submit_to_an_assignment_meant_for_another_class()
    {
        using var context = new TestContext();

        var otherClass = context.GivenAssignment(
            AssignmentStatus.Published, classRoomId: context.ClassNineB.Id, teacherId: context.TeacherTwo.Id);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.SubmitAsync(otherClass.Id, NewSubmission()));
    }

    [Fact]
    public async Task A_student_can_only_submit_once_per_assignment()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        context.GivenSubmission(assignment, context.StudentOne);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<ConflictException>(() => service.SubmitAsync(assignment.Id, NewSubmission()));
    }

    // ---------------------------------------------------------------- deadline

    [Fact]
    public async Task Submitting_after_the_deadline_is_refused_when_the_assignment_forbids_late_work()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(
            AssignmentStatus.Published,
            deadlineUtc: context.Clock.UtcNow.AddDays(-1),
            allowLateSubmission: false);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.SubmitAsync(assignment.Id, NewSubmission()));
    }

    [Fact]
    public async Task Submitting_after_the_deadline_is_accepted_and_flagged_when_late_work_is_allowed()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(
            AssignmentStatus.Published,
            deadlineUtc: context.Clock.UtcNow.AddDays(-1),
            allowLateSubmission: true);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        var submission = await service.SubmitAsync(assignment.Id, NewSubmission());

        submission.IsLate.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Submitted);
    }

    [Fact]
    public async Task The_institution_wide_switch_overrides_an_assignment_that_allows_late_work()
    {
        using var context = new TestContext();

        context.GivenSetting(SettingKeys.AllowLateSubmissionsGlobally, "false");

        var assignment = context.GivenAssignment(
            AssignmentStatus.Published,
            deadlineUtc: context.Clock.UtcNow.AddDays(-1),
            allowLateSubmission: true);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.SubmitAsync(assignment.Id, NewSubmission()));
    }

    [Fact]
    public async Task A_submission_made_before_the_deadline_is_not_flagged_late()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(
            AssignmentStatus.Published, deadlineUtc: context.Clock.UtcNow.AddHours(1));

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        var submission = await service.SubmitAsync(assignment.Id, NewSubmission());

        submission.IsLate.Should().BeFalse();
    }

    // ---------------------------------------------------------------- updating

    [Fact]
    public async Task A_student_can_replace_their_answer_before_the_deadline()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        var submission = context.GivenSubmission(assignment, context.StudentOne);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        var updated = await service.UpdateAsync(submission.Id, new UpdateSubmissionRequest { Content = "A better answer." });

        updated.Content.Should().Be("A better answer.");
        updated.RevisionCount.Should().Be(1);
        updated.LastUpdatedAtUtc.Should().Be(context.Clock.UtcNow);
    }

    [Fact]
    public async Task A_graded_submission_can_no_longer_be_changed()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        var submission = context.GivenSubmission(assignment, context.StudentOne, SubmissionStatus.Graded, marks: 70m);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.UpdateAsync(submission.Id, new UpdateSubmissionRequest { Content = "Second attempt." }));
    }

    [Fact]
    public async Task An_answer_cannot_be_replaced_when_the_assignment_forbids_resubmission()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published, allowResubmission: false);
        var submission = context.GivenSubmission(assignment, context.StudentOne);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.UpdateAsync(submission.Id, new UpdateSubmissionRequest { Content = "Second attempt." }));
    }

    [Fact]
    public async Task A_submission_returned_for_revision_can_always_be_resubmitted()
    {
        using var context = new TestContext();

        // Even though the assignment normally forbids resubmission, the teacher explicitly asked for a fix.
        var assignment = context.GivenAssignment(AssignmentStatus.Published, allowResubmission: false);
        var submission = context.GivenSubmission(assignment, context.StudentOne, SubmissionStatus.ReturnedForRevision);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        var updated = await service.UpdateAsync(submission.Id, new UpdateSubmissionRequest { Content = "Fixed the contact form." });

        updated.Status.Should().Be(SubmissionStatus.Submitted);
        updated.RevisionCount.Should().Be(1);
    }

    [Fact]
    public async Task An_answer_cannot_be_replaced_once_the_assignment_is_closed()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        var submission = context.GivenSubmission(assignment, context.StudentOne);

        assignment.Status = AssignmentStatus.Closed;
        await context.Db.SaveChangesAsync();

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.UpdateAsync(submission.Id, new UpdateSubmissionRequest { Content = "Late edit." }));
    }

    [Fact]
    public async Task A_student_cannot_edit_somebody_elses_submission()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        var submission = context.GivenSubmission(assignment, context.StudentOne);

        context.CurrentUser.SignInAs(context.StudentTwo);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.UpdateAsync(submission.Id, new UpdateSubmissionRequest { Content = "Not mine." }));
    }

    // ---------------------------------------------------------------- grading

    [Fact]
    public async Task Grading_records_the_marks_feedback_and_marker()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published, maxMarks: 100m);
        var submission = context.GivenSubmission(assignment, context.StudentOne);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Submissions(context);

        var graded = await service.GradeAsync(submission.Id, new GradeSubmissionRequest
        {
            Marks = 87.5m,
            Feedback = "Clear working, minor arithmetic slip in question 4."
        });

        graded.Status.Should().Be(SubmissionStatus.Graded);
        graded.Marks.Should().Be(87.5m);
        graded.GradedByName.Should().Be("Teacher One");
        graded.GradedAtUtc.Should().Be(context.Clock.UtcNow);
    }

    [Fact]
    public async Task Marks_above_the_assignment_maximum_are_rejected()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published, maxMarks: 50m);
        var submission = context.GivenSubmission(assignment, context.StudentOne);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.GradeAsync(submission.Id, new GradeSubmissionRequest { Marks = 60m }));
    }

    [Fact]
    public async Task Negative_marks_are_rejected()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        var submission = context.GivenSubmission(assignment, context.StudentOne);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.GradeAsync(submission.Id, new GradeSubmissionRequest { Marks = -1m }));
    }

    [Fact]
    public async Task A_teacher_cannot_grade_work_handed_in_for_another_teachers_assignment()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        var submission = context.GivenSubmission(assignment, context.StudentOne);

        context.CurrentUser.SignInAs(context.TeacherTwo);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GradeAsync(submission.Id, new GradeSubmissionRequest { Marks = 10m }));
    }

    [Fact]
    public async Task A_submission_cannot_be_moved_to_graded_without_marks()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        var submission = context.GivenSubmission(assignment, context.StudentOne);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.UpdateStatusAsync(submission.Id, new UpdateSubmissionStatusRequest { Status = SubmissionStatus.Graded }));
    }

    [Fact]
    public async Task Returning_a_submission_for_revision_clears_any_provisional_marks()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        var submission = context.GivenSubmission(assignment, context.StudentOne, SubmissionStatus.Graded, marks: 40m);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Submissions(context);

        var returned = await service.UpdateStatusAsync(submission.Id, new UpdateSubmissionStatusRequest
        {
            Status = SubmissionStatus.ReturnedForRevision,
            Feedback = "Please redo question 3."
        });

        returned.Status.Should().Be(SubmissionStatus.ReturnedForRevision);
        returned.Marks.Should().BeNull();
        returned.GradedAtUtc.Should().BeNull();
        returned.Feedback.Should().Be("Please redo question 3.");
    }

    // ------------------------------------------------------------- visibility

    [Fact]
    public async Task A_student_only_sees_their_own_submissions()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        var mine = context.GivenSubmission(assignment, context.StudentOne);
        context.GivenSubmission(assignment, context.StudentTwo);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        var result = await service.GetAsync(new SubmissionQuery());

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(mine.Id);
    }

    [Fact]
    public async Task A_teacher_only_sees_submissions_for_the_assignments_they_own()
    {
        using var context = new TestContext();

        var mine = context.GivenAssignment(AssignmentStatus.Published);
        context.GivenSubmission(mine, context.StudentOne);

        var theirs = context.GivenAssignment(
            AssignmentStatus.Published, classRoomId: context.ClassNineB.Id, teacherId: context.TeacherTwo.Id);
        context.GivenSubmission(theirs, context.StudentThree);

        context.CurrentUser.SignInAs(context.TeacherOne);
        var service = ServiceFactory.Submissions(context);

        var result = await service.GetAsync(new SubmissionQuery());

        result.TotalCount.Should().Be(1);
        result.Items.Single().StudentName.Should().Be("Student One");
    }

    [Fact]
    public async Task An_admin_sees_every_submission()
    {
        using var context = new TestContext();

        var first = context.GivenAssignment(AssignmentStatus.Published);
        context.GivenSubmission(first, context.StudentOne);

        var second = context.GivenAssignment(
            AssignmentStatus.Published, classRoomId: context.ClassNineB.Id, teacherId: context.TeacherTwo.Id);
        context.GivenSubmission(second, context.StudentThree);

        context.CurrentUser.SignInAs(context.Admin);
        var service = ServiceFactory.Submissions(context);

        var result = await service.GetAsync(new SubmissionQuery());

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task A_student_cannot_read_another_students_submission()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        var theirs = context.GivenSubmission(assignment, context.StudentTwo);

        context.CurrentUser.SignInAs(context.StudentOne);
        var service = ServiceFactory.Submissions(context);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(theirs.Id));
    }

    private static CreateSubmissionRequest NewSubmission(string? content = null) => new()
    {
        Content = content ?? "My answer to every question.",
        AttachmentUrl = null
    };
}
