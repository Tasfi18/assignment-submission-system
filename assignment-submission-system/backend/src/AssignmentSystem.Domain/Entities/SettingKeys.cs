namespace AssignmentSystem.Domain.Entities;

/// <summary>Well known keys for <see cref="AppSetting"/>.</summary>
public static class SettingKeys
{
    /// <summary>Global kill switch. When "false", late submissions are refused even if an assignment allows them.</summary>
    public const string AllowLateSubmissionsGlobally = "submissions.allow_late_globally";

    /// <summary>Maximum number of characters accepted in a submission answer.</summary>
    public const string MaxSubmissionLength = "submissions.max_answer_length";

    /// <summary>Institution name shown in the UI header.</summary>
    public const string InstitutionName = "app.institution_name";

    /// <summary>When "false", students cannot log in (for example during an exam window).</summary>
    public const string StudentPortalEnabled = "app.student_portal_enabled";
}
