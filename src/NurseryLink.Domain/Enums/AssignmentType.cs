namespace NurseryLink.Domain.Enums;

public enum AssignmentType
{
    /// <summary>The class's one and only regular teacher. Enforced by a filtered unique index.</summary>
    Permanent = 0,

    /// <summary>Temporary cover. Multiple substitutes may overlap with the permanent teacher.</summary>
    Substitute = 1
}
