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

        var lockId = Guid.NewGuid();

        var claimed = await _context.StorageDeletionOutboxMessages
            .Where(x =>
                x.Id == candidateId &&
                x.ProcessedAtUtc == null &&
                x.NextAttemptAtUtc <= now &&
                (x.LockedUntilUtc == null || x.LockedUntilUtc <= now))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.LockedUntilUtc, lockUntil)
                    .SetProperty(x => x.LockId, lockId)
                    .SetProperty(x => x.Attempts, x => x.Attempts + 1),
                cancellationToken);

        if (claimed == 0)
            return null;

        return await _context.StorageDeletionOutboxMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == candidateId && x.LockId == lockId,
                cancellationToken);
    }

    public async Task<bool> MarkAsProcessedAsync(
        Guid id,
        Guid lockId,
        DateTime processedAtUtc,
        CancellationToken cancellationToken)
    {
        var updated = await _context.StorageDeletionOutboxMessages
            .Where(x => x.Id == id && x.LockId == lockId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ProcessedAtUtc, processedAtUtc)
                    .SetProperty(x => x.LockedUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.LockId, (Guid?)null)
                    .SetProperty(x => x.LastError, (string?)null),
                cancellationToken);

        return updated > 0;
    }

    public async Task<bool> MarkAsFailedAsync(
        Guid id,
        Guid lockId,
        DateTime nextAttemptAtUtc,
        string error,
        CancellationToken cancellationToken)
    {
        var updated = await _context.StorageDeletionOutboxMessages
            .Where(x => x.Id == id && x.LockId == lockId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.NextAttemptAtUtc, nextAttemptAtUtc)
                    .SetProperty(x => x.LockedUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.LockId, (Guid?)null)
                    .SetProperty(x => x.LastError, error.Length > 2000
                        ? error[..2000]
                        : error),
                cancellationToken);

        return updated > 0;
    }
}