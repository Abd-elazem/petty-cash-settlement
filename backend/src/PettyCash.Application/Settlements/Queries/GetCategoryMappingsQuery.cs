using PettyCash.Application.Abstractions;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;

namespace PettyCash.Application.Settlements.Queries;

/// <summary>
/// Returns all active category mappings as lightweight DTOs for SPA dropdown population.
/// Read-only; no authorization check beyond "authenticated" — every spender and approver
/// needs the list to render the line form.
/// </summary>
public sealed record GetCategoryMappingsQuery : IQuery<IReadOnlyList<CategoryMappingDto>>;

public sealed class GetCategoryMappingsQueryHandler
    : IQueryHandler<GetCategoryMappingsQuery, IReadOnlyList<CategoryMappingDto>>
{
    private readonly ICategoryMappingRepository _repository;

    public GetCategoryMappingsQueryHandler(ICategoryMappingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CategoryMappingDto>> HandleAsync(
        GetCategoryMappingsQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CategoryMappingReadModel> rows =
            await _repository.GetAllActiveAsync(cancellationToken);

        return rows
            .OrderBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(r => new CategoryMappingDto(r.CategoryCode, r.DisplayName, r.KmRequired))
            .ToList();
    }
}
