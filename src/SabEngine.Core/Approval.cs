namespace SabEngine.Core;

/// <summary>
/// A human's approve/decline decision on a <see cref="Plan"/> — the
/// recommend-and-approve gate (design doc, Section 2 and 4.1). Records
/// *who*, not just "approved", per Section 7's audit requirement.
/// </summary>
public sealed class Approval
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid PlanId { get; init; }

    public required bool WasApproved { get; init; }

    /// <summary>The specific human who made this decision. Never blank.</summary>
    public required string ApprovedByUserId { get; init; }

    public DateTimeOffset DecidedAt { get; init; } = DateTimeOffset.UtcNow;
}
