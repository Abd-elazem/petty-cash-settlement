namespace PettyCash.Infrastructure.SharePoint.ReferenceData.Models;

internal sealed record SharePointAppUserProfileItem(
    string AppUserId,
    string DisplayName,
    string WorkerId,
    string ApproverEmail,
    bool Active);
