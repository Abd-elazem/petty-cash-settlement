namespace PettyCash.Infrastructure.SharePoint.ReferenceData.Models;

internal sealed record SharePointCategoryMappingItem(
    string CategoryCode,
    string ExpenseMainAccount,
    string DimensionDefaults,
    string? SalesTaxGroup,
    string? ItemSalesTaxGroup,
    bool KmRequired,
    bool Active);
