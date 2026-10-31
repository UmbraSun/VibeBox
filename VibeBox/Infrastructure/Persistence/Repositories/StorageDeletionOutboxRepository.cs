using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class StorageDeletionOutboxRepository
    : IStorageDeletionOutboxRepository
{
    private readonly ApplicationDbContext _context;

    public StorageDeletionOutboxRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StorageDeletionOutboxMessage?> TryClaimNextAsync(
        DateTime now,
        DateTime lockUntil,
        CancellationToken cancellationToken)
    {
        var candidateId = await _context.StorageDeletionOutboxMessages
            .AsNoTracking()
            .Where(x =>
                x.ProcessedAtUtc == null &&
                x.NextAttemptAtUtc <= now &&
                (x.LockedUntilUtc == null || x.LockedUntilUtc <= now))
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidateId == Guid.Empty)
            return null;

        var claimed = await _context.StorageDeletionOutboxMessages
            .Where(x =>
                x.Id == candidateId &&
                x.ProcessedAtUtc == null &&
                x.NextAttemptAtUtc <= now &&
                (x.LockedUntilUtc == null || x.LockedUntilUtc <= now))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.LockedUntilUtc, lockUntil)
                    .SetProperty(x => x.Attempts, x => x.Attempts + 1),
                cancellationToken);

        if (claimed == 0)
            return null;

        return await _context.StorageDeletionOutboxMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == candidateId,
                cancellationToken);
    }

    public async Task MarkAsProcessedAsync(
        Guid id,
        DateTime processedAtUtc,
        CancellationToken cancellationToken)
    {
        await _context.StorageDeletionOutboxMessages
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ProcessedAtUtc, processedAtUtc)
                    .SetProperty(x => x.LockedUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.LastError, (string?)null),
                cancellationToken);
    }

    public async Task MarkAsFailedAsync(
        Guid id,
        DateTime nextAttemptAtUtc,
        string error,
        CancellationToken cancellationToken)
    {
        await _context.StorageDeletionOutboxMessages
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.NextAttemptAtUtc, nextAttemptAtUtc)
                    .SetProperty(x => x.LockedUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.LastError, error.Length > 2000
                        ? error[..2000]
                        : error),
                cancellationToken);
    }
}