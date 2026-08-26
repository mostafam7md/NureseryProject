namespace NurseryLink.Application.Common;

public static class AuditActions
{
    public const string AdminCreated = "AdminCreated";
    public const string PrivilegesChanged = "PrivilegesChanged";
    public const string AccountDeactivated = "AccountDeactivated";
    public const string AccountReactivated = "AccountReactivated";
    public const string TeacherCreated = "TeacherCreated";
    public const string ClassCreated = "ClassCreated";
    public const string ClassRenamed = "ClassRenamed";
    public const string ClassDeactivated = "ClassDeactivated";
    public const string ClassTeacherAssigned = "ClassTeacherAssigned";
    public const string ClassTeacherUnassigned = "ClassTeacherUnassigned";
    public const string StudentCreated = "StudentCreated";
    public const string StudentUpdated = "StudentUpdated";
    public const string StudentTransferred = "StudentTransferred";
    public const string StudentWithdrawn = "StudentWithdrawn";
    public const string StudentReadmitted = "StudentReadmitted";
    public const string ParentCreated = "ParentCreated";
    public const string ParentStudentLinked = "ParentStudentLinked";
    public const string ParentStudentUnlinked = "ParentStudentUnlinked";
}
