using Asp.Versioning;
using Asp.Versioning.Builder;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Settlements.Queries;

namespace PettyCash.Api.Endpoints;

/// <summary>
/// Reference-data endpoints. Separate from SettlementsEndpoints because category mappings
/// are Finance-owned master data, not settlement resources — a different resource family,
/// a different route prefix, a different responsibility.
/// </summary>
public static class CategoryMappingsEndpoints
{
    public static IEndpointRouteBuilder MapCategoryMappingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ApiVersionSet versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        RouteGroupBuilder group = endpoints
            .MapGroup("/api/v{version:apiVersion}/category-mappings")
            .WithApiVersionSet(versionSet)
            .WithTags("CategoryMappings")
            .RequireAuthorization();   // Any authenticated user (spender or approver) may call this.

        group.MapGet("/", GetCategoryMappingsAsync)
            .WithName("GetCategoryMappings")
            .WithSummary("Returns all active category mappings for SPA dropdown population.")
            .Produces<IReadOnlyList<CategoryMappingDto>>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<IResult> GetCategoryMappingsAsync(
        IQueryHandler<GetCategoryMappingsQuery, IReadOnlyList<CategoryMappingDto>> handler,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CategoryMappingDto> categories =
            await handler.HandleAsync(new GetCategoryMappingsQuery(), cancellationToken);

        return Results.Ok(categories);
    }
}
