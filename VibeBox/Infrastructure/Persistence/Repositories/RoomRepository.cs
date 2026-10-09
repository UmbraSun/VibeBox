using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class RoomRepository : IRoomRepository
{
    private readonly ApplicationDbContext _context;

    public RoomRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Room room,
        CancellationToken cancellationToken)
    {
        await _context.Rooms.AddAsync(
            room,
            cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        return await _context.Rooms
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<Room?> GetByIdAndOwnerIdAsync(
        Guid roomId,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        return _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == roomId && x.OwnerId == ownerId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> GetByParticipantIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.Rooms
            .AsNoTracking()
            .Where(x => x.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}