namespace Butcher.Api.Domain.Enums;

/// <summary>Nature d'une entrée du journal (FR-021, FR-025). Stockée en <c>snake_case</c>.</summary>
public enum AuditAction
{
    Created,
    Updated,
    Deleted,
    LoginSucceeded,
    LoginFailed,
    LockedOut,
    PasswordChanged,
}
