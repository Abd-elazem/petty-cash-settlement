using PettyCash.Application.Abstractions;
using PettyCash.Infrastructure.SharePoint.ReferenceData.Gateways;

namespace PettyCash.Infrastructure.SharePoint.Repositories;

internal sealed class SharePointCategoryMappingRepository : ICategoryMappingRepository
{
    private readonly ISharePointCategoryMappingsGateway _gateway;

    internal SharePointCategoryMappingRepository(ISharePointCategoryMappingsGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<CategoryMappingReadModel?> GetByCategoryCodeAsync(string categoryCode, CancellationToken cancellationToken = default)
    {
        var item = await _gateway.GetByCategoryCodeAsync(categoryCode, cancellationToken);
        return item is null
            ? null
            : new CategoryMappingReadModel(
                item.CategoryCode,
                item.ExpenseMainAccount,
                item.DimensionDefaults,
                item.SalesTaxGroup,
                item.ItemSalesTaxGroup,
                item.KmRequired,
                item.Active);
    }

    public async Task<IReadOnlyList<CategoryMappingReadModel>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var items = await _gateway.GetAllActiveAsync(cancellationToken);
        return items
            .Where(item => item.Active)
            .Select(item => new CategoryMappingReadModel(
                item.CategoryCode,
                item.ExpenseMainAccount,
                item.DimensionDefaults,
                item.SalesTaxGroup,
                item.ItemSalesTaxGroup,
                item.KmRequired,
                item.Active))
            .ToList();
    }
}
