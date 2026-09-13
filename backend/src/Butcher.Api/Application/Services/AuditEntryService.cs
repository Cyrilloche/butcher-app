using System.Text.Json;
using Butcher.Api.Application.Dtos;
using Butcher.Api.Common;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

/// <summary>
/// Consultation du journal, en lecture seule : aucune méthode ne le modifie ni ne le supprime (FR-024).
/// </summary>
public class AuditEntryService(AppDbContext dbContext) : IAuditEntryService
{
    public const int MaxPageSize = 200;

    public async Task<AuditEntryPageDto> SearchAsync(AuditEntryQuery request)
    {
        if (request.From is not null && request.To is not null && request.From > request.To)
        {
            throw new BadRequestException("La date de début doit précéder la date de fin.");
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = dbContext.AuditEntries.AsNoTracking();

        if (request.AccountId is not null)
        {
            query = query.Where(e => e.AccountId == request.AccountId);
        }

        if (!string.IsNullOrEmpty(request.EntityType))
        {
            var entityType = ParseOrThrow<AuditEntityType>(request.EntityType, "Type d'objet inconnu.");
            query = query.Where(e => e.EntityType == entityType);
        }

        if (!string.IsNullOrEmpty(request.Action))
        {
            var action = ParseOrThrow<AuditAction>(request.Action, "Nature d'opération inconnue.");
            query = query.Where(e => e.Action == action);
        }

        if (request.From is { } from)
        {
            var start = BusinessTime.StartOfDay(from);
            query = query.Where(e => e.OccurredAt >= start);
        }

        if (request.To is { } to)
        {
            var end = BusinessTime.StartOfDay(to.AddDays(1));
            query = query.Where(e => e.OccurredAt < end);
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new { Entry = e, AccountName = e.Account == null ? null : e.Account.DisplayName })
            .ToListAsync();

        return new AuditEntryPageDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = rows.Select(row => new AuditEntryDto
            {
                Id = row.Entry.Id,
                OccurredAt = row.Entry.OccurredAt,
                AccountId = row.Entry.AccountId,
                AccountName = row.AccountName,
                Action = row.Entry.Action,
                EntityType = row.Entry.EntityType,
                EntityId = row.Entry.EntityId,
                EntityLabel = row.Entry.EntityLabel,
                DeletedContent = row.Entry.DeletedContent is null
                    ? null
                    : JsonDocument.Parse(row.Entry.DeletedContent).RootElement.Clone(),
            }).ToList(),
        };
    }

    private static TEnum ParseOrThrow<TEnum>(string value, string message) where TEnum : struct, Enum
    {
        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (EnumSnakeCaseConverter.ToSnakeCase(candidate) == value)
            {
                return candidate;
            }
        }

        throw new BadRequestException(message);
    }
}
