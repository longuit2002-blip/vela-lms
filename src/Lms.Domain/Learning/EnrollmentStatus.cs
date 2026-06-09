namespace Lms.Domain.Learning;

/// <summary>Lifecycle of an <see cref="Enrollment"/>. <c>Expired</c> is reserved for the expiry slice.</summary>
public enum EnrollmentStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Expired = 3,
}
