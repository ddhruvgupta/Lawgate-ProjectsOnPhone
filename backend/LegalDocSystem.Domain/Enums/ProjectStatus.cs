namespace LegalDocSystem.Domain.Enums;

public enum ProjectStatus
{
    /// <summary>
    /// Matter received, client onboarding in progress
    /// </summary>
    Intake = 1,

    /// <summary>
    /// Matter is actively being worked on
    /// </summary>
    Active = 2,

    /// <summary>
    /// Evidence and document gathering phase
    /// </summary>
    Discovery = 3,

    /// <summary>
    /// Settlement or resolution discussions underway
    /// </summary>
    Negotiation = 4,

    /// <summary>
    /// Scheduled before a tribunal, court, or arbitration panel
    /// </summary>
    Hearing = 5,

    /// <summary>
    /// Matter is temporarily paused or waiting
    /// </summary>
    OnHold = 6,

    /// <summary>
    /// Matter resolved via settlement or agreement
    /// </summary>
    Settled = 7,

    /// <summary>
    /// Matter concluded (won, lost, or withdrawn)
    /// </summary>
    Closed = 8,

    /// <summary>
    /// Long-term archive, no active work
    /// </summary>
    Archived = 9
}
