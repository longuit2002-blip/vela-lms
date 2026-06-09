namespace Lms.Domain.Learning;

/// <summary>How an <see cref="Enrollment"/> came to be. <c>Self</c> (self-enrollment) lands in a later slice.</summary>
public enum EnrollmentSource
{
    Assigned = 0,
    Self = 1,
}
