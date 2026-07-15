namespace PettyCash.Application.Abstractions;

/// <summary>
/// Abstraction over wherever receipt photo binaries live (SharePoint document library in
/// production, per DECISIONS.md D-007; local disk/object storage in dev). Returns an
/// opaque StorageRef the caller can persist and later resolve back to a viewable URL —
/// Application never interprets the format of that string.
///
/// NOTE: no UploadReceiptPhotoCommand exists yet even though this interface does. The
/// Settlement/SettlementLine aggregate (frozen as of Milestone 0.2) has no ReceiptPhoto
/// concept, so there's currently nowhere in the domain model to persist the StorageRef
/// against a line. Wiring the full upload use case requires an explicit decision on
/// whether photo references belong inside the Settlement aggregate's consistency boundary
/// or are tracked separately — flagged in ASSUMPTIONS.md as a new item rather than
/// smuggled in as a change to frozen Domain code. See TODO.md for the follow-up milestone.
/// </summary>
public interface IPhotoStore
{
    Task<string> SavePhotoAsync(
        Guid settlementId,
        Guid lineId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
