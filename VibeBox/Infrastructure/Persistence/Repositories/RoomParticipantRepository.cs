using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class RoomParticipantRepository
    : IRoomParticipantRepository
{
    private readonly ApplicationDbContext _context;

    public RoomParticipantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _context.RoomParticipants
            .AnyAsync(
                x => x.RoomId == roomId && x.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        RoomParticipant participant,
        CancellationToken cancellationToken)
    {
        await _context.RoomParticipants.AddAsync(
            participant,
            cancellationToken);
    }

    public async Task<IReadOnlyList<RoomParticipant>> GetByRoomIdAsync(
        Guid roomId,
        CancellationToken cancellationToken)
    {
        return await _context.RoomParticipants
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.JoinedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RoomParticipant?> GetByRoomIdAndUserIdAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.RoomParticipants
            .FirstOrDefaultAsync(
                x => x.RoomId == roomId && x.UserId == userId,
                cancellationToken);
    }

    public void Remove(RoomParticipant participant)
    {
        _context.RoomParticipants.Remove(participant);
    }
}