using Butcher.Api.Application.Dtos;

namespace Butcher.Api.Application.Services;

public interface IAuditEntryService
{
    /// <summary>Entrées du plus récent au plus ancien, filtrées et paginées (FR-023).</summary>
    Task<AuditEntryPageDto> SearchAsync(AuditEntryQuery query);
}
